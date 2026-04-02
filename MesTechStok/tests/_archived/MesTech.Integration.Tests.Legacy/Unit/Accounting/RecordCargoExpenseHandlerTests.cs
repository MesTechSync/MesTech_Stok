using FluentAssertions;
using MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using ICargoExpenseRepository = MesTech.Application.Interfaces.Accounting.ICargoExpenseRepository;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Accounting;

/// <summary>
/// RecordCargoExpenseHandler: Z7 kargo→GL zinciri.
/// Kritik iş kuralları:
///   - CarrierName boş olamaz (domain guard)
///   - Cost negatif olamaz (domain guard)
///   - IsBilled başlangıçta false
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "AccountingChain")]
public class RecordCargoExpenseHandlerTests
{
    private readonly Mock<ICargoExpenseRepository> _repo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public RecordCargoExpenseHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _repo.Setup(r => r.AddAsync(It.IsAny<CargoExpense>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private RecordCargoExpenseHandler CreateHandler() => new(_repo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ValidExpense_PersistsAndReturnsGuid()
    {
        var cmd = new RecordCargoExpenseCommand(
            TenantId: Guid.NewGuid(),
            CarrierName: "Yurtiçi Kargo",
            Cost: 35.50m,
            OrderId: "ORD-001",
            TrackingNumber: "YK123456789");

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repo.Verify(r => r.AddAsync(It.IsAny<CargoExpense>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyCarrierName_ThrowsArgumentException()
    {
        var cmd = new RecordCargoExpenseCommand(
            Guid.NewGuid(), CarrierName: "", Cost: 30m);

        var handler = CreateHandler();

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NegativeCost_ThrowsArgumentOutOfRange()
    {
        var cmd = new RecordCargoExpenseCommand(
            Guid.NewGuid(), CarrierName: "Aras Kargo", Cost: -10m);

        var handler = CreateHandler();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CreatedExpense_IsNotBilledByDefault()
    {
        CargoExpense? captured = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<CargoExpense>(), It.IsAny<CancellationToken>()))
            .Callback<CargoExpense, CancellationToken>((ce, _) => captured = ce)
            .Returns(Task.CompletedTask);

        var cmd = new RecordCargoExpenseCommand(
            Guid.NewGuid(), "Sürat Kargo", 45m, "ORD-002", "SK987654321");

        var handler = CreateHandler();
        await handler.Handle(cmd, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.IsBilled.Should().BeFalse();
        captured.BilledAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ZeroCost_IsAllowed()
    {
        // Bazı kargo kampanyalarında maliyet 0 olabilir
        var cmd = new RecordCargoExpenseCommand(
            Guid.NewGuid(), "Bedava Kargo Kampanyası", Cost: 0m);

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}
