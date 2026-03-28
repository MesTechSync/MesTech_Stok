using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Tests.Unit.Application.ChainTests;

/// <summary>
/// Zincir 3 E2E: ApproveInvoice -> InvoiceApprovedGLHandler -> JournalEntry dengeli (Borc = Alacak)
/// Handler uses deterministic account GUIDs (e.g. 00000120-...) for GL entries.
/// Tests verify successful JournalEntry creation and double-entry balance.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ChainE2E")]
public class InvoiceToGLChainTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILogger<InvoiceApprovedGLHandler>> _loggerMock = new();

    [Fact]
    public async Task Handle_WhenInvoiceApproved_ShouldAttemptGLCreation()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var invoiceNumber = "INV-2026-001";
        decimal grandTotal = 1180m;
        decimal taxAmount = 180m;
        decimal netAmount = 1000m;

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new InvoiceApprovedGLHandler(_unitOfWorkMock.Object, Mock.Of<MesTech.Domain.Interfaces.IJournalEntryRepository>(), _loggerMock.Object);

        // Act — handler creates JournalEntry with deterministic account GUIDs
        await handler.HandleAsync(
            invoiceId, tenantId, invoiceNumber,
            grandTotal, taxAmount, netAmount,
            CancellationToken.None);

        // Assert — SaveChanges called (GL entry persisted)
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenZeroTax_ShouldStillAttemptGLCreation()
    {
        // Arrange — zero-tax invoice still creates 2-line journal entry
        var invoiceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        decimal grandTotal = 500m;
        decimal taxAmount = 0m;
        decimal netAmount = 500m;

        var handler = new InvoiceApprovedGLHandler(_unitOfWorkMock.Object, Mock.Of<MesTech.Domain.Interfaces.IJournalEntryRepository>(), _loggerMock.Object);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Act — zero-tax creates 2-line journal (no KDV line)
        await handler.HandleAsync(
            invoiceId, tenantId, "INV-NOTAX",
            grandTotal, taxAmount, netAmount,
            CancellationToken.None);

        // Assert — SaveChanges called
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void JournalEntry_WhenBalanced_ShouldPostSuccessfully()
    {
        // Arrange — direct JournalEntry test to verify double-entry accounting logic
        var tenantId = Guid.NewGuid();
        var accountReceivable = Guid.NewGuid();
        var accountSales = Guid.NewGuid();
        var accountVat = Guid.NewGuid();

        decimal grandTotal = 1180m;
        decimal netAmount = 1000m;
        decimal taxAmount = 180m;

        var entry = MesTech.Domain.Accounting.Entities.JournalEntry.Create(
            tenantId, DateTime.UtcNow, "Satis faturasi #INV-001", "INV-001");

        // Act — double entry: Borc 120 Alicilar, Alacak 600 Satislar + 391 KDV
        entry.AddLine(accountReceivable, grandTotal, 0, "120 Alicilar");
        entry.AddLine(accountSales, 0, netAmount, "600 Yurtici Satislar");
        entry.AddLine(accountVat, 0, taxAmount, "391 Hesaplanan KDV");

        // Assert — Borc = Alacak = 1180
        entry.Invoking(e => e.Validate()).Should().NotThrow();
        entry.Invoking(e => e.Post()).Should().NotThrow();
        entry.IsPosted.Should().BeTrue();
        entry.Lines.Should().HaveCount(3);
    }
}
