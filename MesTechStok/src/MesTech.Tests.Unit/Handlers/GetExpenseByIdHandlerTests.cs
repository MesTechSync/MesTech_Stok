using FluentAssertions;
using MesTech.Application.Queries.GetExpenseById;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetExpenseByIdHandlerTests
{
    private readonly Mock<IExpenseRepository> _repo;
    private readonly GetExpenseByIdHandler _sut;

    public GetExpenseByIdHandlerTests()
    {
        _repo = new Mock<IExpenseRepository>();
        _sut = new GetExpenseByIdHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ExistingExpense_CallsRepository()
    {
        var expense = new Expense { Description = "Test" };
        _repo.Setup(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(expense);

        var result = await _sut.Handle(new GetExpenseByIdQuery(expense.Id), CancellationToken.None);

        result.Should().NotBeNull();
        _repo.Verify(r => r.GetByIdAsync(expense.Id, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Expense?)null);

        var result = await _sut.Handle(new GetExpenseByIdQuery(id), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}
