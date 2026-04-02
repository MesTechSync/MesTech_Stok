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
/// Tests that parse golden-file eBay API responses into MesTech domain objects.
/// Validates that the parsing logic correctly maps all fields from real eBay
/// Inventory, Fulfillment, and Finances API response formats.
/// No network calls — purely offline parsing of test data files.
/// </summary>
[Trait("Category", "RealData")]
[Trait("Platform", "eBay")]
public class EbayRealDataTests
{
    private static readonly string TestDataBasePath = Path.Combine(
        AppContext.BaseDirectory, "TestData", "RealAPI", "ebay");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    // ══════════════════════════════════════
    // 1. Inventory API Response Parsing
    // ══════════════════════════════════════

    [Fact]
    public async Task ParseRealInventoryResponse_ShouldMapAllFields()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "inventory_response.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        // Act — parse using the same logic as EbayAdapter.PullProductsAsync
        var products = new List<Product>();

        if (doc.RootElement.TryGetProperty("inventoryItems", out var items))
        {
            foreach (var item in items.EnumerateArray())
            {
                var sku = item.TryGetProperty("sku", out var skuEl) ? skuEl.GetString() ?? "" : "";
                var title = "";
                var stock = 0;

                if (item.TryGetProperty("product", out var productEl))
                {
                    title = productEl.TryGetProperty("title", out var titleEl)
                        ? titleEl.GetString() ?? ""
                        : "";
                }

                if (item.TryGetProperty("availability", out var avail) &&
                    avail.TryGetProperty("shipToLocationAvailability", out var shipAvail) &&
                    shipAvail.TryGetProperty("quantity", out var qtyEl))
                {
                    stock = qtyEl.GetInt32();
                }

                products.Add(new Product
                {
                    Name = title,
                    SKU = sku,
                    Stock = stock,
                    SalePrice = 0m
                });
            }
        }

        // Assert — verify all 4 items parsed correctly
        products.Should().HaveCount(4, "golden file contains 4 inventory items");

        // Item 1: Mouse
        products[0].SKU.Should().Be("EBAY-TR-MOUSE-001");
        products[0].Name.Should().Be("Kablosuz Ergonomik Mouse 2.4GHz");
        products[0].Stock.Should().Be(150);

        // Item 2: Keyboard
        products[1].SKU.Should().Be("EBAY-TR-KB-002");
        products[1].Name.Should().Be("Mekanik Gaming Klavye RGB LED");
        products[1].Stock.Should().Be(75);

        // Item 3: Hub (out of stock)
        products[2].SKU.Should().Be("EBAY-TR-HUB-003");
        products[2].Name.Should().Be("USB-C 7-in-1 Hub Adaptoru HDMI 4K");
        products[2].Stock.Should().Be(0, "hub is out of stock in the golden file");

