using MediatR;

namespace MesTech.Application.Features.Accounting.Queries.GetAccountingPeriods;

public record GetAccountingPeriodsQuery(Guid TenantId, int? Year = null)
    : IRequest<IReadOnlyList<AccountingPeriodDto>>;

public record AccountingPeriodDto(
    Guid Id,
    int Year,
    int Month,
    DateTime StartDate,
    DateTime EndDate,
    bool IsClosed,
    DateTime? ClosedAt);
