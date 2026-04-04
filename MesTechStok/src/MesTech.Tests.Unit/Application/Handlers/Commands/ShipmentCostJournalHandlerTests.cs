using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ShipmentCostJournalHandlerCommandTests
{
    private readonly Mock<ICargoExpenseRepository> _cargoRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ShipmentCostJournalHandler>> _logger = new();

    private ShipmentCostJournalHandler CreateSut() =>
        new(_cargoRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ZeroCost_ShouldSkip()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "TRK-001", "Yurtici", 0m, CancellationToken.None);

        _cargoRepo.Verify(r => r.AddAsync(It.IsAny<CargoExpense>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NegativeCost_ShouldSkip()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "TRK-002", "Aras", -10m, CancellationToken.None);

        _cargoRepo.Verify(r => r.AddAsync(It.IsAny<CargoExpense>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValidCost_ShouldCreateCargoExpenseAndSave()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "TRK-100", "Surat", 25.50m, CancellationToken.None);

        _cargoRepo.Verify(r => r.AddAsync(
            It.Is<CargoExpense>(e => e.Cost == 25.50m && e.CarrierName == "Surat"),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassTrackingNumber()
    {
        CargoExpense? captured = null;
        _cargoRepo.Setup(r => r.AddAsync(It.IsAny<CargoExpense>(), It.IsAny<CancellationToken>()))
            .Callback<CargoExpense, CancellationToken>((e, _) => captured = e);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "TRK-TRACK", "Yurtici", 30m, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TrackingNumber.Should().Be("TRK-TRACK");
    }
}
