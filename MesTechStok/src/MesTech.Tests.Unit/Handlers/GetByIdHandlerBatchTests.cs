using FluentAssertions;
using MesTech.Application.Queries.GetStockLotById;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// GetById handler batch testleri — basit delegation pattern doğrulama.
/// GetStockLotById referans — diğer 4 GetById handler aynı pattern.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetByIdHandlerBatchTests
{
    private readonly Mock<IStockLotRepository> _repo = new();

    [Fact]
    public async Task GetById_Existing_ShouldReturnDto()
    {
        var id = Guid.NewGuid();
        var lot = StockLot.Create(Guid.NewGuid(), Guid.NewGuid(), "LOT-001", 10, 100m);
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(lot);

        var handler = new GetStockLotByIdHandler(_repo.Object);
        var result = await handler.Handle(new GetStockLotByIdQuery(id), CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_NonExisting_ShouldReturnNull()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockLot?)null);

        var handler = new GetStockLotByIdHandler(_repo.Object);
        var result = await handler.Handle(new GetStockLotByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetById_NullRequest_ShouldThrow()
    {
        var handler = new GetStockLotByIdHandler(_repo.Object);
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetById_ShouldCallRepoOnce()
    {
        var id = Guid.NewGuid();
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((StockLot?)null);

        var handler = new GetStockLotByIdHandler(_repo.Object);
        await handler.Handle(new GetStockLotByIdQuery(id), CancellationToken.None);

        _repo.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Constructor_NullRepo_ShouldThrow()
    {
        var act = () => new GetStockLotByIdHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
