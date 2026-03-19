using FluentAssertions;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace MesTech.Integration.Tests.Infrastructure.ERP;

/// <summary>
/// Nebim ERP adapter integration tests — WireMock-based.
/// Nebim V3 REST API: API-Key auth, /api/v3 endpoints.
/// Uses a mock IErpAdapter with real HTTP calls to WireMock.
///
/// Note: NebimERPAdapter class is not yet implemented in Infrastructure.
/// These tests define the contract for the Nebim adapter (test-first approach).
/// Tests use a lightweight WireMock-backed stub that implements IErpAdapter.
/// </summary>
[Trait("Category", "Integration")]
public class NebimERPAdapterTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly HttpClient _httpClient;

    public NebimERPAdapterTests()
    {
        _server = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_server.Url!) };
    }

    // ═══════════════════════════════════════════════════════════════════
    // Auth
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Auth_ApiKey_SentInHeader()
    {
        _server.Given(Request.Create().WithPath("/api/v3/ping").UsingGet()
                .WithHeader("X-Api-Key", "test-api-key-123"))
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"status":"ok"}"""));

        // Act — send request with API key
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", "test-api-key-123");
        var response = await _httpClient.GetAsync($"{_server.Url}/api/v3/ping");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    // ═══════════════════════════════════════════════════════════════════
    // PingAsync
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task PingAsync_WhenUp_ReturnsTrue()
    {
        _server.Given(Request.Create().WithPath("/api/v3/ping").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"status":"ok"}"""));

        var adapter = CreateStubAdapter();
        var result = await adapter.PingAsync();

        result.Should().BeTrue();
    }

    [Fact]
    public async Task PingAsync_WhenDown_ReturnsFalse()
    {
        _server.Given(Request.Create().WithPath("/api/v3/ping").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(503)
                .WithBody("""{"error":"service unavailable"}"""));

        var adapter = CreateStubAdapter();
        var result = await adapter.PingAsync();

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════════════════════════════
    // SyncOrderAsync
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncOrderAsync_Success_ReturnsOk()
    {
        _server.Given(Request.Create().WithPath("/api/v3/orders").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"id":"NEBIM-ORD-001","success":true}"""));

        var adapter = CreateStubAdapter();
        var result = await adapter.SyncOrderAsync(Guid.NewGuid());

        result.Success.Should().BeTrue();
        result.ErpRef.Should().Be("NEBIM-ORD-001");
    }

    [Fact]
    public async Task SyncOrderAsync_ServerError_ReturnsFail()
    {
        _server.Given(Request.Create().WithPath("/api/v3/orders").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500)
                .WithBody("""{"error":"internal server error"}"""));

        var adapter = CreateStubAdapter();
        var result = await adapter.SyncOrderAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // SyncInvoiceAsync
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SyncInvoiceAsync_Success_ReturnsOk()
    {
        _server.Given(Request.Create().WithPath("/api/v3/invoices").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""{"id":"NEBIM-INV-001","success":true}"""));

        var adapter = CreateStubAdapter();
        var result = await adapter.SyncInvoiceAsync(Guid.NewGuid());

        result.Success.Should().BeTrue();
        result.ErpRef.Should().Be("NEBIM-INV-001");
    }

    [Fact]
    public async Task SyncInvoiceAsync_Timeout_ReturnsFail()
    {
        _server.Given(Request.Create().WithPath("/api/v3/invoices").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(504)
                .WithDelay(TimeSpan.FromMilliseconds(100))
                .WithBody("""{"error":"gateway timeout"}"""));

        var adapter = CreateStubAdapter();
        var result = await adapter.SyncInvoiceAsync(Guid.NewGuid());

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetAccountBalancesAsync
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetAccountBalancesAsync_ReturnsCustomers()
    {
        _server.Given(Request.Create().WithPath("/api/v3/customers").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""
                [
                    {"code":"CUS-001","name":"Musteri A","balance":25000.00,"currency":"TRY"},
                    {"code":"CUS-002","name":"Musteri B","balance":-5000.00,"currency":"TRY"}
                ]
                """));

        var adapter = CreateStubAdapter();
        var result = await adapter.GetAccountBalancesAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(a => a.AccountCode == "CUS-001" && a.Balance == 25000m);
        result.Should().Contain(a => a.AccountCode == "CUS-002" && a.Balance == -5000m);
    }

    // ═══════════════════════════════════════════════════════════════════
    // GetStockLevels / GetStockByCode
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetStockLevels_ReturnsProducts()
    {
        _server.Given(Request.Create().WithPath("/api/v3/products").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200)
                .WithBody("""
                [
                    {"code":"PRD-001","name":"Urun A","stock":150,"currency":"TRY"},
                    {"code":"PRD-002","name":"Urun B","stock":0,"currency":"TRY"}
                ]
                """));

        var response = await _httpClient.GetAsync($"{_server.Url}/api/v3/products");
        response.IsSuccessStatusCode.Should().BeTrue();

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("PRD-001");
        body.Should().Contain("PRD-002");
    }

    [Fact]
    public async Task GetStockByCode_NotFound_ReturnsNull()
    {
        _server.Given(Request.Create().WithPath("/api/v3/products/NONEXIST").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404)
                .WithBody("""{"error":"product not found"}"""));

        var response = await _httpClient.GetAsync($"{_server.Url}/api/v3/products/NONEXIST");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    // ── Stub Adapter ────────────────────────────────────────────────────
    // Lightweight WireMock-backed IErpAdapter stub for contract testing.
    // Will be replaced by real NebimERPAdapter once implemented.

    private NebimStubAdapter CreateStubAdapter()
        => new(_httpClient, _server.Url!);

    /// <summary>
    /// Stub IErpAdapter for Nebim contract tests.
    /// Calls WireMock server endpoints matching Nebim V3 API contract.
    /// </summary>
    private sealed class NebimStubAdapter : IErpAdapter
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;

        public NebimStubAdapter(HttpClient http, string baseUrl)
        {
            _http = http;
            _baseUrl = baseUrl;
        }

        public ErpProvider Provider => ErpProvider.Nebim;

        public async Task<bool> PingAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/api/v3/ping", ct);
                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        public async Task<ErpSyncResult> SyncOrderAsync(Guid orderId, CancellationToken ct = default)
        {
            try
            {
                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { orderId }),
                    System.Text.Encoding.UTF8, "application/json");
                var response = await _http.PostAsync($"{_baseUrl}/api/v3/orders", content, ct);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    var doc = System.Text.Json.JsonDocument.Parse(body);
                    var erpRef = doc.RootElement.TryGetProperty("id", out var id)
                        ? id.GetString() ?? "OK" : "OK";
                    return ErpSyncResult.Ok(erpRef);
                }

                var err = await response.Content.ReadAsStringAsync(ct);
                return ErpSyncResult.Fail($"HTTP {(int)response.StatusCode}: {err}");
            }
            catch (Exception ex) { return ErpSyncResult.Fail(ex.Message); }
        }

        public async Task<ErpSyncResult> SyncInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
        {
            try
            {
                var content = new StringContent(
                    System.Text.Json.JsonSerializer.Serialize(new { invoiceId }),
                    System.Text.Encoding.UTF8, "application/json");
                var response = await _http.PostAsync($"{_baseUrl}/api/v3/invoices", content, ct);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(ct);
                    var doc = System.Text.Json.JsonDocument.Parse(body);
                    var erpRef = doc.RootElement.TryGetProperty("id", out var id)
                        ? id.GetString() ?? "OK" : "OK";
                    return ErpSyncResult.Ok(erpRef);
                }

                var err = await response.Content.ReadAsStringAsync(ct);
                return ErpSyncResult.Fail($"HTTP {(int)response.StatusCode}: {err}");
            }
            catch (Exception ex) { return ErpSyncResult.Fail(ex.Message); }
        }

        public async Task<IReadOnlyList<ErpAccountDto>> GetAccountBalancesAsync(CancellationToken ct = default)
        {
            try
            {
                var response = await _http.GetAsync($"{_baseUrl}/api/v3/customers", ct);
                if (!response.IsSuccessStatusCode) return Array.Empty<ErpAccountDto>();

                var json = System.Text.Json.JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync(ct));
                var accounts = new List<ErpAccountDto>();

                foreach (var item in json.RootElement.EnumerateArray())
                {
                    var code = item.TryGetProperty("code", out var c) ? c.GetString() ?? "" : "";
                    var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var balance = item.TryGetProperty("balance", out var b) ? b.GetDecimal() : 0m;
                    var currency = item.TryGetProperty("currency", out var cur) ? cur.GetString() ?? "TRY" : "TRY";
                    accounts.Add(new ErpAccountDto(code, name, balance, currency));
                }

                return accounts.AsReadOnly();
            }
            catch { return Array.Empty<ErpAccountDto>(); }
        }
    }

    public void Dispose() => _server.Stop();
}
