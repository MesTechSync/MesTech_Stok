using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs.Cargo;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Adapters;

/// <summary>
/// DEV 5 — HepsiburadaAdapter new endpoint unit tests.
/// 2 tests per method (happy path + error path).
/// Uses Moq HttpMessageHandler for mocking HTTP calls.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Hepsiburada")]
public class HepsiburadaAdapterEndpointTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HepsiburadaAdapter> _logger;
    private readonly HepsiburadaAdapter _sut;
    private readonly JsonSerializerOptions _jsonOptions;

    public HepsiburadaAdapterEndpointTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://api.hepsiburada.com/")
        };
        _logger = NullLogger<HepsiburadaAdapter>.Instance;
        _sut = new HepsiburadaAdapter(_httpClient, _logger);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Configure the adapter via TestConnectionAsync mock
        ConfigureAdapter();
    }

    private void ConfigureAdapter()
    {
        // Mock the TestConnectionAsync response to configure the adapter
        SetupMockResponse(HttpStatusCode.OK,
            JsonSerializer.Serialize(new { listings = Array.Empty<object>(), totalCount = 0 }, _jsonOptions));

        _sut.TestConnectionAsync(new Dictionary<string, string>
        {
            ["MerchantId"] = "test-merchant",
            ["ApiKey"] = "test-api-key"
        }).GetAwaiter().GetResult();
    }

    private void SetupMockResponse(HttpStatusCode statusCode, string content)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }

    private void SetupMockResponseForUrl(HttpStatusCode statusCode, string content, string urlContains)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }

    private void SetupMockBytesResponse(HttpStatusCode statusCode, byte[] content, string urlContains)
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.ToString().Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new ByteArrayContent(content)
            });
    }

    #region GetClaimsAsync

    [Fact]
    public async Task GetClaimsAsync_Success_ReturnsClaims()
    {
        var claims = new HbClaimListResponse
        {
            Claims = new List<HbClaimDto>
            {
                new("CLM-001", "ORD-001", "Defective", "Open", 150.00m),
                new("CLM-002", "ORD-002", "Wrong item", "Open", 85.50m)
            },
            TotalCount = 2
        };
        SetupMockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(claims, _jsonOptions));

        var result = await _sut.GetClaimsAsync();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be("CLM-001");
        result[1].Amount.Should().Be(85.50m);
    }

    [Fact]
    public async Task GetClaimsAsync_ServerError_ReturnsEmptyList()
    {
        SetupMockResponse(HttpStatusCode.InternalServerError, "{\"error\":\"internal\"}");

        var result = await _sut.GetClaimsAsync();

        result.Should().BeEmpty();
    }

    #endregion

    #region ApproveClaimAsync

    [Fact]
    public async Task ApproveClaimAsync_Success_ReturnsTrue()
    {
        SetupMockResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.ApproveClaimAsync("CLM-001");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ApproveClaimAsync_NotFound_ReturnsFalse()
    {
        SetupMockResponse(HttpStatusCode.NotFound, "{\"error\":\"not found\"}");

        var result = await _sut.ApproveClaimAsync("CLM-INVALID");

        result.Should().BeFalse();
    }

    #endregion

    #region RejectClaimAsync

    [Fact]
    public async Task RejectClaimAsync_Success_ReturnsTrue()
    {
        SetupMockResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.RejectClaimAsync("CLM-001", "Not eligible");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RejectClaimAsync_BadRequest_ReturnsFalse()
    {
        SetupMockResponse(HttpStatusCode.BadRequest, "{\"error\":\"invalid reason\"}");

        var result = await _sut.RejectClaimAsync("CLM-001", "");

        result.Should().BeFalse();
    }

    #endregion

    #region ActivateListingAsync

    [Fact]
    public async Task ActivateListingAsync_Success_ReturnsTrue()
    {
        SetupMockResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.ActivateListingAsync("SKU-001");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ActivateListingAsync_NotFound_ReturnsFalse()
    {
        SetupMockResponse(HttpStatusCode.NotFound, "{\"error\":\"SKU not found\"}");

        var result = await _sut.ActivateListingAsync("SKU-INVALID");

        result.Should().BeFalse();
    }

    #endregion

    #region DeactivateListingAsync

    [Fact]
    public async Task DeactivateListingAsync_Success_ReturnsTrue()
    {
        SetupMockResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.DeactivateListingAsync("SKU-001");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeactivateListingAsync_Forbidden_ReturnsFalse()
    {
        SetupMockResponse(HttpStatusCode.Forbidden, "{\"error\":\"not authorized\"}");

        var result = await _sut.DeactivateListingAsync("SKU-001");

        result.Should().BeFalse();
    }

    #endregion

    #region CheckUploadStatusAsync

    [Fact]
    public async Task CheckUploadStatusAsync_Success_ReturnsStatus()
    {
        var status = new HbUploadStatusDto("CORR-001", "Completed", 100, 98, 2,
            new List<string> { "SKU-X: invalid barcode" });
        SetupMockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(status, _jsonOptions));

        var result = await _sut.CheckUploadStatusAsync("CORR-001");

        result.Should().NotBeNull();
        result!.Status.Should().Be("Completed");
        result.TotalItems.Should().Be(100);
        result.SuccessCount.Should().Be(98);
        result.FailureCount.Should().Be(2);
    }

    [Fact]
    public async Task CheckUploadStatusAsync_NotFound_ReturnsNull()
    {
        SetupMockResponse(HttpStatusCode.NotFound, "{\"error\":\"correlation not found\"}");

        var result = await _sut.CheckUploadStatusAsync("CORR-INVALID");

        result.Should().BeNull();
    }

    #endregion

    #region GetCommissionsAsync

    [Fact]
    public async Task GetCommissionsAsync_Success_ReturnsCommissions()
    {
        var commissions = new HbCommissionListResponse
        {
            Commissions = new List<HbCommissionDto>
            {
                new("Elektronik", 8.5m, 425.00m),
                new("Giyim", 12.0m, 600.00m)
            },
            TotalCount = 2
        };
        SetupMockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(commissions, _jsonOptions));

        var result = await _sut.GetCommissionsAsync(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        result.Should().HaveCount(2);
        result[0].Category.Should().Be("Elektronik");
        result[1].Rate.Should().Be(12.0m);
    }

    [Fact]
    public async Task GetCommissionsAsync_Unauthorized_ReturnsEmptyList()
    {
        SetupMockResponse(HttpStatusCode.Unauthorized, "{\"error\":\"invalid token\"}");

        var result = await _sut.GetCommissionsAsync(
            new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        result.Should().BeEmpty();
    }

    #endregion

    #region SendInvoiceAsync

    [Fact]
    public async Task SendInvoiceAsync_Success_ReturnsTrue()
    {
        SetupMockResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.SendInvoiceAsync("ORD-001", "INV-2026-001", new DateTime(2026, 3, 15));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendInvoiceAsync_BadRequest_ReturnsFalse()
    {
        SetupMockResponse(HttpStatusCode.BadRequest, "{\"error\":\"invalid invoice\"}");

        var result = await _sut.SendInvoiceAsync("ORD-INVALID", "INV-X", new DateTime(2026, 3, 15));

        result.Should().BeFalse();
    }

    #endregion

    #region GetCargoLabelAsync

    [Fact]
    public async Task GetCargoLabelAsync_Success_ReturnsPdfBytes()
    {
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
        SetupMockBytesResponse(HttpStatusCode.OK, pdfBytes, "/packages/");

        var result = await _sut.GetCargoLabelAsync("PKG-001");

        result.Should().NotBeNull();
        result!.Length.Should().BeGreaterThan(0);
        result[0].Should().Be(0x25); // PDF magic byte
    }

    [Fact]
    public async Task GetCargoLabelAsync_NotFound_ReturnsNull()
    {
        SetupMockResponse(HttpStatusCode.NotFound, "{\"error\":\"package not found\"}");

        var result = await _sut.GetCargoLabelAsync("PKG-INVALID");

        result.Should().BeNull();
    }

    #endregion

    #region GetShipmentTrackingAsync

    [Fact]
    public async Task GetShipmentTrackingAsync_Success_ReturnsTracking()
    {
        var tracking = new HbTrackingDto(
            "TRK-123456",
            "Yurtici Kargo",
            "InTransit",
            new DateTime(2026, 3, 15, 14, 30, 0),
            new List<HbTrackingEvent>
            {
                new("Kargoya verildi", "Istanbul", new DateTime(2026, 3, 14, 10, 0, 0)),
                new("Dagitima cikti", "Ankara", new DateTime(2026, 3, 15, 8, 0, 0))
            });
        SetupMockResponse(HttpStatusCode.OK, JsonSerializer.Serialize(tracking, _jsonOptions));

        var result = await _sut.GetShipmentTrackingAsync("TRK-123456");

        result.Should().NotBeNull();
        result!.TrackingNumber.Should().Be("TRK-123456");
        result.CargoCompany.Should().Be("Yurtici Kargo");
        result.Status.Should().Be("InTransit");
        result.Events.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetShipmentTrackingAsync_NotFound_ReturnsNull()
    {
        SetupMockResponse(HttpStatusCode.NotFound, "{\"error\":\"tracking not found\"}");

        var result = await _sut.GetShipmentTrackingAsync("TRK-INVALID");

        result.Should().BeNull();
    }

    #endregion

    #region EnsureConfigured Guards

    [Fact]
    public async Task GetClaimsAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var unconfigured = new HepsiburadaAdapter(
            new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://api.hepsiburada.com/") },
            _logger);

        var act = () => unconfigured.GetClaimsAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ActivateListingAsync_Unconfigured_ThrowsInvalidOperationException()
    {
        var unconfigured = new HepsiburadaAdapter(
            new HttpClient(_mockHandler.Object) { BaseAddress = new Uri("https://api.hepsiburada.com/") },
            _logger);

        var act = () => unconfigured.ActivateListingAsync("SKU-001");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
