using System.Net.Http;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// HepsiburadaAdapter entegrasyon testleri.
/// WireMock ile gercek HepsiburadaAdapter sinifi test edilir.
/// Auth: Bearer MerchantId:ApiKey
/// Listings: GET /listings/merchantid/{MerchantId}?limit=50&amp;offset=0
/// Orders: GET /orders/merchantid/{MerchantId}?limit=50&amp;offset=0
/// Shipment: POST /packages/{packageId}/shipment
/// </summary>
[Trait("Category", "Integration")]
public class HepsiburadaAdapterContractTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _mockServer;
    private readonly ILogger<HepsiburadaAdapter> _logger;
    private const string MerchantId = "HB-TEST-123";

    private static readonly Dictionary<string, string> ValidCredentials = new()
    {
        ["MerchantId"] = MerchantId,
        ["ApiKey"] = "test-hb-api-key"
    };

    public HepsiburadaAdapterContractTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _mockServer = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<HepsiburadaAdapter>();
    }

    private HepsiburadaAdapter CreateAdapter()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri(_fixture.BaseUrl) };
        return new HepsiburadaAdapter(httpClient, _logger);
    }

    /// <summary>
    /// Adapter'i yapilandirip kullanima hazir hale getirir.
    /// TestConnectionAsync basarili olursa _isConfigured = true olur.
    /// </summary>
    private async Task<HepsiburadaAdapter> CreateConfiguredAdapterAsync()
    {
        _mockServer
            .Given(Request.Create()
                .WithPath($"/listings/merchantid/{MerchantId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalCount"":0,""listings"":[]}"));

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(ValidCredentials);
        _fixture.Reset();
        return adapter;
    }

    // ══════════════════════════════════════
    // 1. TestConnectionAsync Tests
    // ══════════════════════════════════════

    [Fact]
    public async Task TestConnectionAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"/listings/merchantid/{MerchantId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{""totalCount"":18,""listings"":[]}"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Hepsiburada");
        result.ProductCount.Should().Be(18);
        result.HttpStatusCode.Should().Be(200);
        result.ErrorMessage.Should().BeNull();
        result.ResponseTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task TestConnectionAsync_MissingMerchantId_ReturnsError()
    {
        // Arrange — no WireMock stub needed, should fail before HTTP call
        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(new Dictionary<string, string> { ["ApiKey"] = "key" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("MerchantId");
        result.HttpStatusCode.Should().BeNull();
        _mockServer.LogEntries.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 2. PullProductsAsync — Banned listing skip
    // ══════════════════════════════════════

    [Fact]
    public async Task PullProductsAsync_SkipsBannedListings()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/listings/merchantid/{MerchantId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""totalCount"": 3,
                    ""listings"": [
                        {""hepsiburadaSku"":""HB1"",""merchantSku"":""M1"",""productName"":""Active Product"",""price"":100,""availableStock"":10,""listingStatus"":""Active"",""commissionRate"":8},
                        {""hepsiburadaSku"":""HB2"",""merchantSku"":""M2"",""productName"":""Passive Product"",""price"":200,""availableStock"":5,""listingStatus"":""Passive"",""commissionRate"":8},
                        {""hepsiburadaSku"":""HB3"",""merchantSku"":""M3"",""productName"":""Banned Product"",""price"":50,""availableStock"":0,""listingStatus"":""Banned"",""commissionRate"":8}
                    ]
                }"));

        // Act
        var products = await adapter.PullProductsAsync();

        // Assert — Active + Passive only, Banned is skipped
        products.Should().HaveCount(2);
        products.Should().Contain(p => p.Name == "Active Product" && p.IsActive);
        products.Should().Contain(p => p.Name == "Passive Product" && !p.IsActive);
    }

    // ══════════════════════════════════════
    // 3. PullOrdersAsync — Package-based mapping
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrdersAsync_MapsPackageBasedOrders()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath($"/orders/merchantid/{MerchantId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(@"{
                    ""totalCount"": 1,
                    ""orders"": [{
                        ""orderNumber"": ""HB-ORD-001"",
                        ""status"": ""Open"",
                        ""orderDate"": ""2026-03-01T12:00:00"",
                        ""customerName"": ""Mehmet Demir"",
                        ""totalAmount"": 150.00,
                        ""packageNumber"": ""PKG-001"",
                        ""lines"": [{
                            ""merchantSku"": ""M1"",
                            ""hepsiburadaSku"": ""HB1"",
                            ""productName"": ""Test Urun"",
                            ""quantity"": 2,
                            ""unitPrice"": 75.00,
                            ""totalPrice"": 150.00,
                            ""commissionRate"": 8
                        }]
                    }]
                }"));

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders.Should().HaveCount(1);
        orders[0].OrderNumber.Should().Be("HB-ORD-001");
        orders[0].ShipmentPackageId.Should().Be("PKG-001");
        orders[0].TotalAmount.Should().Be(150m);
        orders[0].CustomerName.Should().Be("Mehmet Demir");
        orders[0].Lines.Should().HaveCount(1);
        orders[0].Lines[0].SKU.Should().Be("M1");
    }

    // ══════════════════════════════════════
    // 4. SendShipmentAsync Test
    // ══════════════════════════════════════

    [Fact]
    public async Task SendShipmentAsync_PostsToPackageEndpoint()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/packages/PKG-001/shipment")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // Act
        var result = await adapter.SendShipmentAsync("PKG-001", "AK123456789", CargoProvider.ArasKargo);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // 5. TestConnectionAsync — Missing ApiKey
    // ══════════════════════════════════════

    [Fact]
    public async Task TestConnectionAsync_MissingApiKey_ReturnsError()
    {
        // Arrange — no WireMock stub needed, should fail before HTTP call
        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(
            new Dictionary<string, string> { ["MerchantId"] = MerchantId });

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ApiKey");
        result.HttpStatusCode.Should().BeNull();
        _mockServer.LogEntries.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 6. TestConnectionAsync — Unauthorized (401)
    // ══════════════════════════════════════

    [Fact]
    public async Task TestConnectionAsync_Unauthorized_ReturnsError()
    {
        // Arrange
        _mockServer
            .Given(Request.Create()
                .WithPath($"/listings/merchantid/{MerchantId}")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithBody("Unauthorized"));

        var adapter = CreateAdapter();

        // Act
        var result = await adapter.TestConnectionAsync(ValidCredentials);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.HttpStatusCode.Should().Be(401);
        result.ErrorMessage.Should().Contain("Yetkisiz");
    }

    // ══════════════════════════════════════
    // 7. PushStockUpdateAsync — Success
    // ══════════════════════════════════════

    [Fact]
    public async Task PushStockUpdateAsync_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/listings/and-inventory")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // Act
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 25);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // 8. PushStockUpdateAsync — Server Error
    // ══════════════════════════════════════

    [Fact]
    public async Task PushStockUpdateAsync_ServerError_ReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/listings/and-inventory")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 10);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 9. PushPriceUpdateAsync — Success
    // ══════════════════════════════════════

    [Fact]
    public async Task PushPriceUpdateAsync_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/listings/and-inventory")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // Act
        var result = await adapter.PushPriceUpdateAsync(Guid.NewGuid(), 149.99m);

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // 10. PushPriceUpdateAsync — Server Error
    // ══════════════════════════════════════

    [Fact]
    public async Task PushPriceUpdateAsync_ServerError_ReturnsFalse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/listings/and-inventory")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error"));

        // Act
        var result = await adapter.PushPriceUpdateAsync(Guid.NewGuid(), 99.90m);

        // Assert
        result.Should().BeFalse();
    }

    // ══════════════════════════════════════
    // 11. PushProductAsync — Not Supported
    // ══════════════════════════════════════

    [Fact]
    public async Task PushProductAsync_NotSupported_ReturnsFalse()
    {
        // Arrange — no WireMock stub needed, HB does not support product push
        var adapter = CreateAdapter();
        var product = new Product { Name = "Test", SKU = "TST-001", SalePrice = 50m, Stock = 5 };

        // Act
        var result = await adapter.PushProductAsync(product);

        // Assert
        result.Should().BeFalse();
        _mockServer.LogEntries.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 12. UpdateOrderStatusAsync — Success
    // ══════════════════════════════════════

    [Fact]
    public async Task UpdateOrderStatusAsync_Success_ReturnsTrue()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();

        _mockServer
            .Given(Request.Create()
                .WithPath("/packages/PKG-001/status")
                .UsingPut())
            .RespondWith(Response.Create()
                .WithStatusCode(200));

        // Act
        var result = await adapter.UpdateOrderStatusAsync("PKG-001", "Shipped");

        // Assert
        result.Should().BeTrue();
    }

    // ══════════════════════════════════════
    // 13. GetCategoriesAsync — Returns Empty
    // ══════════════════════════════════════

    [Fact]
    public async Task GetCategoriesAsync_ReturnsEmptyList()
    {
        // Arrange — no WireMock stub needed, HB returns empty list directly
        var adapter = CreateAdapter();
        IIntegratorAdapter iface = adapter;

        // Act
        var categories = await iface.GetCategoriesAsync();

        // Assert
        categories.Should().NotBeNull();
        categories.Should().BeEmpty();
        _mockServer.LogEntries.Should().BeEmpty();
    }

    // ══════════════════════════════════════
    // 14. EnsureConfigured — Throws
    // ══════════════════════════════════════

    [Fact]
    public async Task EnsureConfigured_ThrowsWhenNotConfigured()
    {
        // Arrange — unconfigured adapter
        var adapter = CreateAdapter();

        // Act
        Func<Task> act = () => adapter.PullProductsAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*konfigure*");
    }

    public void Dispose()
    {
        _fixture.Reset();
    }
}
