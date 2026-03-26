using FluentAssertions;
using MesTech.Application.Commands.ConvertQuotationToInvoice;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// ConvertQuotationToInvoiceHandler: teklif→fatura dönüşüm zinciri.
/// Kritik iş kuralları:
///   - Quotation Accepted durumda olmalı (MarkAsConverted guard)
///   - Fatura satırları teklif satırlarından kopyalanmalı
///   - KDV oranı % → decimal dönüşümü (18 → 0.18)
///   - Teklif Converted olarak işaretlenmeli
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "InvoiceChain")]
public class ConvertQuotationToInvoiceHandlerTests
{
    private readonly Mock<IQuotationRepository> _quotationRepo = new();
    private readonly Mock<IInvoiceRepository> _invoiceRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public ConvertQuotationToInvoiceHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _invoiceRepo.Setup(r => r.AddAsync(It.IsAny<Invoice>())).Returns(Task.CompletedTask);
        _quotationRepo.Setup(r => r.UpdateAsync(It.IsAny<Quotation>())).Returns(Task.CompletedTask);
    }

    private ConvertQuotationToInvoiceHandler CreateHandler() =>
        new(_quotationRepo.Object, _invoiceRepo.Object, _uow.Object);

    private Quotation CreateAcceptedQuotation()
    {
        var q = new Quotation
        {
            TenantId = Guid.NewGuid(),
            QuotationNumber = "TEK-001",
            CustomerName = "Test Müşteri",
            Currency = "TRY"
        };
        q.AddLine(new QuotationLine
        {
            ProductName = "Ürün A",
            SKU = "SKU-001",
            Quantity = 5,
            UnitPrice = 100m,
            TaxRate = 18m // yüzde olarak
        });
        q.Send(); // Draft → Sent
        q.Accept(); // Sent → Accepted
        return q;
    }

    [Fact]
    public async Task Handle_AcceptedQuotation_CreatesInvoice()
    {
        var quotation = CreateAcceptedQuotation();
        _quotationRepo.Setup(r => r.GetByIdWithLinesAsync(quotation.Id)).ReturnsAsync(quotation);

        var cmd = new ConvertQuotationToInvoiceCommand(quotation.Id, "INV-001");
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.InvoiceId.Should().NotBeEmpty();
        _invoiceRepo.Verify(r => r.AddAsync(It.IsAny<Invoice>()), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_QuotationNotFound_ReturnsFailure()
    {
        _quotationRepo.Setup(r => r.GetByIdWithLinesAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Quotation?)null);

        var cmd = new ConvertQuotationToInvoiceCommand(Guid.NewGuid(), "INV-002");
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
        _invoiceRepo.Verify(r => r.AddAsync(It.IsAny<Invoice>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PendingQuotation_ReturnsFailure()
    {
        // Teklif henüz kabul edilmemiş (Pending) — MarkAsConverted fırlatır
        var quotation = new Quotation
        {
            TenantId = Guid.NewGuid(),
            QuotationNumber = "TEK-002",
            CustomerName = "Test",
            Currency = "TRY"
        };
        // Accept() çağrılmadı — hâlâ Pending
        _quotationRepo.Setup(r => r.GetByIdWithLinesAsync(quotation.Id)).ReturnsAsync(quotation);

        var cmd = new ConvertQuotationToInvoiceCommand(quotation.Id, "INV-003");
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Accepted");
    }
}
