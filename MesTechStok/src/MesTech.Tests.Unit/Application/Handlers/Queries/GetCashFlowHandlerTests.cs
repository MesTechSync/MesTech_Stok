using FluentAssertions;
using MesTech.Application.Features.Finance.Queries.GetCashFlow;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

/// <summary>
/// DEV5: GetCashFlowHandler testi — nakit akış raporu.
/// P1: Nakit akış = finansal planlama temeli.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetCashFlowHandlerTests
{
    private readonly Mock<IFinanceExpenseRepository> _expenseRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();

    private GetCashFlowHandler CreateSut() => new(_expenseRepo.Object, _orderRepo.Object);

    private static Order CreateOrderWithAmount(Guid tenantId, decimal totalAmount, OrderStatus status = OrderStatus.Confirmed)
    {
        var order = new Order { TenantId = tenantId, OrderNumber = $"O-{Guid.NewGuid().ToString()[..6]}", Status = status };
        order.SetFinancials(totalAmount * 0.82m, totalAmount * 0.18m, totalAmount);
        return order;
    }

    [Fact]
    public async Task Handle_NoData_ShouldReturnZeros()
    {
        var tenantId = Guid.NewGuid();
        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());
        _expenseRepo.Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>());

        var query = new GetCashFlowQuery(tenantId, 2026, 3);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.TotalInflows.Should().Be(0);
        result.TotalOutflows.Should().Be(0);
        result.NetCashFlow.Should().Be(0);
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
    }

    [Fact]
    public async Task Handle_CancelledOrders_ShouldBeExcluded()
    {
        var tenantId = Guid.NewGuid();
        var orders = new List<Order>
        {
            CreateOrderWithAmount(tenantId, 1000m, OrderStatus.Confirmed),
            CreateOrderWithAmount(tenantId, 500m, OrderStatus.Cancelled),
        };

        _orderRepo.Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);
        _expenseRepo.Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>());

        var query = new GetCashFlowQuery(tenantId, 2026, 3);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.TotalInflows.Should().Be(1000m);
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
        _expenseRepo.Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FinanceExpense>());

        var query = new GetCashFlowQuery(tenantId, 2026, 3);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.TotalInflows.Should().Be(1000m);
    }
}
