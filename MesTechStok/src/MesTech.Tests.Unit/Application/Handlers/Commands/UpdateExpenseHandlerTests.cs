using FluentAssertions;
using MesTech.Application.Commands.UpdateExpense;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: UpdateExpenseHandler testi — gider güncelleme.
/// P1 iş-kritik: muhasebe verileri tutarsız olmamalı.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class UpdateExpenseHandlerTests
{
    private readonly Mock<IExpenseRepository> _expenseRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private UpdateExpenseHandler CreateSut() => new(_expenseRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ExpenseNotFound_ShouldThrowKeyNotFound()
    {
        _expenseRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Expense?)null);
        var cmd = new UpdateExpenseCommand(Guid.NewGuid(), Description: "test");

        var act = () => CreateSut().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_UpdateDescription_ShouldChangeOnlyDescription()
    {
        var expense = new Expense { Description = "Old", TenantId = Guid.NewGuid() };
        expense.SetAmount(500m);
        _expenseRepo.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var cmd = new UpdateExpenseCommand(expense.Id, Description: "New Description");
        await CreateSut().Handle(cmd, CancellationToken.None);

        expense.Description.Should().Be("New Description");
        expense.Amount.Should().Be(500m); // unchanged
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateAmount_ShouldCallSetAmount()
    {
        var expense = new Expense { Description = "Kira", TenantId = Guid.NewGuid() };
        expense.SetAmount(1000m);
        _expenseRepo.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var cmd = new UpdateExpenseCommand(expense.Id, Amount: 1500m);
        await CreateSut().Handle(cmd, CancellationToken.None);

        expense.Amount.Should().Be(1500m);
    }

    [Fact]
    public async Task Handle_NullFields_ShouldNotChangeAnything()
    {
        var expense = new Expense { Description = "Original", TenantId = Guid.NewGuid(), ExpenseType = ExpenseType.Kira };
        expense.SetAmount(200m);
        _expenseRepo.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var cmd = new UpdateExpenseCommand(expense.Id); // all nulls
        await CreateSut().Handle(cmd, CancellationToken.None);

        expense.Description.Should().Be("Original");
        expense.Amount.Should().Be(200m);
        expense.ExpenseType.Should().Be(ExpenseType.Kira);
    }
}
