using System.Net;
using FluentAssertions;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.ERP.BizimHesap;
using MesTech.Infrastructure.Integration.ERP.ERPNext;
using MesTech.Infrastructure.Integration.ERP.Parasut;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Infrastructure.ERP;

/// <summary>
/// ERP adapter gap tests — BizimHesap, Parasut, ERPNext.
/// G488: basic constructor, SyncOrder null guard, PingAsync.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "ERP")]
[Trait("Group", "AdapterGap")]
public class ERPAdapterGapTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();

    private HttpClient CreateHttpClient() =>
        new HttpClient(_handler) { BaseAddress = new Uri("https://api.test.com/") };

    // ═══════════════════════════════════════
    // BizimHesapERPAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void BizimHesap_Constructor_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BizimHesap:BaseUrl"] = "https://api.bizimhesap.com",
                ["BizimHesap:ApiKey"] = "test-key"
            }).Build();

        var client = new BizimHesapApiClient(
            CreateHttpClient(), config, Mock.Of<ILogger<BizimHesapApiClient>>());

        var adapter = new BizimHesapERPAdapter(
            client, _orderRepo.Object, _invoiceRepo.Object,
            Mock.Of<ILogger<BizimHesapERPAdapter>>());

        adapter.Should().NotBeNull();
    }

    [Fact]
    public async Task BizimHesap_SyncOrder_NullOrderId_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BizimHesap:BaseUrl"] = "https://api.bizimhesap.com",
                ["BizimHesap:ApiKey"] = "test-key"
            }).Build();

        var client = new BizimHesapApiClient(
            CreateHttpClient(), config, Mock.Of<ILogger<BizimHesapApiClient>>());

        var adapter = new BizimHesapERPAdapter(
            client, _orderRepo.Object, _invoiceRepo.Object,
            Mock.Of<ILogger<BizimHesapERPAdapter>>());

        // Act & Assert — null/empty order should be handled
        var result = await adapter.SyncOrderAsync(Guid.Empty, CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
    }

    // ═══════════════════════════════════════
    // ParasutERPAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void Parasut_Constructor_DoesNotThrow()
    {
        var tokenService = new Mock<ParasutTokenService>(
            CreateHttpClient(), Mock.Of<ILogger<ParasutTokenService>>());

        var adapter = new ParasutERPAdapter(
            CreateHttpClient(), tokenService.Object,
            _orderRepo.Object, _invoiceRepo.Object,
            Mock.Of<ILogger<ParasutERPAdapter>>());

        adapter.Should().NotBeNull();
    }

    [Fact]
    public async Task Parasut_SyncOrder_EmptyGuid_ReturnsFailed()
    {
        var tokenService = new Mock<ParasutTokenService>(
            CreateHttpClient(), Mock.Of<ILogger<ParasutTokenService>>());

        var adapter = new ParasutERPAdapter(
            CreateHttpClient(), tokenService.Object,
            _orderRepo.Object, _invoiceRepo.Object,
            Mock.Of<ILogger<ParasutERPAdapter>>());

        var result = await adapter.SyncOrderAsync(Guid.Empty, CancellationToken.None);
        result.IsSuccess.Should().BeFalse();
    }

    // ═══════════════════════════════════════
    // ERPNextRestAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void ERPNext_Constructor_DoesNotThrow()
    {
        var options = Options.Create(new ERPNextOptions
        {
            BaseUrl = "https://erpnext.test.com",
            ApiKey = "test-key",
            ApiSecret = "test-secret"
        });

        var adapter = new ERPNextRestAdapter(
            CreateHttpClient(),
            Mock.Of<ILogger<ERPNextRestAdapter>>(),
            options);

        adapter.Should().NotBeNull();
    }

    [Fact]
    public async Task ERPNext_Ping_ServerDown_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);

        var options = Options.Create(new ERPNextOptions
        {
            BaseUrl = "https://erpnext.test.com",
            ApiKey = "test-key",
            ApiSecret = "test-secret"
        });

        var adapter = new ERPNextRestAdapter(
            CreateHttpClient(),
            Mock.Of<ILogger<ERPNextRestAdapter>>(),
            options);

        var result = await adapter.PingAsync(CancellationToken.None);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ERPNext_Ping_ServerOK_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, """{"message":"pong"}""");

        var options = Options.Create(new ERPNextOptions
        {
            BaseUrl = "https://erpnext.test.com",
            ApiKey = "test-key",
            ApiSecret = "test-secret"
        });

        var adapter = new ERPNextRestAdapter(
            CreateHttpClient(),
            Mock.Of<ILogger<ERPNextRestAdapter>>(),
            options);

        var result = await adapter.PingAsync(CancellationToken.None);
        result.Should().BeTrue();
    }
}
