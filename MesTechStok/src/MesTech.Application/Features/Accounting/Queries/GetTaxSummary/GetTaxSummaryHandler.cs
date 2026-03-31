using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetTaxSummary;

public sealed class GetTaxSummaryHandler : IRequestHandler<GetTaxSummaryQuery, TaxSummaryDto>
{
    private readonly ITaxRecordRepository _taxRecordRepo;
    private readonly ITaxWithholdingRepository _withholdingRepo;

    public GetTaxSummaryHandler(ITaxRecordRepository taxRecordRepo, ITaxWithholdingRepository withholdingRepo)
    {
        _taxRecordRepo = taxRecordRepo;
        _withholdingRepo = withholdingRepo;
    }

    public async Task<TaxSummaryDto> Handle(GetTaxSummaryQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var records = await _taxRecordRepo.GetByPeriodAsync(request.TenantId, request.Period, cancellationToken).ConfigureAwait(false);

        return new TaxSummaryDto
        {
            TotalTaxable = records.Sum(r => r.TaxableAmount),
            TotalTax = records.Sum(r => r.TaxAmount),
            TotalWithholding = 0, // Populated by separate withholding query if needed
            TotalPaid = records.Where(r => r.IsPaid).Sum(r => r.TaxAmount),
            TotalUnpaid = records.Where(r => !r.IsPaid).Sum(r => r.TaxAmount),
            Records = records.Select(r => new TaxRecordDto
            {
                Id = r.Id,
                Period = r.Period,
                TaxType = r.TaxType,
                TaxableAmount = r.TaxableAmount,
                TaxAmount = r.TaxAmount,
                DueDate = r.DueDate,
                IsPaid = r.IsPaid
            }).ToList()
        };
    }
}
