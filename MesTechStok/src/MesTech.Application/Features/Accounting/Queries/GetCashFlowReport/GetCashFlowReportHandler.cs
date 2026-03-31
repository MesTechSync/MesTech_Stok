using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Enums;

namespace MesTech.Application.Features.Accounting.Queries.GetCashFlowReport;

public sealed class GetCashFlowReportHandler : IRequestHandler<GetCashFlowReportQuery, CashFlowReportDto>
{
    private readonly ICashFlowEntryRepository _repository;

    public GetCashFlowReportHandler(ICashFlowEntryRepository repository)
        => _repository = repository;

    public async Task<CashFlowReportDto> Handle(GetCashFlowReportQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entries = await _repository.GetByDateRangeAsync(request.TenantId, request.From, request.To, ct: cancellationToken).ConfigureAwait(false);
        var totalInflow = await _repository.GetTotalByDirectionAsync(request.TenantId, CashFlowDirection.Inflow, request.From, request.To, cancellationToken).ConfigureAwait(false);
        var totalOutflow = await _repository.GetTotalByDirectionAsync(request.TenantId, CashFlowDirection.Outflow, request.From, request.To, cancellationToken).ConfigureAwait(false);

        return new CashFlowReportDto
        {
            TotalInflow = totalInflow,
            TotalOutflow = totalOutflow,
            NetFlow = totalInflow - totalOutflow,
            Entries = entries.Select(e => new CashFlowEntryDto
            {
                Id = e.Id,
                EntryDate = e.EntryDate,
                Amount = e.Amount,
                Direction = e.Direction.ToString(),
                Category = e.Category,
                Description = e.Description,
                CounterpartyId = e.CounterpartyId,
                CounterpartyName = e.Counterparty?.Name
            }).ToList()
        };
    }
}
