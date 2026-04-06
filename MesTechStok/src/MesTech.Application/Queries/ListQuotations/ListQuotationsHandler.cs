using MediatR;
using MesTech.Application.DTOs;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Queries.ListQuotations;

public sealed class ListQuotationsHandler : IRequestHandler<ListQuotationsQuery, IReadOnlyList<QuotationDto>>
{
    private readonly IQuotationRepository _quotationRepository;

    public ListQuotationsHandler(IQuotationRepository quotationRepository)
    {
        _quotationRepository = quotationRepository ?? throw new ArgumentNullException(nameof(quotationRepository));
    }

    public async Task<IReadOnlyList<QuotationDto>> Handle(ListQuotationsQuery request, CancellationToken cancellationToken)
    {
        var quotations = request.Status.HasValue
            ? await _quotationRepository.GetByStatusAsync(request.Status.Value, cancellationToken).ConfigureAwait(false)
            : await _quotationRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        return quotations.Select(MapToDto).ToList().AsReadOnly();
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
            Lines = [] // List queries don't include lines for performance
        };
    }
}
