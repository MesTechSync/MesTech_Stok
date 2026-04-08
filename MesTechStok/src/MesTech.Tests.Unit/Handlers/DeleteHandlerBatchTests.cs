using FluentAssertions;
using MesTech.Application.Commands.DeleteStockLot;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

/// <summary>
/// Delete handler batch testleri — soft delete pattern doğrulama.
/// DeleteStockLot handler'ı referans — diğer 9 Delete handler aynı pattern.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class DeleteHandlerBatchTests
{
    private readonly Mock<IStockLotRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public DeleteHandlerBatchTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private DeleteStockLotHandler CreateSut() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Delete_ExistingEntity_ShouldSoftDelete()
    {
        var id = Guid.NewGuid();
        var lot = StockLot.Create(Guid.NewGuid(), Guid.NewGuid(), $"LOT-{id.ToString()[..8]}", 10, 100m);
        _repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(lot);

        var result = await CreateSut().Handle(new DeleteStockLotCommand(id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        lot.IsDeleted.Should().BeTrue("soft delete should set IsDeleted = true");
        lot.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Delete_NonExistingEntity_ShouldReturnError()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockLot?)null);

        var result = await CreateSut().Handle(new DeleteStockLotCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task Delete_ShouldCallSaveChanges()
    {
        var lot = StockLot.Create(Guid.NewGuid(), Guid.NewGuid(), "LOT-TEST", 5, 50m);
        _repo.Setup(r => r.GetByIdAsync(lot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lot);

        await CreateSut().Handle(new DeleteStockLotCommand(lot.Id), CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_NullRequest_ShouldThrow()
    {
        var act = () => CreateSut().Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Delete_ShouldSetDeletedAtToUtcNow()
    {
        var lot = StockLot.Create(Guid.NewGuid(), Guid.NewGuid(), "LOT-TEST", 5, 50m);
        _repo.Setup(r => r.GetByIdAsync(lot.Id, It.IsAny<CancellationToken>())).ReturnsAsync(lot);

        var before = DateTime.UtcNow;
        await CreateSut().Handle(new DeleteStockLotCommand(lot.Id), CancellationToken.None);

        lot.DeletedAt.Should().BeOnOrAfter(before);
    }
}
