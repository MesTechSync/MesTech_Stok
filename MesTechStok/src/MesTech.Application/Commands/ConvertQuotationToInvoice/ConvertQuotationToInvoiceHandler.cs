using MediatR;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.ConvertQuotationToInvoice;

public sealed class ConvertQuotationToInvoiceHandler
    : IRequestHandler<ConvertQuotationToInvoiceCommand, ConvertQuotationToInvoiceResult>
{
    private readonly IQuotationRepository _quotationRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ConvertQuotationToInvoiceHandler(
        IQuotationRepository quotationRepository,
        IInvoiceRepository invoiceRepository,
        IUnitOfWork unitOfWork)
    {
        _quotationRepository = quotationRepository;
        _invoiceRepository = invoiceRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ConvertQuotationToInvoiceResult> Handle(
        ConvertQuotationToInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var quotation = await _quotationRepository.GetByIdWithLinesAsync(request.QuotationId, cancellationToken).ConfigureAwait(false);
        if (quotation is null)
            return new ConvertQuotationToInvoiceResult
            {
                IsSuccess = false,
                ErrorMessage = "Quotation not found."
            };

        var invoice = BuildInvoiceFromQuotation(request, quotation);

        // Mark quotation as converted
        try
        {
            quotation.MarkAsConverted(invoice.Id);
        }
        catch (InvalidOperationException ex)
        {
            return new ConvertQuotationToInvoiceResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }

        await _invoiceRepository.AddAsync(invoice, cancellationToken).ConfigureAwait(false);
        await _quotationRepository.UpdateAsync(quotation, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ConvertQuotationToInvoiceResult
        {
            IsSuccess = true,
            InvoiceId = invoice.Id
        };
    }

    private static Invoice BuildInvoiceFromQuotation(
        ConvertQuotationToInvoiceCommand request,
        Quotation quotation)
    {
        var invoice = new Invoice
        {
            InvoiceNumber = request.InvoiceNumber,
            TenantId = quotation.TenantId,
            Type = InvoiceType.EArsiv,
            // Status defaults to Draft
            CustomerName = quotation.CustomerName,
            CustomerTaxNumber = quotation.CustomerTaxNumber,
            CustomerTaxOffice = quotation.CustomerTaxOffice,
            CustomerAddress = quotation.CustomerAddress ?? string.Empty,
            CustomerEmail = quotation.CustomerEmail,
            Currency = quotation.Currency,
            InvoiceDate = DateTime.UtcNow,
        };

        foreach (var qLine in quotation.Lines)
        {
            var invoiceLine = new InvoiceLine
            {
                TenantId = quotation.TenantId,
                InvoiceId = invoice.Id,
                ProductId = qLine.ProductId,
                ProductName = qLine.ProductName,
                SKU = qLine.SKU,
                Quantity = qLine.Quantity,
                UnitPrice = qLine.UnitPrice,
                TaxRate = qLine.TaxRate / 100, // QuotationLine stores as % (e.g. 18), InvoiceLine as decimal (e.g. 0.18)
            };
            invoiceLine.CalculateLineTotal();
            invoice.AddLine(invoiceLine);
        }

        return invoice;
    }
}
