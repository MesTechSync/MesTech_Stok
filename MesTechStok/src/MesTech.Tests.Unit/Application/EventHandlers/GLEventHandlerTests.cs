using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

// ════════════════════════════════════════════════════════
// DEV5 TUR 10: GL/Accounting event handler tests
// Coverage: CommissionChargedGL, InvoiceApprovedGL,
//           InvoiceCancelledReversal, OrderConfirmedRevenue,
//           OrderShippedCost, ReturnJournalReversal
// ════════════════════════════════════════════════════════

#region CommissionChargedGLHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class CommissionChargedGLHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<ILogger<CommissionChargedGLHandler>> _logger = new();

    private CommissionChargedGLHandler CreateSut() =>
        new(_uow.Object, _journalRepo.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateJournalEntryAndSave()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        await sut.HandleAsync(
            Guid.NewGuid(), tenantId, PlatformType.Trendyol,
            15.50m, 8.5m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(
            It.Is<JournalEntry>(je => je.TenantId == tenantId),
            It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeCommissionAmountInEntry()
    {
        var sut = CreateSut();
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je);

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), PlatformType.Hepsiburada,
            25.00m, 10.0m, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Amount.Should().Be(25.00m);
        captured.Description.Should().Contain("Hepsiburada");
    }
}

#endregion

#region InvoiceApprovedGLHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class InvoiceApprovedGLHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<ILogger<InvoiceApprovedGLHandler>> _logger = new();

    private InvoiceApprovedGLHandler CreateSut() =>
        new(_uow.Object, _journalRepo.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateJournalEntry()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-001",
            1180.00m, 180.00m, 1000.00m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(
            It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeInvoiceNumberInDescription()
    {
        var sut = CreateSut();
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je);

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-2026-042",
            2360.00m, 360.00m, 2000.00m, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Description.Should().Contain("INV-2026-042");
    }
}

#endregion

#region InvoiceCancelledReversalHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class InvoiceCancelledReversalHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<ILogger<InvoiceCancelledReversalHandler>> _logger = new();

    private InvoiceCancelledReversalHandler CreateSut() =>
        new(_uow.Object, _journalRepo.Object, _invoiceRepo.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateReversalJournalEntry()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-001",
            "Musteri talebi", Guid.NewGuid(), 1500.00m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(
            It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NullReason_ShouldNotThrow()
    {
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "INV-002",
            null, Guid.NewGuid(), 750.00m, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

#endregion

#region OrderConfirmedRevenueHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class OrderConfirmedRevenueHandlerTests
{
    private readonly Mock<IIncomeRepository> _incomeRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<OrderConfirmedRevenueHandler>> _logger = new();

    private OrderConfirmedRevenueHandler CreateSut() =>
        new(_incomeRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateIncomeRecordAndSave()
    {
        var sut = CreateSut();
        var tenantId = Guid.NewGuid();

        await sut.HandleAsync(
            Guid.NewGuid(), tenantId, "ORD-001",
            250.00m, Guid.NewGuid(), CancellationToken.None);

        _incomeRepo.Verify(r => r.AddAsync(
            It.IsAny<Income>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NullStoreId_ShouldNotThrow()
    {
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "ORD-002",
            100.00m, null, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

#endregion

#region OrderShippedCostHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class OrderShippedCostHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<ILogger<OrderShippedCostHandler>> _logger = new();

    private OrderShippedCostHandler CreateSut() =>
        new(_uow.Object, _journalRepo.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateShippingCostJournalEntry()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "TR123456789",
            CargoProvider.YurticiKargo, 45.50m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(
            It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeCargoProviderInDescription()
    {
        var sut = CreateSut();
        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je);

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "TR987",
            CargoProvider.ArasKargo, 32.00m, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Description.Should().Contain("Aras");
    }
}

#endregion

#region ReturnJournalReversalHandler

[Trait("Category", "Unit")]
[Trait("Layer", "Accounting")]
public class ReturnJournalReversalHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<ILogger<ReturnJournalReversalHandler>> _logger = new();

    private ReturnJournalReversalHandler CreateSut() =>
        new(_uow.Object, _journalRepo.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCreateReversalEntry()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            350.00m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(
            It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion
