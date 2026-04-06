using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.ChainTests;

/// <summary>
/// TEST 1/4 — PullOrders → Order entity mapping.
/// Trendyol JSON fixture → ExternalOrderDto → Order entity.
/// orderNumber, totalPrice, grossAmount, totalDiscount, lines[].TaxRate doğrulama.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "OrderChain")]
public class PullOrdersEntityMappingChainTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static ExternalOrderDto CreateTrendyolOrderDto(
        string orderNumber = "TY-ORD-2026031001",
        decimal totalPrice = 549.70m,
        decimal? grossAmount = 569.60m,
        decimal? totalDiscount = 19.90m)
    {
        return new ExternalOrderDto
        {
            PlatformCode = "Trendyol",
            PlatformOrderId = orderNumber,
            OrderNumber = orderNumber,
            Status = "Created",
            CustomerName = "Ahmet Yilmaz",
            CustomerEmail = "ahmet@example.com",
            CustomerTaxNumber = "1234567890",
            TotalAmount = totalPrice,
            GrossAmount = grossAmount,
            TotalDiscount = totalDiscount,
            Currency = "TRY",
            OrderDate = new DateTime(2026, 3, 10, 14, 30, 0, DateTimeKind.Utc),
            ShipmentPackageId = "98765001",
            CargoProviderName = "Yurtici Kargo",
            CargoTrackingNumber = "YK2026031001",
            Lines = new List<ExternalOrderLineDto>
            {
                new()
                {
                    PlatformLineId = "300001", SKU = "TY-TSH-001", Barcode = "8691234567001",
                    ProductName = "Pamuklu T-Shirt", Quantity = 2, UnitPrice = 149.90m,
                    TaxRate = 0.10m, LineTotal = 299.80m
                },
                new()
                {
                    PlatformLineId = "300002", SKU = "TY-KT-005", Barcode = "8691234567005",
                    ProductName = "Termos 500ml", Quantity = 1, UnitPrice = 199.90m,
                    DiscountAmount = 19.90m, TaxRate = 0.18m, LineTotal = 180.00m
                }
            }
        };
    }

    /// <summary>TrendyolOrderSyncJob.MapToOrder mantığını simüle eder.</summary>
    private static Order MapToOrder(ExternalOrderDto dto)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            OrderNumber = dto.OrderNumber,
            ExternalOrderId = dto.PlatformOrderId,
            SourcePlatform = PlatformType.Trendyol,
            CustomerName = dto.CustomerName,
            CustomerEmail = dto.CustomerEmail,
            OrderDate = dto.OrderDate
        };

        var gross = dto.GrossAmount ?? dto.TotalAmount;
        var discount = dto.TotalDiscount ?? 0m;
        var subTotal = gross - discount;
        var taxAmount = dto.Lines.Sum(l => l.TaxRate * l.LineTotal);
        order.SetFinancials(subTotal, taxAmount, dto.TotalAmount);

        return order;
    }

    [Fact]
    public void Map_OrderNumber_ShouldMatch()
    {
        var dto = CreateTrendyolOrderDto(orderNumber: "TY-ORD-TEST-001");
        var order = MapToOrder(dto);

        order.OrderNumber.Should().Be("TY-ORD-TEST-001");
        order.ExternalOrderId.Should().Be("TY-ORD-TEST-001");
        order.SourcePlatform.Should().Be(PlatformType.Trendyol);
    }

    [Fact]
    public void Map_Financials_GrossMinusDiscountEqualsTotal()
    {
        var dto = CreateTrendyolOrderDto(grossAmount: 569.60m, totalDiscount: 19.90m, totalPrice: 549.70m);
        var order = MapToOrder(dto);

        order.TotalAmount.Should().Be(549.70m);
        (dto.GrossAmount!.Value - dto.TotalDiscount!.Value).Should().Be(order.TotalAmount);
    }

    [Fact]
    public void Map_TaxAmount_CalculatedFromLineTaxRates()
    {
        var dto = CreateTrendyolOrderDto();
        var order = MapToOrder(dto);

        // Line 1: 0.10 * 299.80 = 29.98
        // Line 2: 0.18 * 180.00 = 32.40
        // Total tax = 62.38
        var expectedTax = (0.10m * 299.80m) + (0.18m * 180.00m);
        order.TaxAmount.Should().Be(expectedTax, "taxAmount = sum(line.TaxRate * line.LineTotal)");
    }

    [Fact]
    public void Map_Lines_TaxRateValues()
    {
        var dto = CreateTrendyolOrderDto();

        dto.Lines[0].TaxRate.Should().Be(0.10m, "vatRate 10 → 0.10");
        dto.Lines[1].TaxRate.Should().Be(0.18m, "vatRate 18 → 0.18");
        dto.Lines[0].LineTotal.Should().Be(299.80m);
        dto.Lines[1].LineTotal.Should().Be(180.00m);
        dto.Lines[1].DiscountAmount.Should().Be(19.90m);
    }

    [Fact]
    public void Map_CustomerInfo_ShouldCarryOver()
    {
        var dto = CreateTrendyolOrderDto();
        var order = MapToOrder(dto);

        order.CustomerName.Should().Be("Ahmet Yilmaz");
        order.CustomerEmail.Should().Be("ahmet@example.com");
        order.OrderDate.Year.Should().Be(2026);
    }
}
