using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// ApproveInvoiceHandler: fatura onay akışı.
/// Sadece Draft durumundaki, kalemli, pozitif tutarlı fatura onaylanabilir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "Invoice")]
public class ApproveInvoiceHandlerTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<ApproveInvoiceHandler>> _logger = new();

    public ApproveInvoiceHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);
    }

    private ApproveInvoiceHandler CreateHandler() =>
        new(_invoiceRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_InvoiceNotFound_ReturnsFalse()
    {
        var missingId = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Invoice?)null);

        var cmd = new ApproveInvoiceCommand(missingId);
        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_ApproveThrowsInvalidOp_ReturnsFalse()
    {
        // Invoice.Approve() throws if not Draft or empty — simulate via mock
        var invoice = new Invoice();
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);

        var cmd = new ApproveInvoiceCommand(invoice.Id);
        var handler = CreateHandler();

        // Invoice is Draft but has 0 lines → Approve() will throw BusinessRuleException (which is InvalidOperationException)
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Should return false because Invoice.Approve() throws for empty invoice
        result.Should().BeFalse();
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
