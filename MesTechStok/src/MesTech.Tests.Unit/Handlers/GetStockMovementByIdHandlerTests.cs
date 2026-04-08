using FluentAssertions;
using MesTech.Application.Queries.GetStockMovementById;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// GetStockMovementByIdHandler + GetStockMovementsHandler testleri.
/// Stok hareket sorgulama ekranı handler'ları.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetStockMovementByIdHandlerTests
{
    private readonly Mock<IStockMovementRepository> _repo = new();

    [Fact]
    public async Task Handle_ExistingId_ShouldReturnDto()
    {
        var id = Guid.NewGuid();
        var movement = new StockMovement
        {
            Id = id, ProductId = Guid.NewGuid(), Quantity = 10,
            MovementType = "Purchase", TenantId = Guid.NewGuid()
        };
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(movement);

        var handler = new GetStockMovementByIdHandler(_repo.Object);
        var result = await handler.Handle(new GetStockMovementByIdQuery(id), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_NonExistingId_ShouldReturnNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockMovement?)null);

        var handler = new GetStockMovementByIdHandler(_repo.Object);
        var result = await handler.Handle(new GetStockMovementByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrow()
    {
        var handler = new GetStockMovementByIdHandler(_repo.Object);
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullRepo_ShouldThrow()
    {
        var act = () => new GetStockMovementByIdHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ShouldCallRepository_ExactlyOnce()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((StockMovement?)null);

        var handler = new GetStockMovementByIdHandler(_repo.Object);
        await handler.Handle(new GetStockMovementByIdQuery(id), CancellationToken.None);

        _repo.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
