using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Tests.Integration._Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// DEV 5 — TrendyolAdapter.PullOrdersAsync mapping doğrulaması.
/// Gerçek Trendyol API response formatıyla WireMock testi.
/// KÇ-12: invoiceAddress, grossAmount, totalDiscount, line-level TaxRate.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Platform", "Trendyol")]
public class TrendyolPullOrdersMappingTests : IClassFixture<WireMockFixture>, IDisposable
{
    private readonly WireMockFixture _fixture;
    private readonly WireMockServer _server;
    private readonly ILogger<TrendyolAdapter> _logger;

    private const string SupplierId = "99002";

    private static readonly Dictionary<string, string> Credentials = new()
    {
        ["ApiKey"] = "order-map-key",
        ["ApiSecret"] = "order-map-secret",
        ["SupplierId"] = SupplierId
    };

    /// <summary>
    /// Full Trendyol order response with ALL fields the real API returns.
    /// grossAmount, totalDiscount, invoiceAddress (with taxNumber/company),
    /// shipmentAddress, line-level vatRate/vatBaseAmount, lastModifiedDate, currencyCode.
    /// </summary>
    private const string FullOrderResponse = """
    {
        "totalElements": 2,
        "totalPages": 1,
        "page": 0,
        "size": 50,
        "content": [
            {
                "orderNumber": "TY-ORD-MAP-001",
                "status": "Created",
                "orderDate": 1773324600000,
                "lastModifiedDate": 1773325200000,
                "totalPrice": 549.70,
                "grossAmount": 569.60,
                "totalDiscount": 19.90,
                "currencyCode": "TRY",
                "shipmentPackageId": 88001,
                "cargoProviderName": "Yurtici Kargo",
                "cargoTrackingNumber": "YK-MAP-001",
                "customerFirstName": "Ahmet",
                "customerLastName": "Yilmaz",
                "customerEmail": "ahmet@example.com",
                "invoiceAddress": {
                    "fullAddress": "Ataturk Cad. No:42 Kadikoy",
                    "city": "Istanbul",
                    "district": "Kadikoy",
                    "postalCode": "34710",
                    "phone": "05321234567",
                    "taxNumber": "1234567890",
                    "company": "Yilmaz Ticaret"
                },
                "shipmentAddress": {
                    "fullAddress": "Bagdat Cad. No:100 Maltepe",
                    "city": "Istanbul",
                    "district": "Maltepe",
                    "postalCode": "34840",
                    "phone": "05321234567"
                },
                "lines": [
                    {
                        "id": 400001,
                        "merchantSku": "TY-TSH-001",
                        "barcode": "8691234567001",
                        "productName": "Pamuklu Oversize T-Shirt Beyaz",
                        "quantity": 2,
                        "price": 149.90,
                        "discount": 0,
                        "amount": 299.80,
                        "vatBaseAmount": 272.55,
                        "vatRate": 10
                    },
                    {
                        "id": 400002,
                        "merchantSku": "TY-KT-005",
                        "barcode": "8691234567005",
                        "productName": "Paslanmaz Celik Termos 500ml",
                        "quantity": 1,
                        "price": 199.90,
                        "discount": 19.90,
                        "amount": 180.00,
                        "vatBaseAmount": 152.54,
                        "vatRate": 18
                    }
                ]
            },
            {
                "orderNumber": "TY-ORD-MAP-002",
                "status": "Shipped",
                "orderDate": 1773411000000,
                "lastModifiedDate": 1773500000000,
                "totalPrice": 749.90,
                "grossAmount": 749.90,
                "totalDiscount": 0,
                "currencyCode": "TRY",
                "shipmentPackageId": 88002,
                "cargoProviderName": "MNG Kargo",
                "cargoTrackingNumber": "MNG-MAP-002",
                "customerFirstName": "Mehmet",
                "customerLastName": "Kaya",
                "customerEmail": "mehmet@example.com",
                "invoiceAddress": {
                    "fullAddress": "Alsancak Mah. Kibris Sehitleri Cad. No:5",
                    "city": "Izmir",
                    "district": "Konak",
                    "postalCode": "35220",
                    "phone": "05441112233",
                    "taxNumber": "9876543210",
                    "company": "Kaya Insaat Ltd."
                },
                "shipmentAddress": {
                    "fullAddress": "Alsancak Mah. Kibris Sehitleri Cad. No:5",
                    "city": "Izmir",
                    "district": "Konak",
                    "postalCode": "35220",
                    "phone": "05441112233"
                },
                "lines": [
                    {
                        "id": 400003,
                        "merchantSku": "TY-SH-003",
                        "barcode": "8691234567003",
                        "productName": "Spor Ayakkabi Hafif Kosu Modeli",
                        "quantity": 1,
                        "price": 749.90,
                        "discount": 0,
                        "amount": 749.90,
                        "vatBaseAmount": 681.73,
                        "vatRate": 10
                    }
                ]
            }
        ]
    }
    """;

