using FluentAssertions;
using MesTech.Application.Features.Finance.Queries.GetProfitLoss;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

/// <summary>
/// DEV5: GetProfitLossHandler testi — Kâr/Zarar raporu.
/// P1: Finansal raporlama doğruluğu kritik.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetProfitLossHandlerTests
{
    private readonly Mock<IFinanceExpenseRepository> _expenseRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();

    private GetProfitLossHandler CreateSut() => new(_expenseRepo.Object, _orderRepo.Object);

    private static Order CreateOrderWithAmount(Guid tenantId, decimal totalAmount, OrderStatus status = OrderStatus.Confirmed, PlatformType? platform = null)
    {
        var order = new Order
        {
            TenantId = tenantId,
            OrderNumber = $"ORD-{Guid.NewGuid().ToString()[..6]}",
            Status = status,
            SourcePlatform = platform
        };
        order.SetFinancials(totalAmount * 0.82m, totalAmount * 0.18m, totalAmount);
        return order;
    }

    [Fact]
    public async Task Handle_NoOrdersNoExpenses_ShouldReturnZeros()
    {
        var tenantId = Guid.NewGuid();
        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());
        _expenseRepo.Setup(r => r.GetTotalByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        _expenseRepo.Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>());

        var query = new GetProfitLossQuery(tenantId, 2026, 3);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.TotalRevenue.Should().Be(0);
        result.TotalExpenses.Should().Be(0);
        result.GrossProfit.Should().Be(0);
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithOrders_ShouldSumNonCancelledOrderAmounts()
    {
        var tenantId = Guid.NewGuid();
        var orders = new List<Order>
        {
            CreateOrderWithAmount(tenantId, 1000m, OrderStatus.Confirmed),
            CreateOrderWithAmount(tenantId, 500m, OrderStatus.Cancelled),
            CreateOrderWithAmount(tenantId, 2000m, OrderStatus.Delivered),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);
        _expenseRepo.Setup(r => r.GetTotalByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        _expenseRepo.Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>());

        var query = new GetProfitLossQuery(tenantId, 2026, 3);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        // Cancelled order (500) excluded
        result.TotalRevenue.Should().Be(3000m);
    }

    [Fact]
    public async Task Handle_ShouldFilterByTenant()
    {
        var tenantId = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        var orders = new List<Order>
        {
            CreateOrderWithAmount(tenantId, 1000m),
            CreateOrderWithAmount(otherTenant, 9999m),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);
        _expenseRepo.Setup(r => r.GetTotalByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        _expenseRepo.Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>());

        var query = new GetProfitLossQuery(tenantId, 2026, 3);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        // Other tenant's 9999 should NOT be included
        result.TotalRevenue.Should().Be(1000m);
    }

    [Fact]
    public async Task Handle_RevenueByPlatform_ShouldGroupCorrectly()
    {
        var tenantId = Guid.NewGuid();
        var orders = new List<Order>
        {
            CreateOrderWithAmount(tenantId, 500m, platform: PlatformType.Trendyol),
            CreateOrderWithAmount(tenantId, 300m, platform: PlatformType.Trendyol),
            CreateOrderWithAmount(tenantId, 1000m, platform: PlatformType.Hepsiburada),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);
        _expenseRepo.Setup(r => r.GetTotalByDateRangeAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        _expenseRepo.Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>());

        var query = new GetProfitLossQuery(tenantId, 2026, 3);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.RevenueByPlatform.Should().HaveCount(2);
        result.RevenueByPlatform.First().Revenue.Should().Be(1000m); // HB first (ordered by revenue desc)
    }
}
