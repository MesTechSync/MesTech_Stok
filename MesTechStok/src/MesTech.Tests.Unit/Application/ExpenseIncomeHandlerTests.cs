using FluentAssertions;
using MesTech.Application.Commands.DeleteExpense;
using MesTech.Application.Commands.DeleteIncome;
using MesTech.Application.Commands.UpdateExpense;
using MesTech.Application.Commands.UpdateIncome;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class ExpenseIncomeHandlerTests
{
    private readonly Mock<IExpenseRepository> _expenseRepo = new();
    private readonly Mock<IIncomeRepository> _incomeRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    // ── DeleteExpense ──

    [Fact]
    public async Task DeleteExpense_ValidId_SoftDeletes()
    {
        var expense = new Expense { Description = "Test" };
        _expenseRepo.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var handler = new DeleteExpenseHandler(_expenseRepo.Object, _uow.Object);
        await handler.Handle(new DeleteExpenseCommand(expense.Id), CancellationToken.None);

        expense.IsDeleted.Should().BeTrue();
        expense.DeletedAt.Should().NotBeNull();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteExpense_NotFound_ThrowsKeyNotFound()
    {
        _expenseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expense?)null);
        var handler = new DeleteExpenseHandler(_expenseRepo.Object, _uow.Object);

        var act = () => handler.Handle(new DeleteExpenseCommand(Guid.NewGuid()), CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── UpdateExpense ──

    [Fact]
    public async Task UpdateExpense_ValidCommand_UpdatesDescription()
    {
        var expense = new Expense { Description = "Old" };
        _expenseRepo.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var cmd = new UpdateExpenseCommand(expense.Id, Description: "New Desc", Amount: 100m);
        var handler = new UpdateExpenseHandler(_expenseRepo.Object, _uow.Object);
        await handler.Handle(cmd, CancellationToken.None);

        expense.Description.Should().Be("New Desc");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── DeleteIncome ──

    [Fact]
    public async Task DeleteIncome_ValidId_SoftDeletes()
    {
        var income = new Income { Description = "Test" };
        _incomeRepo.Setup(r => r.GetByIdAsync(income.Id, It.IsAny<CancellationToken>())).ReturnsAsync(income);

        var handler = new DeleteIncomeHandler(_incomeRepo.Object, _uow.Object);
        await handler.Handle(new DeleteIncomeCommand(income.Id), CancellationToken.None);

        income.IsDeleted.Should().BeTrue();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── UpdateIncome ──

    [Fact]
    public async Task UpdateIncome_ValidCommand_UpdatesDescription()
    {
        var income = new Income { Description = "Old" };
        _incomeRepo.Setup(r => r.GetByIdAsync(income.Id, It.IsAny<CancellationToken>())).ReturnsAsync(income);

        var cmd = new UpdateIncomeCommand(income.Id, Description: "New Desc", Amount: 500m);
        var handler = new UpdateIncomeHandler(_incomeRepo.Object, _uow.Object);
        await handler.Handle(cmd, CancellationToken.None);

        income.Description.Should().Be("New Desc");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
