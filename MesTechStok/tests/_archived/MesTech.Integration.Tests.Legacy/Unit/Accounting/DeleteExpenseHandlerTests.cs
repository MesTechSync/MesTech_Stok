using FluentAssertions;
using MesTech.Application.Commands.DeleteExpense;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// DeleteExpenseHandler: soft delete pattern testi.
/// Kritik iş kuralları:
///   - Kayıt silinmez, IsDeleted=true + DeletedAt set edilir
///   - Kayıt bulunamazsa KeyNotFoundException
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "FinanceChain")]
public class DeleteExpenseHandlerTests
{
    private readonly Mock<IExpenseRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public DeleteExpenseHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<Expense>())).Returns(Task.CompletedTask);
    }

    private DeleteExpenseHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ExistingExpense_SoftDeletes()
    {
        var expense = new Expense { Description = "Test gider" };
        _repo.Setup(r => r.GetByIdAsync(expense.Id)).ReturnsAsync(expense);

        var cmd = new DeleteExpenseCommand(expense.Id);
        var handler = CreateHandler();

        await handler.Handle(cmd, CancellationToken.None);

        expense.IsDeleted.Should().BeTrue();
        expense.DeletedAt.Should().NotBeNull();
        expense.DeletedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _repo.Verify(r => r.UpdateAsync(expense), Times.Once);
    }

    [Fact]
    public async Task Handle_ExpenseNotFound_ThrowsKeyNotFound()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Expense?)null);

        var cmd = new DeleteExpenseCommand(Guid.NewGuid());
        var handler = CreateHandler();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }
}
