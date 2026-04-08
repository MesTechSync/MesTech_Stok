using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands.PlaceDropshipOrder;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

/// <summary>
/// DEV5: PlaceDropshipOrderHandler testi — dropship sipariş.
/// P1: Dropship operasyonlar tedarikçi entegrasyonunu tetikler.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class PlaceDropshipOrderHandlerTests
{
    private readonly Mock<IDropshipOrderRepository> _dropshipRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private PlaceDropshipOrderHandler CreateSut() => new(_dropshipRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_HappyPath_ShouldCreateAndReturnId()
    {
        DropshipOrder? captured = null;
        _dropshipRepo.Setup(r => r.AddAsync(It.IsAny<DropshipOrder>(), It.IsAny<CancellationToken>()))
            .Callback<DropshipOrder, CancellationToken>((o, _) => captured = o);

        var cmd = new PlaceDropshipOrderCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "SUP-REF-001");

        var result = await CreateSut().Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        captured.Should().NotBeNull();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPlaceWithSupplierRef()
    {
        DropshipOrder? captured = null;
        _dropshipRepo.Setup(r => r.AddAsync(It.IsAny<DropshipOrder>(), It.IsAny<CancellationToken>()))
            .Callback<DropshipOrder, CancellationToken>((o, _) => captured = o);

        var cmd = new PlaceDropshipOrderCommand(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "MY-SUPPLIER-REF");

        await CreateSut().Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.SupplierOrderRef.Should().Be("MY-SUPPLIER-REF");
    }
}
