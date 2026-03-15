using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
using Microsoft.Extensions.Logging;

namespace MesTech.Tests.Integration.RealData;

/// <summary>
/// Tests that parse golden-file Trendyol API responses into MesTech domain objects.
/// Validates that the parsing logic correctly maps all fields from real Trendyol
/// product list and order list API response formats.
/// No network calls — purely offline parsing of test data files.
/// </summary>
[Trait("Category", "RealData")]
[Trait("Platform", "Trendyol")]
public class TrendyolRealDataTests
{
    private static readonly string TestDataBasePath = Path.Combine(
        AppContext.BaseDirectory, "TestData", "RealAPI", "trendyol");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // ══════════════════════════════════════
    // 1. Products API Response Parsing
    // ══════════════════════════════════════

    [Fact]
    public async Task ParseRealProductsResponse_ShouldMapAllFields()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "products_response.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        // Act — parse using the same logic as TrendyolAdapter.PullProductsAsync
        var products = new List<Product>();

        if (doc.RootElement.TryGetProperty("content", out var contentArr))
        {
            foreach (var item in contentArr.EnumerateArray())
            {
                products.Add(new Product
                {
                    Name = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                    SKU = item.TryGetProperty("stockCode", out var sc) ? sc.GetString() ?? "" : "",
                    Barcode = item.TryGetProperty("barcode", out var b) ? b.GetString() : null,
                    SalePrice = item.TryGetProperty("salePrice", out var sp) ? sp.GetDecimal() : 0,
                    ListPrice = item.TryGetProperty("listPrice", out var lp) ? lp.GetDecimal() : null,
                    Stock = item.TryGetProperty("quantity", out var q) ? q.GetInt32() : 0,
                    Description = item.TryGetProperty("description", out var d) ? d.GetString() : null
                });
            }
        }

        // Assert — verify all 5 products parsed correctly
        products.Should().HaveCount(5, "golden file contains 5 products");

        // Product 1: T-Shirt
        products[0].SKU.Should().Be("TY-TSH-001");
        products[0].Barcode.Should().Be("8691234567001");
        products[0].Name.Should().Be("Pamuklu Oversize T-Shirt Beyaz");
        products[0].SalePrice.Should().Be(149.90m);
        products[0].ListPrice.Should().Be(249.90m);
        products[0].Stock.Should().Be(200);
        products[0].Description.Should().Contain("yuzde yuz pamuk", "description should contain fabric info");

        // Product 2: Jean
        products[1].SKU.Should().Be("TY-JN-002");
        products[1].Barcode.Should().Be("8691234567002");
        products[1].SalePrice.Should().Be(399.90m);
        products[1].Stock.Should().Be(85);

        // Product 3: Shoe
        products[2].SKU.Should().Be("TY-SH-003");
        products[2].SalePrice.Should().Be(749.90m);
        products[2].ListPrice.Should().Be(999.90m);
        products[2].Stock.Should().Be(42);

        // Product 4: Home textile
        products[3].SKU.Should().Be("TY-HM-004");
        products[3].SalePrice.Should().Be(69.90m);
        products[3].Stock.Should().Be(500);

