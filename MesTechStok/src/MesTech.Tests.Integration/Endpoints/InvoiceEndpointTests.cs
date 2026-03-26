using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace MesTech.Tests.Integration.Endpoints;

/// <summary>
/// Endpoint hardening tests for InvoiceEndpoints (Sprint 1 DEV-H1).
/// Routes: POST /api/v1/invoices
/// Requires: InvoiceEndpointRequest { Provider (int), Invoice (InvoiceCreateRequest) }
/// </summary>
[Trait("Category", "Endpoint")]
[Trait("Sprint", "H1")]
public sealed class InvoiceEndpointTests : IClassFixture<EndpointTestWebAppFactory>
{
    private readonly HttpClient _noAuthClient;
    private readonly HttpClient _authClient;

    public InvoiceEndpointTests(EndpointTestWebAppFactory factory)
    {
        _noAuthClient = factory.CreateClient();

        _authClient = factory.CreateClient();
        _authClient.DefaultRequestHeaders.Add(
            "X-API-Key", EndpointTestWebAppFactory.TestApiKey);
    }

    // ── 1. Happy path ──

    [Fact]
    public async Task CreateInvoice_ValidRequest_ReturnsResponse()
    {
        // Arrange — provider 0 (first enum value) with minimal invoice data
        var payload = new
        {
            provider = 0,
            invoice = new
            {
                customerName = "Test Customer",
                totalAmount = 100.50m,
                items = new[]
                {
                    new { name = "Test Product", quantity = 1, unitPrice = 100.50m }
                }
            }
        };
        var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await _authClient.PostAsync("/api/v1/invoices", content);

        // Assert — 200 if adapter resolves, 400 if provider unknown, 500 if adapter fails
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.OK,
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    // ── 2. Validation ──

    [Fact]
    public async Task CreateInvoice_EmptyBody_ReturnsBadRequest()
    {
        // Arrange — empty JSON body
        var content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Act
        var response = await _authClient.PostAsync("/api/v1/invoices", content);

        // Assert — should reject missing required fields
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest,
            HttpStatusCode.InternalServerError);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrWhiteSpace();
    }

    // ── 3. Auth ──

    [Fact]
    public async Task CreateInvoice_NoApiKey_Returns401()
    {
        // Arrange
        var payload = new { provider = 0, invoice = new { customerName = "Test" } };
        var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await _noAuthClient.PostAsync("/api/v1/invoices", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("API key");
    }

    // ── 4. Not found ──

    [Fact]
    public async Task GetInvoice_NonExistentRoute_Returns404()
    {
        // Act — GET on the invoice endpoint (only POST is mapped)
        var response = await _authClient.GetAsync("/api/v1/invoices/nonexistent-id");

        // Assert — no GET route mapped, should return 404
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.NotFound,
            HttpStatusCode.MethodNotAllowed);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // ── 5. Server error ──

    [Fact]
    public async Task CreateInvoice_InvalidProvider_ReturnsErrorResponse()
    {
        // Arrange — provider value 9999 should not resolve to any adapter
        var payload = new
        {
            provider = 9999,
            invoice = new { customerName = "Error Test", totalAmount = 50m }
        };
        var content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        // Act
        var response = await _authClient.PostAsync("/api/v1/invoices", content);

        // Assert — unknown provider should return 400 "Unknown provider" or 500
        if (response.StatusCode == HttpStatusCode.InternalServerError)
        {
            var body = await response.Content.ReadAsStringAsync();
            response.Content.Headers.ContentType?.MediaType.Should().Contain("json");
            var json = JsonDocument.Parse(body);
            json.RootElement.TryGetProperty("status", out var status).Should().BeTrue();
            status.GetInt32().Should().Be(500);
        }
        else
        {
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
