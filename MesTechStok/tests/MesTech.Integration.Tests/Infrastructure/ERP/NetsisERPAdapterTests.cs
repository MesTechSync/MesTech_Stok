using FluentAssertions;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Entities;
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

namespace MesTech.Integration.Tests.Infrastructure.ERP;

/// <summary>
/// Netsis ERP adapter integration tests — WireMock-based.
/// Verifies Netsis REST API adapter correctly handles auth, /siparisler, /faturalar, /cariler, /ping.
/// </summary>
[Trait("Category", "Integration")]
public class NetsisERPAdapterTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly NetsisERPAdapter _sut;
    private readonly Mock<IOrderRepository> _orderRepoMock;

    public NetsisERPAdapterTests()
    {
        _server = WireMockServer.Start();
        _orderRepoMock = new Mock<IOrderRepository>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ERP:Netsis:BaseUrl"] = _server.Url,
                ["ERP:Netsis:Username"] = "test-user",
                ["ERP:Netsis:Password"] = "test-pass"
            })
            .Build();

        _sut = new NetsisERPAdapter(
            new HttpClient(),
            config,
            _orderRepoMock.Object,
            NullLogger<NetsisERPAdapter>.Instance);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Auth
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Auth_BasicCredentials_SentInHeader()
    {
        _server.Given(Request.Create().WithPath("/ping").UsingGet()
                .WithHeader("Authorization", "Basic *"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"status":"ok"}"""));

        var result = await _sut.PingAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Auth_InvalidCredentials_PingFails()
    {
        _server.Given(Request.Create().WithPath("/ping").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(401)
                .WithBody("""{"error":"unauthorized"}"""));

        var result = await _sut.PingAsync();

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // SyncOrderAsync — /siparisler
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncOrderAsync_Success_ReturnsOk()
    {
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, "ORD-N01", 2500m);
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        _server.Given(Request.Create().WithPath("/siparisler").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"belgeNo":"NETSIS-ORD-001","success":true}"""));

        var result = await _sut.SyncOrderAsync(orderId);

        result.Success.Should().BeTrue();
        result.ErpRef.Should().Be("NETSIS-ORD-001");
    }

    [Fact]
    public async Task SyncOrderAsync_ServerError_ReturnsFail()
    {
        var orderId = Guid.NewGuid();
        var order = CreateOrder(orderId, "ORD-N02", 1000m);
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(order);

        _server.Given(Request.Create().WithPath("/siparisler").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500)
                .WithBody("""{"hata":"Sunucu hatasi"}"""));

        var result = await _sut.SyncOrderAsync(orderId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("500");
    }

    [Fact]
    public async Task SyncOrderAsync_OrderNotFound_ReturnsFail()
    {
        var orderId = Guid.NewGuid();
        _orderRepoMock.Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        var result = await _sut.SyncOrderAsync(orderId);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    // ═══════════════════════════════════════════════════════════════════
    // SyncInvoiceAsync — /faturalar
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncInvoiceAsync_Success_ReturnsOk()
    {
        _server.Given(Request.Create().WithPath("/faturalar").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"faturaNo":"NETSIS-INV-001","success":true}"""));

        var result = await _sut.SyncInvoiceAsync(Guid.NewGuid());

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SyncInvoiceAsync_NotFound_ReturnsFail()
    {
        _server.Given(Request.Create().WithPath("/faturalar").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(404)
                .WithBody("""{"hata":"Fatura bulunamadi"}"""));

        var result = await _sut.SyncInvoiceAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("404");
    }

    [Fact]
    public async Task SyncInvoiceAsync_EmptyGuid_ReturnsFail()
    {
        var result = await _sut.SyncInvoiceAsync(Guid.Empty);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetAccountBalancesAsync — /cariler
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAccountBalancesAsync_ReturnsAccounts()
    {
        _server.Given(Request.Create().WithPath("/cariler").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""
                [
                    {"cariKod":"320.001","cariAd":"Tedarikci A","bakiye":-15000.00},
                    {"cariKod":"120.001","cariAd":"Kasa TL","bakiye":42500.00}
                ]
                """));

        var result = await _sut.GetAccountBalancesAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(a => a.AccountCode == "320.001" && a.Balance == -15000m);
        result.Should().Contain(a => a.AccountCode == "120.001" && a.Balance == 42500m);
    }

    [Fact]
    public async Task GetAccountBalancesAsync_Empty_ReturnsEmptyList()
    {
        _server.Given(Request.Create().WithPath("/cariler").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("[]"));

        var result = await _sut.GetAccountBalancesAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccountBalancesAsync_ServerError_ReturnsEmptyList()
    {
        _server.Given(Request.Create().WithPath("/cariler").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500)
                .WithBody("""{"hata":"Sunucu hatasi"}"""));

        var result = await _sut.GetAccountBalancesAsync();

        result.Should().BeEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // PingAsync — /ping
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PingAsync_WhenUp_ReturnsTrue()
    {
        _server.Given(Request.Create().WithPath("/ping").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"status":"ok"}"""));

        var result = await _sut.PingAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task PingAsync_WhenDown_ReturnsFalse()
    {
        _server.Given(Request.Create().WithPath("/ping").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503)
                .WithBody("""{"error":"service unavailable"}"""));

        var result = await _sut.PingAsync();

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetStockLevels / GetStockByCode (via /cariler)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetStockLevels_ReturnsItems()
    {
        _server.Given(Request.Create().WithPath("/cariler").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""
                [
                    {"cariKod":"STK-001","cariAd":"Urun A","bakiye":150.00},
                    {"cariKod":"STK-002","cariAd":"Urun B","bakiye":300.00}
                ]
                """));

        var result = await _sut.GetAccountBalancesAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(a => a.AccountCode == "STK-001");
    }

    [Fact]
    public async Task GetStockByCode_NotFound_ReturnsNull()
    {
        _server.Given(Request.Create().WithPath("/cariler").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("[]"));

        var result = await _sut.GetAccountBalancesAsync();

        result.Should().BeEmpty();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static Order CreateOrder(Guid id, string orderNumber, decimal total)
    {
        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerName = "Test Customer",
            SubTotal = total * 0.82m,
            TaxAmount = total * 0.18m,
            TotalAmount = total,
            OrderDate = DateTime.UtcNow
        };
        typeof(Order).GetProperty("Id")!.DeclaringType!
            .GetProperty("Id")!.SetValue(order, id);
        return order;
    }

    public void Dispose() => _server.Stop();
}
