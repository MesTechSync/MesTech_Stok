using System.Net;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace MesTech.Tests.Unit.Adapters;

/// <summary>
/// DEV3 TUR7-FULL: Tests for 9 new TrendyolAdapter methods
/// (UpdateProduct, DeleteProduct, CancelPackage, GetShipmentProviders,
///  GetSellerAddresses, GetTrackingDetails, ListWebhooks, GetCurrentAccount, GetClaimAudit).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Trendyol")]
public class TrendyolAdapterNewMethodTests : IDisposable
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly HttpClient _httpClient;
    private readonly TrendyolAdapter _sut;

    public TrendyolAdapterNewMethodTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Loose);

        // TestConnectionAsync için varsayılan response — connection init
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"totalElements": 1, "totalPages": 1, "content": []}""",
                    Encoding.UTF8, "application/json")
            });

        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("https://apigw.trendyol.com")
        };
        var options = Options.Create(new TrendyolOptions());
        _sut = new TrendyolAdapter(_httpClient, NullLogger<TrendyolAdapter>.Instance, options);

        _sut.TestConnectionAsync(new Dictionary<string, string>
        {
            ["ApiKey"] = "test", ["ApiSecret"] = "test", ["SupplierId"] = "12345",
            ["BaseUrl"] = "https://apigw.trendyol.com"
        }).GetAwaiter().GetResult();
    }

    public void Dispose() => _httpClient.Dispose();

    private void SetupResponse(HttpStatusCode status, string body = "{}")
    {
        _mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }

    // ═══ UpdateProduct ═══

    [Fact]
    public async Task UpdateProduct_Success_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK);
        var product = new MesTech.Domain.Entities.Product { SKU = "TST-001", Name = "Test" };
        var result = await _sut.UpdateProductAsync(product);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateProduct_ServerError_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.InternalServerError, """{"errors":["server error"]}""");
        var product = new MesTech.Domain.Entities.Product { SKU = "TST-002", Name = "Test" };
        var result = await _sut.UpdateProductAsync(product);
        result.Should().BeFalse();
    }

    // ═══ DeleteProduct ═══

    [Fact]
    public async Task DeleteProduct_Success_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK);
        var result = await _sut.DeleteProductAsync("8690001234567");
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProduct_NotFound_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.NotFound);
        var result = await _sut.DeleteProductAsync("INVALID");
        result.Should().BeFalse();
    }

    // ═══ CancelPackage ═══

    [Fact]
    public async Task CancelPackage_Success_ReturnsTrue()
    {
        SetupResponse(HttpStatusCode.OK);
        var result = await _sut.CancelPackageAsync(999);
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CancelPackage_Forbidden_ReturnsFalse()
    {
        SetupResponse(HttpStatusCode.Forbidden);
        var result = await _sut.CancelPackageAsync(999);
        result.Should().BeFalse();
    }

    // ═══ GetShipmentProviders ═══

    [Fact]
    public async Task GetShipmentProviders_Success_ReturnsJson()
    {
        SetupResponse(HttpStatusCode.OK, """[{"id":1,"name":"Yurtici Kargo"}]""");
        var result = await _sut.GetShipmentProvidersAsync();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetShipmentProviders_Error_ReturnsNull()
    {
        SetupResponse(HttpStatusCode.InternalServerError);
        var result = await _sut.GetShipmentProvidersAsync();
        result.Should().BeNull();
    }

    // ═══ GetSellerAddresses ═══

    [Fact]
    public async Task GetSellerAddresses_Success_ReturnsJson()
    {
        SetupResponse(HttpStatusCode.OK, """{"supplierAddresses":[{"id":1,"city":"Istanbul"}]}""");
        var result = await _sut.GetSellerAddressesAsync();
        result.Should().NotBeNull();
    }

    // ═══ GetTrackingDetails ═══

    [Fact]
    public async Task GetTrackingDetails_Success_ReturnsJson()
    {
        SetupResponse(HttpStatusCode.OK, """{"trackingEvents":[{"date":"2026-04-01","status":"Delivered"}]}""");
        var result = await _sut.GetTrackingDetailsAsync(12345);
        result.Should().NotBeNull();
    }

    // ═══ ListWebhooks ═══

    [Fact]
    public async Task ListWebhooks_Success_ReturnsJson()
    {
        SetupResponse(HttpStatusCode.OK, """{"webhooks":[{"url":"https://mestech.app/webhook"}]}""");
        var result = await _sut.ListWebhooksAsync();
        result.Should().NotBeNull();
    }

    // ═══ GetCurrentAccount ═══

    [Fact]
    public async Task GetCurrentAccount_Success_ReturnsJson()
    {
        SetupResponse(HttpStatusCode.OK, """{"id":12345,"name":"Test Seller","status":"Active"}""");
        var result = await _sut.GetCurrentAccountAsync();
        result.Should().NotBeNull();
    }

    // ═══ GetClaimAudit ═══

    [Fact]
    public async Task GetClaimAudit_Success_ReturnsJson()
    {
        SetupResponse(HttpStatusCode.OK, """{"claimId":555,"status":"Approved","history":[]}""");
        var result = await _sut.GetClaimAuditAsync(555);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetClaimAudit_NotFound_ReturnsNull()
    {
        SetupResponse(HttpStatusCode.NotFound);
        var result = await _sut.GetClaimAuditAsync(999);
        result.Should().BeNull();
    }
}
