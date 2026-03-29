using MediatR;
using MesTech.Application.Behaviors;

namespace MesTech.Application.Features.Accounting.Queries.GetAccountingPeriods;

public record GetAccountingPeriodsQuery(Guid TenantId, int? Year = null)
    : IRequest<IReadOnlyList<AccountingPeriodDto>>, ICacheableQuery
{
    public string CacheKey => $"AccountingPeriods_{TenantId}_{Year}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
}

public record AccountingPeriodDto(
    Guid Id,
    int Year,
    int Month,
    DateTime StartDate,
    DateTime EndDate,
    bool IsClosed,
    DateTime? ClosedAt);
