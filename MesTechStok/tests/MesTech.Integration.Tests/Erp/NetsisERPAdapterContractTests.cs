using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.ERP.Netsis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace MesTech.Integration.Tests.Erp;

/// <summary>
/// Dalga 13: Netsis ERP adapter contract tests — WireMock-based.
/// Verifies adapter correctly handles Netsis REST API responses.
/// </summary>
public class NetsisERPAdapterContractTests : IDisposable
{
    private readonly WireMockServer _mockServer;

    public NetsisERPAdapterContractTests()
        => _mockServer = WireMockServer.Start();

    [Fact]
    public async Task PingAsync_WhenServerOk_ReturnsTrue()
    {
        _mockServer.Given(
            Request.Create().WithPath("/ping").UsingGet()
        ).RespondWith(Response.Create().WithStatusCode(200)
            .WithBody("""{"status":"ok"}"""));

        var adapter = CreateAdapter();
        var result = await adapter.PingAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task PingAsync_WhenServerDown_ReturnsFalse()
    {
        // No mock configured for /ping — will get 404
        var adapter = CreateAdapter();
        var result = await adapter.PingAsync();
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAccountBalancesAsync_ParsesCariList()
    {
        _mockServer.Given(
            Request.Create().WithPath("/cariler").UsingGet()
        ).RespondWith(Response.Create().WithStatusCode(200).WithBody("""
        [
            {"cariKod": "320.001", "cariAd": "Tedarikci A", "bakiye": -15000.00},
            {"cariKod": "120.001", "cariAd": "Kasa TL",     "bakiye":  42500.00}
        ]
        """));

        var adapter = CreateAdapter();
        var accounts = await adapter.GetAccountBalancesAsync();

        accounts.Should().HaveCount(2);
        accounts.Should().Contain(a => a.AccountCode == "320.001");
        accounts.First(a => a.AccountCode == "120.001")
            .Balance.Should().Be(42500.00m);
    }

    [Fact]
    public async Task SyncInvoiceAsync_WhenServerFails_ReturnsFailure()
    {
        _mockServer.Given(
            Request.Create().WithPath("/faturalar").UsingPost()
        ).RespondWith(Response.Create().WithStatusCode(500)
            .WithBody("""{"hata":"Sunucu hatasi"}"""));

        var adapter = CreateAdapter();
        var result = await adapter.SyncInvoiceAsync(Guid.NewGuid());
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public void NetsisAdapter_Provider_ShouldBeNetsis()
    {
        var adapter = CreateAdapter();
        adapter.Provider.Should().Be(ErpProvider.Netsis);
    }

    private NetsisERPAdapter CreateAdapter()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ERP:Netsis:BaseUrl"]  = _mockServer.Url,
                ["ERP:Netsis:Username"] = "test-user",
                ["ERP:Netsis:Password"] = "test-pass"
            })
            .Build();

        return new NetsisERPAdapter(
            new HttpClient(),
            config,
            Mock.Of<IOrderRepository>(),
            NullLogger<NetsisERPAdapter>.Instance);
    }

    public void Dispose() => _mockServer.Stop();
}