    public TrendyolPullOrdersMappingTests(WireMockFixture fixture)
    {
        _fixture = fixture;
        _fixture.Reset();
        _server = fixture.Server;
        _logger = new LoggerFactory().CreateLogger<TrendyolAdapter>();
    }

    private TrendyolAdapter CreateAdapter()
    {
        var opts = Options.Create(new TrendyolOptions
        {
            ProductionBaseUrl = _fixture.BaseUrl,
            UseSandbox = false
        });
        return new TrendyolAdapter(new HttpClient(), _logger, opts);
    }

    private async Task<TrendyolAdapter> CreateConfiguredAdapterAsync()
    {
        _server
            .Given(Request.Create()
                .WithPath($"/integration/product/sellers/{SupplierId}/products")
                .WithParam("page", "0")
                .WithParam("size", "1")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"content":[],"totalElements":0,"totalPages":0,"page":0}"""));

        var adapter = CreateAdapter();
        await adapter.TestConnectionAsync(Credentials);
        _fixture.Reset();
        return adapter;
    }

    // ══════════════════════════════════════
    // 1. grossAmount — brüt tutar mapping
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrders_GrossAmount_ShouldMapFromResponse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — grossAmount adapter'da parse ediliyor (satır 737)
        orders.Should().HaveCount(2);
        orders[0].GrossAmount.Should().Be(569.60m,
            "Trendyol grossAmount should map to ExternalOrderDto.GrossAmount");
        orders[1].GrossAmount.Should().Be(749.90m);
    }

    [Fact]
    public async Task PullOrders_TotalDiscount_ShouldMapFromResponse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders[0].TotalDiscount.Should().Be(19.90m,
            "Trendyol totalDiscount should map to ExternalOrderDto.TotalDiscount");
        orders[1].TotalDiscount.Should().Be(0m);
    }

    [Fact]
    public async Task PullOrders_GrossMinusDiscount_ShouldEqualTotalPrice()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — muhasebe kontrolü: gross - discount = total
        var order1 = orders[0];
        (order1.GrossAmount!.Value - order1.TotalDiscount!.Value)
            .Should().Be(order1.TotalAmount,
                "grossAmount - totalDiscount should equal totalPrice (net)");
    }

    // ══════════════════════════════════════
    // 2. invoiceAddress — fatura adresi (GAP BELGELEME)
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrders_ShipmentAddress_ShouldMapToCustomerAddress()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — shipmentAddress adapter'da parse ediliyor (satır 772-778)
        orders[0].CustomerAddress.Should().Be("Bagdat Cad. No:100 Maltepe",
            "shipmentAddress.fullAddress should map to CustomerAddress");
        orders[0].CustomerCity.Should().Be("Istanbul",
            "shipmentAddress.city should map to CustomerCity");
    }

    [Fact]
    public async Task PullOrders_InvoiceAddress_TaxNumber_ShouldBeParsed()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — adapter invoiceAddress.taxNumber'ı parse EDİYOR
        orders[0].CustomerTaxNumber.Should().Be("1234567890",
            "invoiceAddress.taxNumber should map to CustomerTaxNumber for e-invoice");
        orders[1].CustomerTaxNumber.Should().Be("9876543210",
            "kurumsal müşteri vergi no mapped correctly");
    }

    [Fact]
    public async Task PullOrders_InvoiceAddress_FullAddress_NotInDto_DocumentsGap()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — CustomerAddress = shipmentAddress, NOT invoiceAddress
        // GAP: fatura adresi ve kargo adresi farklı olabilir
        // Order 1: invoiceAddress=Kadikoy, shipmentAddress=Maltepe
        orders[0].CustomerAddress.Should().Be("Bagdat Cad. No:100 Maltepe",
            "CustomerAddress maps from shipmentAddress, NOT invoiceAddress — " +
            "separate InvoiceAddress field needed for UBL-TR e-fatura");
    }

    // ══════════════════════════════════════
    // 3. Line-level TaxRate (vatRate) — GAP BELGELEME
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrders_LineTaxRate_ShouldBeCalculatedFromVatBaseAmount()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — adapter vatBaseAmount/amount oranından TaxRate hesaplıyor
        // Line 1: vatRate=10 (API'den), vatBaseAmount=272.55, amount=299.80
        // Line 2: vatRate=18 (API'den), vatBaseAmount=152.54, amount=180.00
        orders[0].Lines[0].TaxRate.Should().BeGreaterThan(0m,
            "line-level TaxRate should be calculated from vatBaseAmount ratio");
        orders[0].Lines[1].TaxRate.Should().BeGreaterThan(0m,
            "second line TaxRate should also be non-zero");
    }

    [Fact]
    public async Task PullOrders_LineTaxRate_ShouldMatchVatRateExactly()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — DEV1 fix (commit 29c7f904): adapter artık vatRate/100 kullanıyor
        // Line 1: vatRate=10 → TaxRate=0.10
        // Line 2: vatRate=18 → TaxRate=0.18
        orders[0].Lines[0].TaxRate.Should().Be(0.10m,
            "vatRate 10 should map to TaxRate 0.10 (vatRate/100 — fixed by DEV1)");
        orders[0].Lines[1].TaxRate.Should().Be(0.18m,
            "vatRate 18 should map to TaxRate 0.18");
    }

    // ══════════════════════════════════════
    // 4. Temel sipariş alanları (çalışan kısımlar)
    // ══════════════════════════════════════

    [Fact]
    public async Task PullOrders_CoreFields_ShouldMapCorrectly()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — Order 1
        orders[0].PlatformOrderId.Should().Be("TY-ORD-MAP-001");
        orders[0].PlatformCode.Should().Be("Trendyol");
        orders[0].Status.Should().Be("Created");
        orders[0].TotalAmount.Should().Be(549.70m);
        orders[0].ShipmentPackageId.Should().Be("88001");
        orders[0].CargoProviderName.Should().Be("Yurtici Kargo");
        orders[0].CargoTrackingNumber.Should().Be("YK-MAP-001");
        orders[0].CustomerEmail.Should().Be("ahmet@example.com");

        // Assert — Order 2
        orders[1].PlatformOrderId.Should().Be("TY-ORD-MAP-002");
        orders[1].Status.Should().Be("Shipped");
        orders[1].TotalAmount.Should().Be(749.90m);
    }

    [Fact]
    public async Task PullOrders_LineItems_ShouldMapCorrectly()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert — Order 1, 2 lines
        orders[0].Lines.Should().HaveCount(2);
        orders[0].Lines[0].SKU.Should().Be("TY-TSH-001");
        orders[0].Lines[0].Barcode.Should().Be("8691234567001");
        orders[0].Lines[0].Quantity.Should().Be(2);
        orders[0].Lines[0].UnitPrice.Should().Be(149.90m);
        orders[0].Lines[0].LineTotal.Should().Be(299.80m);

        // Line 2: indirimli ürün
        orders[0].Lines[1].SKU.Should().Be("TY-KT-005");
        orders[0].Lines[1].DiscountAmount.Should().Be(19.90m);
        orders[0].Lines[1].LineTotal.Should().Be(180.00m);
    }

    [Fact]
    public async Task PullOrders_LastModifiedDate_ShouldMapFromEpoch()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders[0].LastModifiedDate.Should().NotBeNull(
            "lastModifiedDate epoch should map to LastModifiedDate");
        orders[0].LastModifiedDate!.Value.Year.Should().Be(2026);
    }

    [Fact]
    public async Task PullOrders_Currency_ShouldMapFromResponse()
    {
        // Arrange
        var adapter = await CreateConfiguredAdapterAsync();
        SetupOrdersEndpoint(FullOrderResponse);

        // Act
        var orders = await adapter.PullOrdersAsync();

        // Assert
        orders[0].Currency.Should().Be("TRY");
    }

    // ══════════════════════════════════════
    // Helper
    // ══════════════════════════════════════

    private void SetupOrdersEndpoint(string responseBody)
    {
        _server
            .Given(Request.Create()
                .WithPath($"/integration/order/sellers/{SupplierId}/orders")
                .UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(responseBody));
    }

    public void Dispose() { }
}
