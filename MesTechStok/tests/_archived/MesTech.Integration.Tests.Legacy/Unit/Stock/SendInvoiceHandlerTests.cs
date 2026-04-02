using FluentAssertions;
using MesTech.Application.Commands.SendInvoice;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// SendInvoiceHandler: faturayı "Gönderildi" olarak işaretler.
/// Gerçek e-fatura gönderimi domain event / consumer ile yapılır.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "Invoice")]
public class SendInvoiceHandlerTests
{
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<SendInvoiceHandler>> _logger = new();

    public SendInvoiceHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private SendInvoiceHandler CreateHandler() =>
        new(_invoiceRepo.Object, _uow.Object, _logger.Object);

    [Fact]
    public async Task Handle_ValidInvoice_MarksAsSentAndReturnsSuccess()
    {
        var invoice = new Invoice();
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);

        var cmd = new SendInvoiceCommand(invoice.Id);
        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _invoiceRepo.Verify(r => r.UpdateAsync(invoice), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvoiceNotFound_ReturnsFailure()
    {
        var missingId = Guid.NewGuid();
        _invoiceRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Invoice?)null);

        var cmd = new SendInvoiceCommand(missingId);
        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_RepositoryThrows_ReturnsFalseWithMessage()
    {
        var invoice = new Invoice();
        _invoiceRepo.Setup(r => r.GetByIdAsync(invoice.Id)).ReturnsAsync(invoice);
        _invoiceRepo.Setup(r => r.UpdateAsync(It.IsAny<Invoice>()))
            .ThrowsAsync(new InvalidOperationException("DB bağlantı hatası"));

        var cmd = new SendInvoiceCommand(invoice.Id);
        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("hata");
    }
}
