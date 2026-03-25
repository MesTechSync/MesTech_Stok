using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.GetQuotationById;

public sealed class GetQuotationByIdHandler : IRequestHandler<GetQuotationByIdQuery, QuotationDto?>
{
    private readonly IQuotationRepository _quotationRepository;

    public GetQuotationByIdHandler(IQuotationRepository quotationRepository)
    {
        _quotationRepository = quotationRepository ?? throw new ArgumentNullException(nameof(quotationRepository));
    }

    public async Task<QuotationDto?> Handle(GetQuotationByIdQuery request, CancellationToken cancellationToken)
    {
        var quotation = await _quotationRepository.GetByIdWithLinesAsync(request.Id).ConfigureAwait(false);
        if (quotation is null) return null;

        return MapToDto(quotation);
    }

    private static QuotationDto MapToDto(Quotation quotation)
    {
        return new QuotationDto
        {
            Id = quotation.Id,
            QuotationNumber = quotation.QuotationNumber,
            Status = quotation.Status.ToString(),
            QuotationDate = quotation.QuotationDate,
            ValidUntil = quotation.ValidUntil ?? DateTime.MinValue,
            CustomerName = quotation.CustomerName,
            CustomerTaxNumber = quotation.CustomerTaxNumber,
            CustomerEmail = quotation.CustomerEmail,
            SubTotal = quotation.SubTotal,
            TaxTotal = quotation.TaxTotal,
            GrandTotal = quotation.GrandTotal,
            Currency = quotation.Currency,
            Notes = quotation.Notes,
            ConvertedInvoiceId = quotation.ConvertedInvoiceId,
            Lines = quotation.Lines.Select(l => new QuotationLineDto
            {
                Id = l.Id,
                ProductName = l.ProductName,
                SKU = l.SKU,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                TaxRate = l.TaxRate,
                TaxAmount = l.TaxAmount,
                LineTotal = l.LineTotal,
                Description = l.Description,
            }).ToList()
        };
    }
}
