using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.ERP.Netsis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MesTech.Tests.Unit.Infrastructure.Erp;

/// <summary>
/// Dalga 13: NetsisERPAdapter unit tests — no HTTP calls, only property/config checks.
/// </summary>
public class NetsisERPAdapterUnitTests
{
    private static NetsisERPAdapter CreateAdapter(bool withConfig = true)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(withConfig
                ? new Dictionary<string, string?>
                {
                    ["ERP:Netsis:BaseUrl"]  = "https://test.netsis.com.tr/api/v2",
                    ["ERP:Netsis:Username"] = "test",
                    ["ERP:Netsis:Password"] = "pass"
                }
                : new Dictionary<string, string?>())
            .Build();

        return new NetsisERPAdapter(
            new HttpClient(),
            config,
            Mock.Of<IOrderRepository>(),
            NullLogger<NetsisERPAdapter>.Instance);
    }

    [Fact]
    public void Provider_ShouldBeNetsis()
    {
        var adapter = CreateAdapter();
        adapter.Provider.Should().Be(ErpProvider.Netsis);
    }

    [Fact]
    public async Task SyncOrderAsync_WithEmptyGuid_ReturnsFail()
    {
        var adapter = CreateAdapter();
        var result = await adapter.SyncOrderAsync(Guid.Empty);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public async Task SyncInvoiceAsync_WithEmptyGuid_ReturnsFail()
    {
        var adapter = CreateAdapter();
        var result = await adapter.SyncInvoiceAsync(Guid.Empty);
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public async Task SyncOrderAsync_WithNonExistentOrder_ReturnsFail()
    {
        var adapter = CreateAdapter();
        var result = await adapter.SyncOrderAsync(Guid.NewGuid());
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }
}
