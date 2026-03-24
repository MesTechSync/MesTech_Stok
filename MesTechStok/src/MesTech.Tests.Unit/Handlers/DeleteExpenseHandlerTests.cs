using FluentAssertions;
using MesTech.Application.Commands.DeleteExpense;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class DeleteExpenseHandlerTests
{
    private readonly Mock<IExpenseRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly DeleteExpenseHandler _sut;

    public DeleteExpenseHandlerTests()
    {
        _repo = new Mock<IExpenseRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new DeleteExpenseHandler(_repo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ExistingExpense_SoftDeletes()
    {
        var expense = new Expense { Description = "Test" };
        _repo.Setup(r => r.GetByIdAsync(expense.Id)).ReturnsAsync(expense);

        await _sut.Handle(new DeleteExpenseCommand(expense.Id), CancellationToken.None);

        expense.IsDeleted.Should().BeTrue();
        expense.DeletedAt.Should().NotBeNull();
        _repo.Verify(r => r.UpdateAsync(expense), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Expense?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.Handle(new DeleteExpenseCommand(id), CancellationToken.None));
    }
}
