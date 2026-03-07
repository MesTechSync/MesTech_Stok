using FluentAssertions;
using MesTech.Tests.Integration._Shared;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// Trendyol API adapter contract testleri.
/// WireMock ile Trendyol API davranisi simule edilir.
/// Adapter henuz implement edilmediginden, HTTP client seviyesinde test yapilir.
/// </summary>
public class TrendyolAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly HttpClient _httpClient;

    public TrendyolAdapterContractTests(WireMockFixture fixture)
    {
        fixture.Reset();
        _mockServer = fixture.Server;
        _httpClient = new HttpClient { BaseAddress = new Uri(fixture.BaseUrl) };
    }

    [Fact]
    public async Task SyncProducts_WhenApiReturns200_ShouldReturnSuccess()
    {
        // Arrange — mock Trendyol API
        _mockServer
            .Given(Request.Create()
                .WithPath("/sapigw/suppliers/*/products")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("{\"batchRequestId\":\"batch-123\"}"));

        // Act
        var response = await _httpClient.PostAsync(
            "/sapigw/suppliers/12345/products",
            new StringContent("{\"items\":[]}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("batch-123");
    }

    [Fact]
    public async Task SyncProducts_WhenApiReturns401_ShouldReturnUnauthorized()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/sapigw/suppliers/*/products")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("{\"error\":\"Unauthorized\"}"));

        // Act
        var response = await _httpClient.PostAsync(
            "/sapigw/suppliers/12345/products",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SyncProducts_WhenApiReturns429_ShouldReturnTooManyRequests()
    {
        // Arrange — Rate limit
        _mockServer
            .Given(Request.Create()
                .WithPath("/sapigw/suppliers/*/products")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithHeader("Retry-After", "60")
                .WithBody("{\"error\":\"Too Many Requests\"}"));

        // Act
        var response = await _httpClient.PostAsync(
            "/sapigw/suppliers/12345/products",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GetProducts_WhenApiReturns200_ShouldReturnProducts()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/sapigw/suppliers/*/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""content"": [
                        {""barcode"":""8691234567005"",""title"":""Test Urun"",""stockCode"":""TST-001"",""quantity"":50}
                    ],
                    ""totalElements"": 1,
                    ""totalPages"": 1,
                    ""page"": 0
                }"));

        // Act
        var response = await _httpClient.GetAsync("/sapigw/suppliers/12345/products?page=0&size=50");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test Urun");
        content.Should().Contain("TST-001");
    }

    [Fact]
    public async Task UpdateStock_WhenApiReturns200_ShouldSucceed()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/sapigw/suppliers/*/products/price-and-inventory")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"batchRequestId\":\"stock-update-456\"}"));

        // Act
        var body = @"{""items"":[{""barcode"":""8691234567005"",""quantity"":100}]}";
        var response = await _httpClient.PostAsync(
            "/sapigw/suppliers/12345/products/price-and-inventory",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task ApiTimeout_ShouldBeHandled()
    {
        // Arrange — 5 saniye gecikme
        _mockServer
            .Given(Request.Create()
                .WithPath("/sapigw/suppliers/*/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithDelay(TimeSpan.FromSeconds(5))
                .WithBody("{}"));

        // Act & Assert — kisa timeout ile istek
        using var shortTimeoutClient = new HttpClient
        {
            BaseAddress = new Uri(_mockServer.Url!),
            Timeout = TimeSpan.FromSeconds(2)
        };

        var act = () => shortTimeoutClient.GetAsync("/sapigw/suppliers/12345/products");

        await act.Should().ThrowAsync<TaskCanceledException>();
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
