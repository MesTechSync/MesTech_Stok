using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.ListTaxWithholdings;

/// <summary>
/// Stopaj kayitlarini listeleme handler.
/// ITaxWithholdingRepository uzerinden tenant ve tarih araligi filtresiyle sorgular.
/// </summary>
public class ListTaxWithholdingsHandler : IRequestHandler<ListTaxWithholdingsQuery, IReadOnlyList<TaxWithholdingDto>>
{
    private readonly ITaxWithholdingRepository _repository;

    public ListTaxWithholdingsHandler(ITaxWithholdingRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<TaxWithholdingDto>> Handle(ListTaxWithholdingsQuery request, CancellationToken cancellationToken)
    {
        var withholdings = await _repository.GetAllAsync(request.TenantId, request.StartDate, request.EndDate, cancellationToken);
        return withholdings.Select(w => new TaxWithholdingDto
        {
            Id = w.Id,
            TenantId = w.TenantId,
            InvoiceId = w.InvoiceId,
            TaxExclusiveAmount = w.TaxExclusiveAmount,
            Rate = w.Rate,
            WithholdingAmount = w.WithholdingAmount,
            TaxType = w.TaxType,
            CreatedAt = w.CreatedAt
        }).ToList().AsReadOnly();
    }
}
