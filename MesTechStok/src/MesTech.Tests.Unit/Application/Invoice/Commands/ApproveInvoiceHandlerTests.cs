// DISABLED: Referenced handlers/entities were removed from codebase. Re-enable when re-created.
#if false
using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Invoice.Commands;

[Trait("Category", "Unit")]
public class ApproveInvoiceHandlerTests
{
    private readonly Mock<IInvoiceRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<ApproveInvoiceHandler>> _logger = new();

    private ApproveInvoiceHandler CreateHandler() =>
        new(_repository.Object, _unitOfWork.Object, _logger.Object);

    [Fact]
    public async Task Handle_DraftInvoiceWithLines_ShouldApproveAndReturnTrue()
    {
        // Arrange
        var order = CreateTestOrder();
        var invoice = MesTech.Domain.Entities.Invoice.CreateForOrder(
            order, InvoiceType.EFatura, "INV-001");
        invoice.AddLine(CreateTestLine());

        _repository.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);

        var handler = CreateHandler();
        var command = new ApproveInvoiceCommand(invoice.Id);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        invoice.Status.Should().Be(InvoiceStatus.Queued);
        _repository.Verify(r => r.UpdateAsync(invoice), Times.Once);
    }

    [Fact]
    public async Task Handle_InvoiceNotFound_ShouldReturnFalse()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _repository.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((MesTech.Domain.Entities.Invoice?)null);

        var handler = CreateHandler();
        var command = new ApproveInvoiceCommand(missingId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _repository.Verify(r => r.UpdateAsync(It.IsAny<MesTech.Domain.Entities.Invoice>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvoiceAlreadySent_ShouldReturnFalse()
    {
        // Arrange — create a sent invoice (non-Draft) which should fail Approve()
        var order = CreateTestOrder();
        var invoice = MesTech.Domain.Entities.Invoice.CreateForOrder(
            order, InvoiceType.EArsiv, "INV-002");
        invoice.AddLine(CreateTestLine());
        invoice.Approve(); // Draft -> Queued
        invoice.MarkAsSent("GIB-123", "https://pdf.url"); // Queued -> Sent

        _repository.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);

        var handler = CreateHandler();
        var command = new ApproveInvoiceCommand(invoice.Id);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert — Domain throws BusinessRuleException for non-Draft invoice
        await act.Should().ThrowAsync<MesTech.Domain.Exceptions.BusinessRuleException>();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static Order CreateTestOrder()
    {
        var order = new Order
        {
            TenantId = Guid.NewGuid(),
            CustomerName = "Test Musteri",
        };
        order.AddItem(new OrderItem
        {
            TenantId = order.TenantId,
            ProductId = Guid.NewGuid(),
            ProductName = "Test Urun",
            ProductSKU = "SKU-TEST",
            Quantity = 1,
            UnitPrice = 100m,
            TotalPrice = 100m,
            TaxRate = 0.18m,
            TaxAmount = 18m
        });
        return order;
    }

    private static InvoiceLine CreateTestLine()
    {
        return new InvoiceLine
        {
            ProductName = "Test Urun",
            Quantity = 1,
            UnitPrice = 100m,
            TaxRate = 18,
            TaxAmount = 18m
        };
    }
}
#endif
