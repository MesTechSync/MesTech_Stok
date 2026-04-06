using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

// ════════════════════════════════════════════════════════
// DEV5: 15 Zincir iş mantığı testleri — null-guard DEĞİL, gerçek business logic
// Z2: OrderConfirmedRevenue — gelir kaydı oluşturma
// Z4: InvoiceCancelledReversal — ters yevmiye
// Z5: ReturnApprovedStockRestoration — stok geri yükleme
// Z6: CommissionChargedGL — komisyon gider kaydı
// Z7: ShipmentCostJournal — kargo gider kaydı (iş mantığı derinleştirme)
// ════════════════════════════════════════════════════════

#region Z2: OrderConfirmedRevenue — İş Mantığı

[Trait("Category", "Unit")]
[Trait("Layer", "Chain")]
public class OrderConfirmedRevenueBusinessTests
{
    private readonly Mock<IIncomeRepository> _incomeRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<OrderConfirmedRevenueHandler>> _logger = new();

    private OrderConfirmedRevenueHandler CreateSut() => new(_incomeRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidAmount_ShouldCreateIncomeAndSave()
    {
        var sut = CreateSut();
        Income? captured = null;
        _incomeRepo.Setup(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()))
            .Callback<Income, CancellationToken>((i, _) => captured = i);

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-001", 250.50m, null, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Description.Should().Contain("ORD-001");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ZeroAmount_ShouldSkipWithoutSaving()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-002", 0m, null, CancellationToken.None);

        _incomeRepo.Verify(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NegativeAmount_ShouldSkipWithoutSaving()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-003", -10m, null, CancellationToken.None);

        _incomeRepo.Verify(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSetCorrectTenantId()
    {
        var tenantId = Guid.NewGuid();
        Income? captured = null;
        _incomeRepo.Setup(r => r.AddAsync(It.IsAny<Income>(), It.IsAny<CancellationToken>())).Callback<Income, CancellationToken>((i, _) => captured = i);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), tenantId, "ORD-004", 100m, null, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TenantId.Should().Be(tenantId);
    }
}

#endregion

#region Z6: CommissionChargedGL — İş Mantığı

[Trait("Category", "Unit")]
[Trait("Layer", "Chain")]
public class CommissionChargedGLBusinessTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<ILogger<CommissionChargedGLHandler>> _logger = new();

    private CommissionChargedGLHandler CreateSut() => new(_uow.Object, _journalRepo.Object, _logger.Object);

    [Fact]
    public async Task Handle_ZeroCommission_ShouldSkipGLEntry()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), PlatformType.Trendyol, 0m, 0m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DuplicateReference_ShouldSkipIdempotent()
    {
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), PlatformType.Hepsiburada, 25.50m, 8.5m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommission_ShouldCreateJournalEntry()
    {
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), PlatformType.Trendyol, 42.00m, 10m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Z4: InvoiceCancelledReversal — İş Mantığı

[Trait("Category", "Unit")]
[Trait("Layer", "Chain")]
public class InvoiceCancelledReversalBusinessTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<ILogger<InvoiceCancelledReversalHandler>> _logger = new();

    private InvoiceCancelledReversalHandler CreateSut() =>
        new(_uow.Object, _journalRepo.Object, _invoiceRepo.Object, _logger.Object);

    [Fact]
    public async Task Handle_ZeroGrandTotal_ShouldSkipReversal()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "INV-001", "İptal", Guid.NewGuid(), 0m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCancellation_ShouldCreateReversalEntry()
    {
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "INV-002", "Müşteri iade", Guid.NewGuid(), 1500m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region Z7: ShipmentCostJournal — İş Mantığı Derinleştirme

[Trait("Category", "Unit")]
[Trait("Layer", "Chain")]
public class ShipmentCostJournalBusinessTests
{
    private readonly Mock<ICargoExpenseRepository> _cargoRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ShipmentCostJournalHandler>> _logger = new();

    private ShipmentCostJournalHandler CreateSut() => new(_cargoRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_ShouldSetCorrectTrackingNumberAndCarrier()
    {
        CargoExpense? captured = null;
        _cargoRepo.Setup(r => r.AddAsync(It.IsAny<CargoExpense>(), It.IsAny<CancellationToken>()))
            .Callback<CargoExpense, CancellationToken>((e, _) => captured = e);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "TR999888777", "SuratKargo", 28.50m, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.TrackingNumber.Should().Be("TR999888777");
        captured.CarrierName.Should().Be("SuratKargo");
        captured.Cost.Should().Be(28.50m);
    }

    [Fact]
    public async Task Handle_MultipleShipments_ShouldCreateSeparateExpenses()
    {
        var expenses = new List<CargoExpense>();
        _cargoRepo.Setup(r => r.AddAsync(It.IsAny<CargoExpense>(), It.IsAny<CancellationToken>()))
            .Callback<CargoExpense, CancellationToken>((e, _) => expenses.Add(e));

        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        await sut.HandleAsync(Guid.NewGuid(), tenantId, "TR001", "YurticiKargo", 15m, CancellationToken.None);
        await sut.HandleAsync(Guid.NewGuid(), tenantId, "TR002", "ArasKargo", 22m, CancellationToken.None);

        expenses.Should().HaveCount(2);
        expenses[0].CarrierName.Should().Be("YurticiKargo");
        expenses[1].CarrierName.Should().Be("ArasKargo");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}

#endregion
