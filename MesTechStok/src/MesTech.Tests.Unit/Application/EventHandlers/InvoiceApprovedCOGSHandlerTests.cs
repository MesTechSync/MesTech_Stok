using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using IInvoiceRepository = MesTech.Domain.Interfaces.IInvoiceRepository;
using InvoiceEntity = MesTech.Domain.Entities.Invoice;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

/// <summary>
/// InvoiceApprovedCOGSHandler — Zincir 3b: BORÇ 621 SATM / ALACAK 153 Stok.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class InvoiceApprovedCOGSHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<IFifoCostCalculationService> _fifoService = new();
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public InvoiceApprovedCOGSHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private InvoiceApprovedCOGSHandler CreateSut() => new(
        _uow.Object, _journalRepo.Object, _invoiceRepo.Object,
        _fifoService.Object, Mock.Of<ILogger<InvoiceApprovedCOGSHandler>>());

    [Fact]
    public async Task Handle_ShouldCreateDebit621Credit153()
    {
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var invoice = InvoiceEntity.CreateForOrder(
            new Order { Id = orderId, OrderNumber = "ORD-COGS", TenantId = TenantId, CustomerId = Guid.NewGuid() },
            InvoiceType.EArsiv, "MES-COGS-001");
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);

        _fifoService.Setup(s => s.CalculateAllCOGSAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FifoCostResultDto> { new() { TotalCOGS = 500m } }.AsReadOnly());

        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je).Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(invoiceId, TenantId, "MES-COGS-001", 590m, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Lines.Should().Contain(l => l.AccountId == AccountingConstants.Account621Cogs && l.Debit > 0);
        captured.Lines.Should().Contain(l => l.AccountId == AccountingConstants.Account153Inventory && l.Credit > 0);
    }

    [Fact]
    public async Task Handle_DebitEqualsCredit()
    {
        var invoiceId = Guid.NewGuid();
        var invoice = InvoiceEntity.CreateForOrder(
            new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-BAL", TenantId = TenantId, CustomerId = Guid.NewGuid() },
            InvoiceType.EArsiv, "MES-BAL-001");
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
        _fifoService.Setup(s => s.CalculateAllCOGSAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FifoCostResultDto> { new() { TotalCOGS = 1000m } }.AsReadOnly());

        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((je, _) => captured = je).Returns(Task.CompletedTask);

        await CreateSut().HandleAsync(invoiceId, TenantId, "MES-BAL-001", 1000m, CancellationToken.None);

        captured!.Lines.Sum(l => l.Debit).Should().Be(captured.Lines.Sum(l => l.Credit));
    }

    [Fact]
    public async Task Handle_Idempotent_ShouldSkipDuplicate()
    {
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(TenantId, "COGS-MES-DUP", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await CreateSut().HandleAsync(Guid.NewGuid(), TenantId, "MES-DUP", 500m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ZeroCOGS_ShouldSkip()
    {
        var invoiceId = Guid.NewGuid();
        var invoice = InvoiceEntity.CreateForOrder(
            new Order { Id = Guid.NewGuid(), OrderNumber = "ORD-Z", TenantId = TenantId, CustomerId = Guid.NewGuid() },
            InvoiceType.EArsiv, "MES-Z-001");
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>())).ReturnsAsync(invoice);
        _fifoService.Setup(s => s.CalculateAllCOGSAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FifoCostResultDto> { new() { TotalCOGS = 0m } }.AsReadOnly());

        await CreateSut().HandleAsync(invoiceId, TenantId, "MES-Z-001", 500m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NoOrderId_ShouldSkip()
    {
        var invoiceId = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InvoiceEntity?)null);

        await CreateSut().HandleAsync(invoiceId, TenantId, "MES-NULL", 500m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
