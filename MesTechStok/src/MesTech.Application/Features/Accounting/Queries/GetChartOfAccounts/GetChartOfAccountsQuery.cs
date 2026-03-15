using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetChartOfAccounts;

public record GetChartOfAccountsQuery(Guid TenantId, bool? IsActive = true)
    : IRequest<IReadOnlyList<ChartOfAccountsDto>>;
