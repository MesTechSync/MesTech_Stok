using FluentAssertions;
using MesTech.Application.Queries.GetIncomeById;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetIncomeByIdHandlerTests
{
    private readonly Mock<IIncomeRepository> _repo;
    private readonly GetIncomeByIdHandler _sut;

    public GetIncomeByIdHandlerTests()
    {
        _repo = new Mock<IIncomeRepository>();
        _sut = new GetIncomeByIdHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ExistingIncome_CallsRepository()
    {
        var income = new Income { Description = "Test" };
        _repo.Setup(r => r.GetByIdAsync(income.Id, It.IsAny<CancellationToken>())).ReturnsAsync(income);

        var result = await _sut.Handle(new GetIncomeByIdQuery(income.Id), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Income?)null);

        var result = await _sut.Handle(new GetIncomeByIdQuery(id), CancellationToken.None);

        result.Should().BeNull();
    }
}
