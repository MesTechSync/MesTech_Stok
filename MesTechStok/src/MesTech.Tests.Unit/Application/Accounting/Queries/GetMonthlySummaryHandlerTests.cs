using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetMonthlySummary;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Accounting.Queries;

/// <summary>
/// GetMonthlySummaryHandler tests — monthly summary report aggregation.
/// Verifies sales, expenses, commissions, returns and platform breakdown.
/// </summary>
[Trait("Category", "Unit")]
public class GetMonthlySummaryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock;
    private readonly Mock<IExpenseRepository> _expenseRepoMock;
    private readonly Mock<IIncomeRepository> _incomeRepoMock;
    private readonly GetMonthlySummaryHandler _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetMonthlySummaryHandlerTests()
    {
        _orderRepoMock = new Mock<IOrderRepository>();
        _expenseRepoMock = new Mock<IExpenseRepository>();
        _incomeRepoMock = new Mock<IIncomeRepository>();

        _sut = new GetMonthlySummaryHandler(
            _orderRepoMock.Object,
            _expenseRepoMock.Object,
            _incomeRepoMock.Object);
    }

    private static Order CreateOrder(
        Guid tenantId,
        decimal totalAmount,
        decimal taxAmount,
        OrderStatus status = OrderStatus.Confirmed,
        PlatformType? platform = null)
    {
        var order = new Order
        {
            TenantId = tenantId,
            OrderNumber = $"ORD-{Guid.NewGuid():N}"[..12],
            OrderDate = DateTime.UtcNow,
            SourcePlatform = platform
        };
        order.SetFinancials(totalAmount - taxAmount, taxAmount, totalAmount);
        if (status == OrderStatus.Cancelled)
        {
            order.Cancel();
        }
        return order;
    }

    private static Expense CreateExpense(Guid tenantId, decimal amount, ExpenseType type)
    {
        var expense = new Expense
        {
            TenantId = tenantId,
            ExpenseType = type,
            Description = "Test expense",
            Date = DateTime.UtcNow
        };
        expense.SetAmount(amount);
        return expense;
    }

    [Fact]
    public async Task Handle_WithOrdersAndExpenses_ReturnsCorrectSummary()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 1000m, 180m, OrderStatus.Confirmed, PlatformType.Trendyol),
            CreateOrder(_tenantId, 500m, 90m, OrderStatus.Confirmed, PlatformType.Trendyol),
            CreateOrder(_tenantId, 300m, 54m, OrderStatus.Cancelled)
        };

        var expenses = new List<Expense>
        {
            CreateExpense(_tenantId, 150m, ExpenseType.Komisyon),
            CreateExpense(_tenantId, 80m, ExpenseType.Kargo),
            CreateExpense(_tenantId, 50m, ExpenseType.Diger)
        };

        _orderRepoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _expenseRepoMock
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expenses);

        var query = new GetMonthlySummaryQuery(2026, 3, _tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
        result.TotalSales.Should().Be(1500m); // 1000 + 500 (cancelled excluded)
        result.TotalTaxDue.Should().Be(270m); // 180 + 90
        result.TotalOrders.Should().Be(2);
        result.TotalReturns.Should().Be(1);
        result.TotalExpenses.Should().Be(280m); // 150 + 80 + 50
        result.TotalCommissions.Should().Be(150m);
        result.TotalShippingCost.Should().Be(80m);
    }

    [Fact]
    public async Task Handle_NoOrdersNoExpenses_ReturnsZeroSummary()
    {
        // Arrange
        _orderRepoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        _expenseRepoMock
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense>());

        var query = new GetMonthlySummaryQuery(2026, 1, _tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.TotalSales.Should().Be(0m);
        result.TotalOrders.Should().Be(0);
        result.TotalReturns.Should().Be(0);
        result.TotalExpenses.Should().Be(0m);
        result.TotalCommissions.Should().Be(0m);
        result.TotalShippingCost.Should().Be(0m);
        result.SalesByPlatform.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_MultiplePlatforms_GroupsSalesByPlatform()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 1000m, 180m, OrderStatus.Confirmed, PlatformType.Trendyol),
            CreateOrder(_tenantId, 2000m, 360m, OrderStatus.Confirmed, PlatformType.Hepsiburada),
            CreateOrder(_tenantId, 500m, 90m, OrderStatus.Confirmed, PlatformType.Trendyol)
        };

        _orderRepoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _expenseRepoMock
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense>());

        var query = new GetMonthlySummaryQuery(2026, 3, _tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.SalesByPlatform.Should().HaveCount(2);
        result.SalesByPlatform.Should().Contain(p => p.Platform == "Hepsiburada" && p.Sales == 2000m);
        result.SalesByPlatform.Should().Contain(p => p.Platform == "Trendyol" && p.Sales == 1500m);
        // Sorted by sales descending
        result.SalesByPlatform[0].Sales.Should().BeGreaterThanOrEqualTo(result.SalesByPlatform[1].Sales);
    }

    [Fact]
    public async Task Handle_AllCancelledOrders_ZeroSalesButCountsReturns()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateOrder(_tenantId, 1000m, 180m, OrderStatus.Cancelled),
            CreateOrder(_tenantId, 500m, 90m, OrderStatus.Cancelled)
        };

        _orderRepoMock
            .Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _expenseRepoMock
            .Setup(r => r.GetByDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), _tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Expense>());

        var query = new GetMonthlySummaryQuery(2026, 3, _tenantId);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.TotalSales.Should().Be(0m);
        result.TotalOrders.Should().Be(0);
        result.TotalReturns.Should().Be(2);
    }
}
