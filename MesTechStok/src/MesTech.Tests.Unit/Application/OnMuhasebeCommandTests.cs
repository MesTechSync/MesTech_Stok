using FluentAssertions;
using MesTech.Application.Features.Finance.Commands.CreateExpense;
using MesTech.Application.Commands.CreateIncome;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class OnMuhasebeCommandTests
{
    private readonly Mock<IIncomeRepository> _incomeRepo = new();
    private readonly Mock<IExpenseRepository> _expenseRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private CreateIncomeHandler IncomeHandler() =>
        new(_incomeRepo.Object, _unitOfWork.Object);

    private CreateExpenseHandler ExpenseHandler() =>
        new(_expenseRepo.Object, _unitOfWork.Object);

    // ── CreateIncome ──

    [Fact]
    public async Task CreateIncome_ValidCommand_ReturnsNonEmptyGuid()
    {
        var tenantId = Guid.NewGuid();
        var command = new CreateIncomeCommand(
            TenantId: tenantId,
            StoreId: null,
            Description: "Trendyol satış",
            Amount: 1500m,
            IncomeType: IncomeType.Satis,
            InvoiceId: null,
            Date: null,
            Note: null);

        var result = await IncomeHandler().Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _incomeRepo.Verify(r => r.AddAsync(It.Is<Income>(i =>
            i.TenantId == tenantId &&
            i.Amount == 1500m &&
            i.IncomeType == IncomeType.Satis)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateIncome_WithExplicitDate_DateIsPreserved()
    {
        var explicitDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var command = new CreateIncomeCommand(
            Guid.NewGuid(), null, "Hizmet geliri", 500m,
            IncomeType.Hizmet, null, explicitDate, "not");

        Income? captured = null;
        _incomeRepo
            .Setup(r => r.AddAsync(It.IsAny<Income>()))
            .Callback<Income>(i => captured = i);

        await IncomeHandler().Handle(command, CancellationToken.None);

        captured!.Date.Should().Be(explicitDate);
    }

    [Fact]
    public async Task CreateIncome_NullDate_UsesUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var command = new CreateIncomeCommand(
            Guid.NewGuid(), null, "desc", 100m,
            IncomeType.Diger, null, null, null);

        Income? captured = null;
        _incomeRepo
            .Setup(r => r.AddAsync(It.IsAny<Income>()))
            .Callback<Income>(i => captured = i);

        await IncomeHandler().Handle(command, CancellationToken.None);

        captured!.Date.Should().BeOnOrAfter(before);
        captured.Date.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }

    // ── CreateExpense ──

    [Fact]
    public async Task CreateExpense_ValidCommand_ReturnsNonEmptyGuid()
    {
        var tenantId = Guid.NewGuid();
        var command = new CreateExpenseCommand(
            TenantId: tenantId,
            StoreId: null,
            Description: "Kargo masrafı",
            Amount: 200m,
            ExpenseType: ExpenseType.Kargo,
            Date: null,
            Note: null);

        var result = await ExpenseHandler().Handle(command, CancellationToken.None);

        result.Should().NotBe(Guid.Empty);
        _expenseRepo.Verify(r => r.AddAsync(It.Is<Expense>(e =>
            e.TenantId == tenantId &&
            e.Amount == 200m &&
            e.ExpenseType == ExpenseType.Kargo)), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateExpense_NullDate_UsesUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var command = new CreateExpenseCommand(
            Guid.NewGuid(), null, "desc", 50m,
            ExpenseType.Diger, null, null);

        Expense? captured = null;
        _expenseRepo
            .Setup(r => r.AddAsync(It.IsAny<Expense>()))
            .Callback<Expense>(e => captured = e);

        await ExpenseHandler().Handle(command, CancellationToken.None);

        captured!.Date.Should().BeOnOrAfter(before);
        captured.Date.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task CreateExpense_IsRecurring_FlagPersisted()
    {
        var command = new CreateExpenseCommand(
            Guid.NewGuid(), null, "Kira ödemesi", 5000m,
            ExpenseType.Kira, null, null,
            IsRecurring: true,
            RecurrencePeriod: "Aylik");

        Expense? captured = null;
        _expenseRepo
            .Setup(r => r.AddAsync(It.IsAny<Expense>()))
            .Callback<Expense>(e => captured = e);

        await ExpenseHandler().Handle(command, CancellationToken.None);

        captured!.IsRecurring.Should().BeTrue();
        captured.RecurrencePeriod.Should().Be("Aylik");
    }
}
