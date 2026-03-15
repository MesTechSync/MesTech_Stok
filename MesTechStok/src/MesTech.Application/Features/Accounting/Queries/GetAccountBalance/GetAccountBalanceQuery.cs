using MediatR;
using MesTech.Application.DTOs.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetAccountBalance;

public record GetAccountBalanceQuery(Guid TenantId, Guid AccountId)
    : IRequest<AccountBalanceDto?>;