        // Product 5: Kitchen — out of stock
        products[4].SKU.Should().Be("TY-KT-005");
        products[4].SalePrice.Should().Be(199.90m);
        products[4].Stock.Should().Be(0, "termos is out of stock in the golden file");
    }

    [Fact]
    public async Task ParseRealProductsResponse_PaginationFields_ShouldBeCorrect()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "products_response.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        // Act
        var totalElements = doc.RootElement.TryGetProperty("totalElements", out var te) ? te.GetInt32() : 0;
        var totalPages = doc.RootElement.TryGetProperty("totalPages", out var tp) ? tp.GetInt32() : 0;
        var page = doc.RootElement.TryGetProperty("page", out var p) ? p.GetInt32() : -1;

        // Assert
        totalElements.Should().Be(5);
        totalPages.Should().Be(1);
        page.Should().Be(0);
    }

    [Fact]
    public async Task ParseRealProductsResponse_DiscountedProducts_ShouldHaveLowerSalePrice()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "products_response.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        // Act — check that all products have salePrice <= listPrice
        if (doc.RootElement.TryGetProperty("content", out var contentArr))
        {
            foreach (var item in contentArr.EnumerateArray())
            {
                var salePrice = item.TryGetProperty("salePrice", out var sp) ? sp.GetDecimal() : 0;
                var listPrice = item.TryGetProperty("listPrice", out var lp) ? lp.GetDecimal() : 0;
                var stockCode = item.TryGetProperty("stockCode", out var sc) ? sc.GetString() : "unknown";

                // Assert
                salePrice.Should().BeLessThanOrEqualTo(listPrice,
                    $"salePrice should not exceed listPrice for SKU {stockCode}");
            }
        }
    }

    // ══════════════════════════════════════
    // 2. Orders API Response Parsing
    // ══════════════════════════════════════

    [Fact]
    public async Task ParseRealOrdersResponse_ShouldMapAllFields()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "orders_response.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        // Act — parse using the same logic as TrendyolAdapter.PullOrdersAsync
        var orders = new List<ExternalOrderDto>();

        if (doc.RootElement.TryGetProperty("content", out var contentArr))
        {
            foreach (var item in contentArr.EnumerateArray())
            {
                var orderNumber = item.TryGetProperty("orderNumber", out var onProp) ? onProp.GetString() ?? "" : "";

                var order = new ExternalOrderDto
                {
                    PlatformCode = "Trendyol",
                    PlatformOrderId = orderNumber,
                    OrderNumber = orderNumber,
                    Status = item.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
                    TotalAmount = item.TryGetProperty("totalPrice", out var tp2) ? tp2.GetDecimal() : 0,
                    OrderDate = item.TryGetProperty("orderDate", out var od)
                        ? DateTimeOffset.FromUnixTimeMilliseconds(od.GetInt64()).UtcDateTime
                        : DateTime.UtcNow
                };

                // Lines
                if (item.TryGetProperty("lines", out var lines))
                {
                    foreach (var line in lines.EnumerateArray())
                    {
                        order.Lines.Add(new ExternalOrderLineDto
                        {
                            PlatformLineId = line.TryGetProperty("id", out var lid) ? lid.GetInt64().ToString() : null,
                            SKU = line.TryGetProperty("merchantSku", out var sku) ? sku.GetString() : null,
                            Barcode = line.TryGetProperty("barcode", out var bc) ? bc.GetString() : null,
                            ProductName = line.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "",
                            Quantity = line.TryGetProperty("quantity", out var qty) ? qty.GetInt32() : 1,
                            UnitPrice = line.TryGetProperty("price", out var up) ? up.GetDecimal() : 0,
                            DiscountAmount = line.TryGetProperty("discount", out var disc) ? disc.GetDecimal() : null,
                            LineTotal = line.TryGetProperty("amount", out var amt) ? amt.GetDecimal() : 0
                        });
                    }
                }

                // Cargo info
                if (item.TryGetProperty("shipmentPackageId", out var spId))
                    order.ShipmentPackageId = spId.GetInt64().ToString();
                if (item.TryGetProperty("cargoProviderName", out var cpn))
                    order.CargoProviderName = cpn.GetString();
                if (item.TryGetProperty("cargoTrackingNumber", out var ctn))
                    order.CargoTrackingNumber = ctn.GetString();

                orders.Add(order);
            }
        }

        // Assert — verify all 3 orders parsed correctly
        orders.Should().HaveCount(3, "golden file contains 3 orders");

        // Order 1: Created, 3 line items
        orders[0].PlatformOrderId.Should().Be("TY-ORD-2026031001");
        orders[0].Status.Should().Be("Created");
        orders[0].TotalAmount.Should().Be(549.70m);
        orders[0].ShipmentPackageId.Should().Be("98765001");
        orders[0].CargoProviderName.Should().Be("Yurtici Kargo");
        orders[0].CargoTrackingNumber.Should().Be("YK2026031001");
        orders[0].Lines.Should().HaveCount(3);
        orders[0].Lines[0].SKU.Should().Be("TY-TSH-001");
        orders[0].Lines[0].Barcode.Should().Be("8691234567001");
        orders[0].Lines[0].Quantity.Should().Be(2);
        orders[0].Lines[0].UnitPrice.Should().Be(149.90m);
        orders[0].Lines[0].LineTotal.Should().Be(299.80m);

        // Order 1, Line 3: discounted item
        orders[0].Lines[2].SKU.Should().Be("TY-KT-005");
        orders[0].Lines[2].UnitPrice.Should().Be(199.90m);
        orders[0].Lines[2].DiscountAmount.Should().Be(19.90m);
        orders[0].Lines[2].LineTotal.Should().Be(180.00m);

        // Order 2: Picking, single line, no tracking number yet
        orders[1].PlatformOrderId.Should().Be("TY-ORD-2026031002");
        orders[1].Status.Should().Be("Picking");
        orders[1].TotalAmount.Should().Be(399.90m);
        orders[1].CargoProviderName.Should().Be("Aras Kargo");
        orders[1].CargoTrackingNumber.Should().BeNull("order is still in Picking status");
        orders[1].Lines.Should().HaveCount(1);
        orders[1].Lines[0].SKU.Should().Be("TY-JN-002");

        // Order 3: Shipped
        orders[2].PlatformOrderId.Should().Be("TY-ORD-2026031003");
        orders[2].Status.Should().Be("Shipped");
        orders[2].TotalAmount.Should().Be(749.90m);
        orders[2].CargoProviderName.Should().Be("MNG Kargo");
        orders[2].CargoTrackingNumber.Should().Be("MNG2026031003");
    }

    [Fact]
    public async Task ParseRealOrdersResponse_OrderDates_ShouldBeEpochMilliseconds()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "orders_response.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        // Act — parse all order dates
        var orders = doc.RootElement.GetProperty("content").EnumerateArray().ToList();

        foreach (var order in orders)
        {
            var epochMs = order.GetProperty("orderDate").GetInt64();
            var parsed = DateTimeOffset.FromUnixTimeMilliseconds(epochMs).UtcDateTime;

            // Assert — all dates should be in 2026
            parsed.Year.Should().Be(2026, "order dates in the golden file are from 2026");
            parsed.Month.Should().Be(3, "order dates are in March");
        }
    }

    [Fact]
    public async Task ParseRealOrdersResponse_LineIds_ShouldBeNumeric()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "orders_response.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        // Act & Assert — Trendyol line IDs are numeric (long)
        var orders = doc.RootElement.GetProperty("content").EnumerateArray().ToList();

        foreach (var order in orders)
        {
            if (order.TryGetProperty("lines", out var lines))
            {
                foreach (var line in lines.EnumerateArray())
                {
                    var lineId = line.GetProperty("id").GetInt64();
                    lineId.Should().BeGreaterThan(0, "Trendyol line IDs should be positive integers");
                }
            }
        }
    }
}
