using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class InvoiceApprovedGLHandlerCommandTests
{
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<IJournalEntryRepository> _journalRepo = new();
    private readonly Mock<ILogger<InvoiceApprovedGLHandler>> _logger = new();

    private InvoiceApprovedGLHandler CreateSut() =>
        new(_uow.Object, _journalRepo.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ZeroGrandTotal_ShouldSkipWithoutCreatingEntry()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "INV-001", 0m, 0m, 0m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NegativeTaxAmount_ShouldReject()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "INV-NEG", 100m, -10m, 90m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_NegativeNetAmount_ShouldReject()
    {
        var sut = CreateSut();

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "INV-NEGNET", 100m, 10m, -90m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_DuplicateReference_ShouldSkipIdempotently()
    {
        var tenantId = Guid.NewGuid();
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(tenantId, "INV-DUP", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), tenantId, "INV-DUP", 500m, 90m, 410m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ValidInvoice_ShouldCreateJournalEntryAndSave()
    {
        var tenantId = Guid.NewGuid();
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(tenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), tenantId, "INV-100", 118m, 18m, 100m, CancellationToken.None);

        _journalRepo.Verify(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ZeroTax_ShouldNotAddVatLine()
    {
        var tenantId = Guid.NewGuid();
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(tenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((e, _) => captured = e);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), tenantId, "INV-NOTAX", 100m, 0m, 100m, CancellationToken.None);

        captured.Should().NotBeNull();
        // Only 2 lines: 120 Receivables (debit) + 600 Sales (credit), no 391 VAT line
        captured!.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task HandleAsync_WithTax_ShouldAdd3Lines()
    {
        var tenantId = Guid.NewGuid();
        _journalRepo.Setup(r => r.ExistsByReferenceAsync(tenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        JournalEntry? captured = null;
        _journalRepo.Setup(r => r.AddAsync(It.IsAny<JournalEntry>(), It.IsAny<CancellationToken>()))
            .Callback<JournalEntry, CancellationToken>((e, _) => captured = e);

        var sut = CreateSut();
        await sut.HandleAsync(Guid.NewGuid(), tenantId, "INV-TAX", 118m, 18m, 100m, CancellationToken.None);

        captured.Should().NotBeNull();
        // 3 lines: 120 Receivables (debit), 600 Sales (credit), 391 VAT (credit)
        captured!.Lines.Should().HaveCount(3);
    }
}
