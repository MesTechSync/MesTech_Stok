using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Accounting.Queries.GetAccountingPeriods;

public sealed class GetAccountingPeriodsHandler
    : IRequestHandler<GetAccountingPeriodsQuery, IReadOnlyList<AccountingPeriodDto>>
{
    private readonly IAccountingPeriodRepository _repo;

    public GetAccountingPeriodsHandler(IAccountingPeriodRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<AccountingPeriodDto>> Handle(
        GetAccountingPeriodsQuery request, CancellationToken cancellationToken)
    {
        var periods = await _repo.GetByTenantAsync(
            request.TenantId, request.Year, cancellationToken).ConfigureAwait(false);

        return periods.Select(p => new AccountingPeriodDto(
            p.Id, p.Year, p.Month, p.StartDate, p.EndDate,
            p.IsClosed, p.ClosedAt)).ToList();
    }
}
