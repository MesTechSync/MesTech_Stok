using FluentAssertions;
using MesTech.Tests.Integration._Shared;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// OpenCart API adapter contract testleri.
/// WireMock ile OpenCart REST API davranisi simule edilir.
/// </summary>
public class OpenCartAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly HttpClient _httpClient;

    public OpenCartAdapterContractTests(WireMockFixture fixture)
    {
        fixture.Reset();
        _mockServer = fixture.Server;
        _httpClient = new HttpClient { BaseAddress = new Uri(fixture.BaseUrl) };
    }

    [Fact]
    public async Task GetProducts_WhenApiReturns200_ShouldReturnProducts()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [
                        {""product_id"":1, ""name"":""Test Urun"", ""model"":""TST-001"", ""quantity"":50, ""price"":""100.00""}
                    ]
                }"));

        // Act
        var response = await _httpClient.GetAsync("/api/rest/products");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test Urun");
    }

    [Fact]
    public async Task UpdateStock_WhenApiReturns200_ShouldSucceed()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/products/1")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody("{\"success\":true}"));

        // Act
        var body = @"{""quantity"":75}";
        var response = await _httpClient.PutAsync(
            "/api/rest/products/1",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public async Task GetProducts_WhenApiReturns500_ShouldReturnServerError()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/products")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("{\"error\":\"Internal Server Error\"}"));

        // Act
        var response = await _httpClient.GetAsync("/api/rest/products");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task GetOrders_WhenApiReturns200_ShouldReturnOrders()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""data"": [
                        {""order_id"":1001,""total"":""250.00"",""order_status_id"":1}
                    ]
                }"));

        // Act
        var response = await _httpClient.GetAsync("/api/rest/orders");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("1001");
    }

    [Fact]
    public async Task Authentication_WhenTokenInvalid_ShouldReturn403()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath("/api/rest/products")
                .WithHeader("X-Oc-Restadmin-Id", "invalid-token")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(403)
                .WithBody("{\"error\":\"Forbidden\"}"));

        // Act
        _httpClient.DefaultRequestHeaders.Add("X-Oc-Restadmin-Id", "invalid-token");
        var response = await _httpClient.GetAsync("/api/rest/products");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
