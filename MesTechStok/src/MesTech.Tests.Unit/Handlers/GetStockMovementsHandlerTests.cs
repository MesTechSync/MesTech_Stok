using FluentAssertions;
using MesTech.Application.Queries.GetStockMovements;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class GetStockMovementsHandlerTests
{
    private readonly Mock<IStockMovementRepository> _movementRepoMock = new();
    private readonly GetStockMovementsHandler _sut;

    public GetStockMovementsHandlerTests()
    {
        _sut = new GetStockMovementsHandler(_movementRepoMock.Object);
    }

    [Fact]
    public async Task Handle_ByProductId_ReturnsMovements()
    {
        var productId = Guid.NewGuid();
        var movements = new List<StockMovement>
        {
            new() { Id = Guid.NewGuid(), ProductId = productId, Quantity = 10, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), ProductId = productId, Quantity = -5, CreatedAt = DateTime.UtcNow }
        };
        _movementRepoMock.Setup(r => r.GetByProductIdAsync(productId)).ReturnsAsync(movements.AsReadOnly());

        var query = new GetStockMovementsQuery(ProductId: productId);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ByDateRange_ReturnsMovements()
    {
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;
        var movements = new List<StockMovement> { new() { Id = Guid.NewGuid(), Quantity = 10, CreatedAt = DateTime.UtcNow } };
        _movementRepoMock.Setup(r => r.GetByDateRangeAsync(from, to)).ReturnsAsync(movements.AsReadOnly());

        var query = new GetStockMovementsQuery(From: from, To: to);
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoFilters_ReturnsEmpty()
    {
        var query = new GetStockMovementsQuery();
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var act = () => _sut.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