        // Item 4: Cable
        products[3].SKU.Should().Be("EBAY-TR-CABLE-004");
        products[3].Name.Should().Be("USB-C to Lightning Kablo 2m MFi Sertifikali");
        products[3].Stock.Should().Be(320);
    }

    [Fact]
    public async Task ParseRealInventoryResponse_TotalField_ShouldMatchItemCount()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "inventory_response.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        // Act
        var total = doc.RootElement.TryGetProperty("total", out var totalEl) ? totalEl.GetInt32() : 0;
        var itemCount = 0;
        if (doc.RootElement.TryGetProperty("inventoryItems", out var items))
        {
            foreach (var _ in items.EnumerateArray()) itemCount++;
        }

        // Assert
        total.Should().Be(4);
        itemCount.Should().Be(total, "item count should match the total field");
    }

    // ══════════════════════════════════════
    // 2. Fulfillment API Response Parsing (Orders)
    // ══════════════════════════════════════

    [Fact]
    public async Task ParseRealOrdersResponse_ShouldMapAllFields()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "orders_response.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        // Act — parse using the same logic as EbayAdapter.PullOrdersAsync
        var orders = new List<ExternalOrderDto>();

        if (doc.RootElement.TryGetProperty("orders", out var ordersArr))
        {
            foreach (var orderEl in ordersArr.EnumerateArray())
            {
                var orderId = orderEl.TryGetProperty("orderId", out var oidEl) ? oidEl.GetString() ?? "" : "";
                var orderStatus = orderEl.TryGetProperty("orderFulfillmentStatus", out var stEl)
                    ? stEl.GetString() ?? ""
                    : "";

                var orderDate = DateTime.UtcNow;
                if (orderEl.TryGetProperty("creationDate", out var createdEl) &&
                    DateTime.TryParse(createdEl.GetString(), CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var parsedDate))
                {
                    orderDate = parsedDate;
                }

                var order = new ExternalOrderDto
                {
                    PlatformCode = "eBay",
                    PlatformOrderId = orderId,
                    OrderNumber = orderId,
                    Status = orderStatus,
                    OrderDate = orderDate,
                    Currency = "USD"
                };

                // Extract total price
                if (orderEl.TryGetProperty("pricingSummary", out var pricing))
                {
                    if (pricing.TryGetProperty("total", out var totalEl))
                    {
                        if (totalEl.TryGetProperty("value", out var valEl) &&
                            decimal.TryParse(valEl.GetString(), NumberStyles.Number,
                                CultureInfo.InvariantCulture, out var totalAmount))
                        {
                            order.TotalAmount = totalAmount;
                        }
                        if (totalEl.TryGetProperty("currency", out var currEl))
                            order.Currency = currEl.GetString() ?? "USD";
                    }

                    if (pricing.TryGetProperty("deliveryCost", out var deliveryEl) &&
                        deliveryEl.TryGetProperty("value", out var dValEl) &&
                        decimal.TryParse(dValEl.GetString(), NumberStyles.Number,
                            CultureInfo.InvariantCulture, out var shippingCost))
                    {
                        order.ShippingCost = shippingCost;
                    }
                }

                // Extract buyer info
                if (orderEl.TryGetProperty("buyer", out var buyer))
                {
                    order.CustomerName = buyer.TryGetProperty("username", out var unEl)
                        ? unEl.GetString() ?? ""
                        : "";
                }

                // Extract shipping address
                if (orderEl.TryGetProperty("fulfillmentStartInstructions", out var fulfillArr))
                {
                    foreach (var instr in fulfillArr.EnumerateArray())
                    {
                        if (instr.TryGetProperty("shippingStep", out var shipStep) &&
                            shipStep.TryGetProperty("shipTo", out var shipTo))
                        {
                            if (shipTo.TryGetProperty("fullName", out var fnEl))
                                order.CustomerName = fnEl.GetString() ?? order.CustomerName;

                            if (shipTo.TryGetProperty("primaryPhone", out var phoneEl) &&
                                phoneEl.TryGetProperty("phoneNumber", out var phoneNum))
                                order.CustomerPhone = phoneNum.GetString();

                            if (shipTo.TryGetProperty("contactAddress", out var addr))
                            {
                                var street = addr.TryGetProperty("addressLine1", out var al1)
                                    ? al1.GetString() ?? ""
                                    : "";
                                var city = addr.TryGetProperty("city", out var cityEl)
                                    ? cityEl.GetString() ?? ""
                                    : "";
                                order.CustomerAddress = $"{street}, {city}".Trim(' ', ',');
                                order.CustomerCity = string.IsNullOrEmpty(city) ? order.CustomerCity : city;
                            }
                            break;
                        }
                    }
                }

                // Extract order lines
                if (orderEl.TryGetProperty("lineItems", out var lineItems))
                {
                    foreach (var lineEl in lineItems.EnumerateArray())
                    {
                        var lineId = lineEl.TryGetProperty("lineItemId", out var liEl) ? liEl.GetString() : null;
                        var sku = lineEl.TryGetProperty("sku", out var skuEl) ? skuEl.GetString() : null;
                        var title = lineEl.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? "" : "";
                        var qty = lineEl.TryGetProperty("quantity", out var qtyEl) ? qtyEl.GetInt32() : 1;

                        var unitPrice = 0m;
                        if (lineEl.TryGetProperty("lineItemCost", out var licEl) &&
                            licEl.TryGetProperty("value", out var licValEl))
                        {
                            decimal.TryParse(licValEl.GetString(), NumberStyles.Number,
                                CultureInfo.InvariantCulture, out unitPrice);
                            if (qty > 0) unitPrice /= qty;
                        }

                        order.Lines.Add(new ExternalOrderLineDto
                        {
                            PlatformLineId = lineId,
                            SKU = sku,
                            ProductName = title,
                            Quantity = qty,
                            UnitPrice = unitPrice,
                            TaxRate = 0m,
                            LineTotal = unitPrice * qty
                        });
                    }
                }

                orders.Add(order);
            }
        }

        // Assert — verify all 3 orders parsed correctly
        orders.Should().HaveCount(3, "golden file contains 3 orders");

        // Order 1: NOT_STARTED, 2 line items
        orders[0].PlatformOrderId.Should().Be("12-34567-89012");
        orders[0].Status.Should().Be("NOT_STARTED");
        orders[0].TotalAmount.Should().Be(109.69m);
        orders[0].ShippingCost.Should().Be(12.50m);
        orders[0].Currency.Should().Be("USD");
        orders[0].CustomerName.Should().Be("Ahmet Yilmaz");
        orders[0].CustomerPhone.Should().Be("+905551234567");
        orders[0].CustomerCity.Should().Be("Istanbul");
        orders[0].Lines.Should().HaveCount(2);
        orders[0].Lines[0].SKU.Should().Be("EBAY-TR-MOUSE-001");
        orders[0].Lines[0].Quantity.Should().Be(2);
        orders[0].Lines[1].SKU.Should().Be("EBAY-TR-CABLE-004");
        orders[0].Lines[1].Quantity.Should().Be(1);

        // Order 2: FULFILLED, free shipping
        orders[1].PlatformOrderId.Should().Be("12-34567-89013");
        orders[1].Status.Should().Be("FULFILLED");
        orders[1].TotalAmount.Should().Be(215.99m);
        orders[1].ShippingCost.Should().Be(0.00m);
        orders[1].CustomerName.Should().Be("Mehmet Demir");
        orders[1].Lines.Should().HaveCount(1);
        orders[1].Lines[0].SKU.Should().Be("EBAY-TR-KB-002");

        // Order 3: IN_PROGRESS
        orders[2].PlatformOrderId.Should().Be("12-34567-89014");
        orders[2].Status.Should().Be("IN_PROGRESS");
        orders[2].TotalAmount.Should().Be(58.13m);
        orders[2].CustomerName.Should().Be("Ayse Kaya");
        orders[2].CustomerCity.Should().Be("Izmir");
    }

    [Fact]
    public async Task ParseRealOrdersResponse_DateParsing_ShouldBeUtc()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "orders_response.json");
        var json = await File.ReadAllTextAsync(jsonPath);
        using var doc = JsonDocument.Parse(json);

        // Act — parse first order's creationDate
        var firstOrder = doc.RootElement.GetProperty("orders").EnumerateArray().First();
        var dateStr = firstOrder.GetProperty("creationDate").GetString()!;
        DateTime.TryParse(dateStr, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed);

        // Assert
        parsed.Kind.Should().Be(DateTimeKind.Utc, "eBay dates are ISO 8601 UTC");
        parsed.Year.Should().Be(2026);
        parsed.Month.Should().Be(3);
        parsed.Day.Should().Be(10);
    }

    // ══════════════════════════════════════
    // 3. Finances API Response Parsing (Settlements)
    // ══════════════════════════════════════

    [Fact]
    public async Task ParseRealSettlementResponse_ShouldCreateBatch()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "settlement_response.json");
        var jsonBytes = await File.ReadAllBytesAsync(jsonPath);
        using var stream = new MemoryStream(jsonBytes);

        var logger = new LoggerFactory().CreateLogger<EbaySettlementParser>();
        var parser = new EbaySettlementParser(logger);

        // Act
        var batch = await parser.ParseAsync(Guid.NewGuid(), stream, "json");

        // Assert — batch-level validation
        batch.Should().NotBeNull();
        batch.Platform.Should().Be("eBay");

        // Total gross = sum of totalFeeBasisAmount for SALE transactions
        // SALE transactions: 109.69 + 215.99 + 58.13 + 82.50 + 155.00 = 621.31
        batch.TotalGross.Should().Be(621.31m, "sum of totalFeeBasisAmount for SALE transactions");

        // Total commission = sum of all totalFeeAmount (including refund negative)
        // 13.38 + 26.80 + 7.23 + (-3.90) + 0 + 10.05 + 20.20 = 73.76
        batch.TotalCommission.Should().Be(73.76m, "sum of all totalFeeAmount values");

        // Total net = sum of all amounts
        // 96.31 + 189.19 + 50.90 + (-29.99) + (-8.50) + 72.45 + 134.80 = 505.16
        batch.TotalNet.Should().Be(505.16m, "sum of all transaction amounts");

        // Period should span from earliest to latest transaction date
        batch.PeriodStart.Should().Be(new DateTime(2026, 3, 8, 9, 15, 0, DateTimeKind.Utc));
        batch.PeriodEnd.Should().Be(new DateTime(2026, 3, 14, 20, 10, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ParseRealSettlementResponse_ShouldCreateLines()
    {
        // Arrange
        var jsonPath = Path.Combine(TestDataBasePath, "settlement_response.json");
        var jsonBytes = await File.ReadAllBytesAsync(jsonPath);
        using var stream = new MemoryStream(jsonBytes);

        var logger = new LoggerFactory().CreateLogger<EbaySettlementParser>();
        var parser = new EbaySettlementParser(logger);

        // Act
        var batch = await parser.ParseAsync(Guid.NewGuid(), stream, "json");
        var lines = await parser.ParseLinesAsync(batch);

        // Assert — line-level validation
        lines.Should().HaveCount(7, "golden file contains 7 transactions");
        batch.Lines.Should().HaveCount(7, "lines should also be added to the batch");

        // SALE line (first): orderId=12-34567-89012, gross=109.69, fee=13.38, net=96.31
        var saleLine = lines[0];
        saleLine.OrderId.Should().Be("12-34567-89012");
        saleLine.GrossAmount.Should().Be(109.69m);
        saleLine.CommissionAmount.Should().Be(13.38m);
        saleLine.NetAmount.Should().Be(96.31m);
        saleLine.CargoDeduction.Should().Be(0m, "SALE transactions have no cargo deduction");
        saleLine.RefundDeduction.Should().Be(0m, "SALE transactions have no refund deduction");

        // REFUND line (4th): amount is negative
        var refundLine = lines[3];
        refundLine.OrderId.Should().Be("12-34567-89010");
        refundLine.RefundDeduction.Should().Be(29.99m, "REFUND amount absolute value");
        refundLine.NetAmount.Should().Be(-29.99m, "REFUND net is negative");

        // SHIPPING_LABEL line (5th): cargo deduction
        var shippingLine = lines[4];
        shippingLine.OrderId.Should().Be("12-34567-89012");
        shippingLine.CargoDeduction.Should().Be(8.50m, "SHIPPING_LABEL amount absolute value");
        shippingLine.NetAmount.Should().Be(-8.50m, "SHIPPING_LABEL net is negative");
    }

    [Fact]
    public async Task ParseRealSettlementResponse_EmptyTransactions_ShouldCreateEmptyBatch()
    {
        // Arrange — empty transactions array
        var json = """{"transactions": []}""";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var logger = new LoggerFactory().CreateLogger<EbaySettlementParser>();
        var parser = new EbaySettlementParser(logger);

        // Act
        var batch = await parser.ParseAsync(Guid.NewGuid(), stream, "json");

        // Assert
        batch.Should().NotBeNull();
        batch.Platform.Should().Be("eBay");
        batch.TotalGross.Should().Be(0m);
        batch.TotalCommission.Should().Be(0m);
        batch.TotalNet.Should().Be(0m);
    }
}
