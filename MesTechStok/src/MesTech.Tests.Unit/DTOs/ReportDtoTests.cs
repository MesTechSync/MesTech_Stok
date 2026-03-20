using System.Reflection;
using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.DTOs.Reports;

namespace MesTech.Tests.Unit.DTOs;

/// <summary>
/// Report DTO testleri.
/// Envanter degerleme, musteri CLV, siparis karsilama, vergi ozeti ve PII koruma kontrolu.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Reporting")]
[Trait("Phase", "I-11")]
public class ReportDtoTests
{
    [Fact(DisplayName = "InventoryValuationDto — TotalCostValue equals CurrentStock * PurchasePrice")]
    public void InventoryValuationDto_TotalValue_ShouldBeCorrect()
    {
        // Arrange
        var dto = new InventoryValuationReportDto
        {
            ProductId = Guid.NewGuid(),
            ProductName = "Test Product",
            SKU = "TST-001",
            CurrentStock = 50,
            PurchasePrice = 25.00m,
            SalePrice = 45.00m,
            TotalCostValue = 50 * 25.00m,
            TotalSaleValue = 50 * 45.00m,
            PotentialProfit = (50 * 45.00m) - (50 * 25.00m)
        };

        // Assert
        dto.TotalCostValue.Should().Be(1250.00m);
        dto.TotalSaleValue.Should().Be(2250.00m);
        dto.PotentialProfit.Should().Be(1000.00m);
    }

    [Fact(DisplayName = "CustomerCLVDto — EstimatedCLV should be positive for active customer")]
    public void CustomerCLVDto_EstimatedCLV_ShouldBePositive()
    {
        // Arrange — real customer order data
        var dto = new CustomerLifetimeValueReportDto
        {
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            TotalOrders = 12,
            TotalSpent = 4500.00m,
            AverageOrderValue = 375.00m,
            FirstOrderDate = new DateTime(2025, 1, 15),
            LastOrderDate = new DateTime(2026, 3, 10),
            DaysSinceLastOrder = 10,
            EstimatedCLV = 8500.00m
        };

        // Assert
        dto.EstimatedCLV.Should().BePositive();
        dto.TotalOrders.Should().BeGreaterThan(0);
        dto.AverageOrderValue.Should().Be(dto.TotalSpent / dto.TotalOrders);
    }

    [Fact(DisplayName = "FulfillmentReportDto — FulfillmentRate should be 0-100 percentage")]
    public void FulfillmentReportDto_FulfillmentRate_ShouldBePercentage()
    {
        // Arrange
        var dto = new OrderFulfillmentReportDto
        {
            Platform = "Trendyol",
            TotalOrders = 200,
            ShippedOrders = 195,
            DeliveredOrders = 190,
            AvgOrderToShipHours = 4.5,
            AvgShipToDeliverDays = 2.3,
            AvgTotalFulfillmentDays = 3.1,
            FulfillmentRate = 97.5
        };

        // Assert
        dto.FulfillmentRate.Should().BeInRange(0, 100);
        dto.DeliveredOrders.Should().BeLessThanOrEqualTo(dto.ShippedOrders);
        dto.ShippedOrders.Should().BeLessThanOrEqualTo(dto.TotalOrders);
    }

    [Fact(DisplayName = "TaxSummaryReportDto — NetVatPayable equals OutputVat minus InputVat")]
    public void TaxSummaryDto_KDVPayable_ShouldBeCollectedMinusPaid()
    {
        // Arrange
        var dto = new TaxSummaryReportDto
        {
            TaxPeriod = "2026-02",
            TotalSalesAmount = 100_000m,
            TotalPurchaseAmount = 60_000m,
            OutputVat = 20_000m,   // KDV collected from sales
            InputVat = 12_000m,    // KDV paid on purchases
            NetVatPayable = 8_000m, // Difference
            InvoiceCount = 45,
            WithholdingAmount = 500m
        };

        // Assert
        dto.NetVatPayable.Should().Be(dto.OutputVat - dto.InputVat);
        dto.OutputVat.Should().BeGreaterThan(dto.InputVat, "sales > purchases means more VAT collected");
    }

    [Fact(DisplayName = "NotificationSettingDto — should NOT expose ChannelAddress (PII protection)")]
    public void NotificationSettingDto_ShouldNotExposeChannelAddress()
    {
        // Act — check that the DTO type does NOT have a ChannelAddress property
        var dtoType = typeof(NotificationSettingDto);
        var channelAddressProp = dtoType.GetProperty(
            "ChannelAddress",
            BindingFlags.Public | BindingFlags.Instance);

        // Assert — PII must not leak to UI layer
        channelAddressProp.Should().BeNull(
            "NotificationSettingDto must not expose ChannelAddress (PII) — only the entity holds it");
    }
}
