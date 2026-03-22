using FluentAssertions;
using MesTech.Application.Queries.GetExpenses;
using MesTech.Application.Queries.GetIncomes;
using MesTech.Application.Queries.GetKarZarar;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class OnMuhasebeQueryTests
{
    private readonly Mock<IIncomeRepository> _incomeRepo = new();
    private readonly Mock<IExpenseRepository> _expenseRepo = new();

    private GetIncomesHandler IncomesHandler() => new(_incomeRepo.Object);
    private GetExpensesHandler ExpensesHandler() => new(_expenseRepo.Object);
    private GetKarZararHandler KarZararHandler() =>
        new(_incomeRepo.Object, _expenseRepo.Object);

    private static Income MakeIncome(Guid tenantId, decimal amount, IncomeType type = IncomeType.Satis)
        => new() { TenantId = tenantId, Amount = amount, IncomeType = type,
                   Description = "test", Date = DateTime.UtcNow };

    private static Expense MakeExpense(Guid tenantId, decimal amount, ExpenseType type = ExpenseType.Diger)
    {
        var e = new Expense { TenantId = tenantId, ExpenseType = type,
                   Description = "test", Date = DateTime.UtcNow };
        e.SetAmount(amount);
        return e;
    }

    // ── GetIncomes ──

    [Fact]
    public async Task GetIncomes_NoFilter_ReturnsAllIncomes()
    {
        var tenantId = Guid.NewGuid();
        _incomeRepo.Setup(r => r.GetAllAsync(tenantId))
            .ReturnsAsync(new List<Income>
            {
                MakeIncome(tenantId, 1000m),
                MakeIncome(tenantId, 500m)
            });

        var result = await IncomesHandler().Handle(
            new GetIncomesQuery(TenantId: tenantId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Sum(i => i.Amount).Should().Be(1500m);
    }

    [Fact]
    public async Task GetIncomes_FilterByType_CallsGetByTypeAsync()
    {
        var tenantId = Guid.NewGuid();
        _incomeRepo.Setup(r => r.GetByTypeAsync(IncomeType.Hizmet, tenantId))
            .ReturnsAsync(new List<Income> { MakeIncome(tenantId, 200m, IncomeType.Hizmet) });

        var result = await IncomesHandler().Handle(
            new GetIncomesQuery(Type: IncomeType.Hizmet, TenantId: tenantId),
            CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].IncomeType.Should().Be(IncomeType.Hizmet);
        _incomeRepo.Verify(r => r.GetByTypeAsync(IncomeType.Hizmet, tenantId), Times.Once);
        _incomeRepo.Verify(r => r.GetAllAsync(It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task GetIncomes_FilterByDateRange_CallsGetByDateRangeAsync()
    {
        var tenantId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);
        _incomeRepo.Setup(r => r.GetByDateRangeAsync(from, to, tenantId))
            .ReturnsAsync(new List<Income> { MakeIncome(tenantId, 3000m) });

        var result = await IncomesHandler().Handle(
            new GetIncomesQuery(From: from, To: to, TenantId: tenantId),
            CancellationToken.None);

        result.Should().HaveCount(1);
        _incomeRepo.Verify(r => r.GetByDateRangeAsync(from, to, tenantId), Times.Once);
        _incomeRepo.Verify(r => r.GetAllAsync(It.IsAny<Guid?>()), Times.Never);
    }

    // ── GetExpenses ──

    [Fact]
    public async Task GetExpenses_NoFilter_ReturnsAllExpenses()
    {
        var tenantId = Guid.NewGuid();
        _expenseRepo.Setup(r => r.GetAllAsync(tenantId))
            .ReturnsAsync(new List<Expense>
            {
                MakeExpense(tenantId, 100m, ExpenseType.Kargo),
                MakeExpense(tenantId, 200m, ExpenseType.Komisyon)
            });

        var result = await ExpensesHandler().Handle(
            new GetExpensesQuery(TenantId: tenantId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Sum(e => e.Amount).Should().Be(300m);
    }

    [Fact]
    public async Task GetExpenses_FilterByType_CallsGetByTypeAsync()
    {
        var tenantId = Guid.NewGuid();
        _expenseRepo.Setup(r => r.GetByTypeAsync(ExpenseType.Kargo, tenantId))
            .ReturnsAsync(new List<Expense> { MakeExpense(tenantId, 150m, ExpenseType.Kargo) });

        var result = await ExpensesHandler().Handle(
            new GetExpensesQuery(Type: ExpenseType.Kargo, TenantId: tenantId),
            CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].ExpenseType.Should().Be(ExpenseType.Kargo);
        _expenseRepo.Verify(r => r.GetByTypeAsync(ExpenseType.Kargo, tenantId), Times.Once);
        _expenseRepo.Verify(r => r.GetAllAsync(It.IsAny<Guid?>()), Times.Never);
    }

    // ── GetKarZarar ──

    [Fact]
    public async Task GetKarZarar_GelirBuyukGider_NetKarPositive()
    {
        var tenantId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Utc);

        _incomeRepo.Setup(r => r.GetByDateRangeAsync(from, to, tenantId))
            .ReturnsAsync(new List<Income>
            {
                MakeIncome(tenantId, 5000m),
                MakeIncome(tenantId, 2000m)
            });
        _expenseRepo.Setup(r => r.GetByDateRangeAsync(from, to, tenantId))
            .ReturnsAsync(new List<Expense> { MakeExpense(tenantId, 1500m) });

        var result = await KarZararHandler().Handle(
            new GetKarZararQuery(from, to, tenantId), CancellationToken.None);

        result.ToplamGelir.Should().Be(7000m);
        result.ToplamGider.Should().Be(1500m);
        result.NetKar.Should().Be(5500m);
        result.DönemBasi.Should().Be(from);
        result.DönemSonu.Should().Be(to);
    }

    [Fact]
    public async Task GetKarZarar_GiderBuyukGelir_NetKarNegative()
    {
        var tenantId = Guid.NewGuid();
        var from = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 2, 28, 0, 0, 0, DateTimeKind.Utc);

        _incomeRepo.Setup(r => r.GetByDateRangeAsync(from, to, tenantId))
            .ReturnsAsync(new List<Income> { MakeIncome(tenantId, 1000m) });
        _expenseRepo.Setup(r => r.GetByDateRangeAsync(from, to, tenantId))
            .ReturnsAsync(new List<Expense>
            {
                MakeExpense(tenantId, 800m),
                MakeExpense(tenantId, 700m)
            });

        var result = await KarZararHandler().Handle(
            new GetKarZararQuery(from, to, tenantId), CancellationToken.None);

        result.NetKar.Should().Be(-500m, "gelir 1000 - gider 1500 = -500");
    }

    [Fact]
    public async Task GetKarZarar_EmptyPeriod_ReturnsZeroNetKar()
    {
        var tenantId = Guid.NewGuid();
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);

        _incomeRepo.Setup(r => r.GetByDateRangeAsync(from, to, tenantId))
            .ReturnsAsync(new List<Income>());
        _expenseRepo.Setup(r => r.GetByDateRangeAsync(from, to, tenantId))
            .ReturnsAsync(new List<Expense>());

        var result = await KarZararHandler().Handle(
            new GetKarZararQuery(from, to, tenantId), CancellationToken.None);

        result.NetKar.Should().Be(0m);
        result.ToplamGelir.Should().Be(0m);
        result.ToplamGider.Should().Be(0m);
    }
}
