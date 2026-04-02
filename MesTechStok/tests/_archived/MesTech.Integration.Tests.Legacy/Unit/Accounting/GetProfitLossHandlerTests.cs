using FluentAssertions;
using MesTech.Application.Features.Finance.Queries.GetProfitLoss;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using ExpenseStatus = MesTech.Domain.Enums.ExpenseStatus;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// GetProfitLossHandler: kar/zarar raporu — gelir vs gider hesaplama.
/// TotalRevenue - TotalExpenses = NetProfit.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "AccountingReport")]
public class GetProfitLossHandlerTests
{
    private readonly Mock<IFinanceExpenseRepository> _expenseRepo = new();
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Guid _tenantId = Guid.NewGuid();

    private GetProfitLossHandler CreateHandler() =>
        new(_expenseRepo.Object, _orderRepo.Object);

    [Fact]
    public async Task Handle_NoOrdersNoExpenses_ReturnsZeros()
    {
        _orderRepo.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Order>());
        _expenseRepo.Setup(r => r.GetTotalByDateRangeAsync(
                _tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);
        _expenseRepo.Setup(r => r.GetByTenantAsync(
                _tenantId, It.IsAny<ExpenseStatus?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MesTech.Domain.Entities.Finance.FinanceExpense>());

        var handler = CreateHandler();
        var result = await handler.Handle(
            new GetProfitLossQuery(_tenantId, 2026, 3), CancellationToken.None);

        result.TotalRevenue.Should().Be(0m);
        result.TotalExpenses.Should().Be(0m);
        result.Year.Should().Be(2026);
        result.Month.Should().Be(3);
    }

    [Fact]
    public async Task Handle_NullRequest_Throws()
    {
        var handler = CreateHandler();
        await Assert.ThrowsAnyAsync<Exception>(() =>
            handler.Handle(null!, CancellationToken.None));
    }
}
