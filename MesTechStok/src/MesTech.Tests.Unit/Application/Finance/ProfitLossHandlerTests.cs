using FluentAssertions;
using MesTech.Application.DTOs.Finance;
using MesTech.Application.Features.Finance.Queries.GetProfitLoss;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Finance;

/// <summary>
/// DEV 5 — H31 Task 5.2: ProfitLossHandler unit tests.
/// Verifies GetProfitLossHandler calculates net profit correctly,
/// handles empty orders, and groups revenue by platform.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Finance")]
public class ProfitLossHandlerTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();

    private readonly Mock<IFinanceExpenseRepository> _expenseRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();

    private GetProfitLossHandler CreateHandler() =>
        new(_expenseRepo.Object, _orderRepo.Object);

    /// <summary>
    /// Test 1: Orders (some cancelled) + expenses -> correct NetProfit.
    /// NetProfit = TotalRevenue - TotalExpenses
    /// Cancelled orders should be excluded from revenue.
    /// </summary>
    [Fact]
    public async Task GetProfitLoss_WithOrdersAndExpenses_ReturnsCorrectNetProfit()
    {
        // Arrange
        var year = 2026;
        var month = 3;
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);

        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 500m, OrderStatus.Confirmed, PlatformType.Trendyol),
            CreateOrder(_tenantId, 300m, OrderStatus.Shipped, PlatformType.Hepsiburada),
            CreateOrder(_tenantId, 200m, OrderStatus.Cancelled, PlatformType.Trendyol), // excluded
            CreateOrder(_tenantId, 150m, OrderStatus.Delivered, PlatformType.N11),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);

        var totalExpenses = 400m;
        _expenseRepo.Setup(r => r.GetTotalByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(totalExpenses);

        _expenseRepo.Setup(r => r.GetByTenantAsync(
                _tenantId, It.IsAny<ExpenseStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>());

        var query = new GetProfitLossQuery(_tenantId, year, month);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        // Revenue = 500 + 300 + 150 = 950 (cancelled 200 excluded)
        result.TotalRevenue.Should().Be(950m,
            "only non-cancelled orders should count toward revenue");
        result.TotalExpenses.Should().Be(400m);
        result.NetProfit.Should().Be(550m,
            "NetProfit = TotalRevenue (950) - TotalExpenses (400) = 550");
        result.Year.Should().Be(year);
        result.Month.Should().Be(month);
    }

    /// <summary>
    /// Test 2: No orders -> all zeros for revenue, with expenses still counted.
    /// </summary>
    [Fact]
    public async Task GetProfitLoss_NoOrders_ReturnsZeroRevenue()
    {
        // Arrange
        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Order>());

        _expenseRepo.Setup(r => r.GetTotalByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        _expenseRepo.Setup(r => r.GetByTenantAsync(
                _tenantId, It.IsAny<ExpenseStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>());

        var query = new GetProfitLossQuery(_tenantId, 2026, 1);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalRevenue.Should().Be(0m, "no orders means zero revenue");
        result.TotalExpenses.Should().Be(0m);
        result.NetProfit.Should().Be(0m, "zero revenue minus zero expenses = zero");
        result.RevenueByPlatform.Should().BeEmpty(
            "no orders means no platform groupings");
    }

    /// <summary>
    /// Test 3: Multiple platforms -> revenue grouped correctly.
    /// </summary>
    [Fact]
    public async Task GetProfitLoss_RevenueByPlatform_GroupsCorrectly()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 1000m, OrderStatus.Confirmed, PlatformType.Trendyol),
            CreateOrder(_tenantId, 600m, OrderStatus.Shipped, PlatformType.Trendyol),
            CreateOrder(_tenantId, 500m, OrderStatus.Delivered, PlatformType.Hepsiburada),
            CreateOrder(_tenantId, 300m, OrderStatus.Confirmed, PlatformType.N11),
            CreateOrder(_tenantId, 200m, OrderStatus.Cancelled, PlatformType.Trendyol), // excluded
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(orders);

        _expenseRepo.Setup(r => r.GetTotalByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        _expenseRepo.Setup(r => r.GetByTenantAsync(
                _tenantId, It.IsAny<ExpenseStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>());

        var query = new GetProfitLossQuery(_tenantId, 2026, 3);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.RevenueByPlatform.Should().HaveCount(3,
            "should have 3 platform groups: Trendyol, Hepsiburada, N11");

        var trendyolGroup = result.RevenueByPlatform
            .FirstOrDefault(p => p.Platform == PlatformType.Trendyol.ToString());
        trendyolGroup.Should().NotBeNull("Trendyol group must exist");
        trendyolGroup!.Revenue.Should().Be(1600m,
            "Trendyol revenue = 1000 + 600 = 1600 (cancelled 200 excluded)");
        trendyolGroup.OrderCount.Should().Be(2,
            "Trendyol should have 2 non-cancelled orders");

        var hbGroup = result.RevenueByPlatform
            .FirstOrDefault(p => p.Platform == PlatformType.Hepsiburada.ToString());
        hbGroup.Should().NotBeNull("Hepsiburada group must exist");
        hbGroup!.Revenue.Should().Be(500m);
        hbGroup.OrderCount.Should().Be(1);

        var n11Group = result.RevenueByPlatform
            .FirstOrDefault(p => p.Platform == PlatformType.N11.ToString());
        n11Group.Should().NotBeNull("N11 group must exist");
        n11Group!.Revenue.Should().Be(300m);
        n11Group.OrderCount.Should().Be(1);
    }

    #region Helpers

    private static Order CreateOrder(
        Guid tenantId, decimal totalAmount, OrderStatus status, PlatformType platform)
    {
        var order = new Order
        {
            TenantId = tenantId,
            OrderNumber = $"ORD-{Guid.NewGuid():N}".Substring(0, 12),
            CustomerId = Guid.NewGuid(),
            Status = status,
            SourcePlatform = platform,
            OrderDate = DateTime.UtcNow
        };
        order.SetFinancials(0m, 0m, totalAmount);
        return order;
    }

    #endregion
}
