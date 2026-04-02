using System.Net;
using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

/// <summary>
/// N11Adapter unit tests — SOAP-based marketplace adapter.
/// G487: TestConnection, PullProducts, PullOrders, GetCategories.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "N11")]
public class N11AdapterTests
{
    private readonly MockHttpMessageHandler _handler = new();
    private readonly Mock<ILogger<N11Adapter>> _loggerMock = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();

    private N11Adapter CreateAdapter()
    {
        var httpClient = new HttpClient(_handler)
        {
            BaseAddress = new Uri("https://api.n11.com/ws/")
        };
        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);

        return new N11Adapter(_loggerMock.Object, _httpClientFactoryMock.Object);
    }

    // ─── Properties ───

    [Fact]
    public void PlatformCode_ReturnsN11()
    {
        var adapter = CreateAdapter();
        adapter.PlatformCode.Should().Be(nameof(PlatformType.N11));
    }

    [Fact]
    public void SupportsStockUpdate_ReturnsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsStockUpdate.Should().BeTrue();
    }

    [Fact]
    public void SupportsPriceUpdate_ReturnsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsPriceUpdate.Should().BeTrue();
    }

    [Fact]
    public void SupportsShipment_ReturnsTrue()
    {
        var adapter = CreateAdapter();
        adapter.SupportsShipment.Should().BeTrue();
    }

    // ─── TestConnection ───

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange — SOAP response for GetProductList
        _handler.EnqueueResponse(HttpStatusCode.OK, """
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                <soap:Body>
                    <GetProductListResponse xmlns="http://www.n11.com/ws/ProductService">
                        <result><status>success</status><errorCode/></result>
                        <products/>
                        <pagingData><currentPage>0</currentPage><totalCount>0</totalCount></pagingData>
                    </GetProductListResponse>
                </soap:Body>
            </soap:Envelope>
        """);

        var adapter = CreateAdapter();
        var credentials = new Dictionary<string, string>
        {
            ["AppKey"] = "test-app-key",
            ["AppSecret"] = "test-app-secret",
            ["SoapBaseUrl"] = "https://api.n11.com/ws/"
        };

        // Act
        var result = await adapter.TestConnectionAsync(credentials);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_MissingCredentials_ReturnsFailed()
    {
        var adapter = CreateAdapter();
        var credentials = new Dictionary<string, string>(); // empty

        // Act
        var result = await adapter.TestConnectionAsync(credentials);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
    }

    // ─── PullProducts without Configure ───

    [Fact]
    public async Task PullProductsAsync_NotConfigured_ThrowsOrReturnsEmpty()
    {
        var adapter = CreateAdapter();

        // Act & Assert — adapter not configured, should handle gracefully
        var action = async () => await adapter.PullProductsAsync();
        await action.Should().ThrowAsync<Exception>();
    }

    // ─── GetCategories without Configure ───

    [Fact]
    public async Task GetCategoriesAsync_NotConfigured_ThrowsOrReturnsEmpty()
    {
        var adapter = CreateAdapter();

        // Act & Assert
        var action = async () => await adapter.GetCategoriesAsync();
        await action.Should().ThrowAsync<Exception>();
    }
}
