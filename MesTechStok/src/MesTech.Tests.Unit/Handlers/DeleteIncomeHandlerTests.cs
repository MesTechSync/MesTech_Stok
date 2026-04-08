using FluentAssertions;
using MesTech.Application.Commands.DeleteIncome;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class DeleteIncomeHandlerTests
{
    private readonly Mock<IIncomeRepository> _repo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly DeleteIncomeHandler _sut;

    public DeleteIncomeHandlerTests()
    {
        _repo = new Mock<IIncomeRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new DeleteIncomeHandler(_repo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ExistingIncome_SoftDeletes()
    {
        var income = new Income { Description = "Test" };
        _repo.Setup(r => r.GetByIdAsync(income.Id, It.IsAny<CancellationToken>())).ReturnsAsync(income);

        await _sut.Handle(new DeleteIncomeCommand(income.Id), CancellationToken.None);

        income.IsDeleted.Should().BeTrue();
        income.DeletedAt.Should().NotBeNull();
        _repo.Verify(r => r.UpdateAsync(income, It.IsAny<CancellationToken>()), Times.Once());
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NotFound_ThrowsKeyNotFoundException()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Income?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.Handle(new DeleteIncomeCommand(id), CancellationToken.None));
    }
}
