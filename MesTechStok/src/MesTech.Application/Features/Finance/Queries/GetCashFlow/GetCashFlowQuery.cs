using MediatR;
using MesTech.Application.DTOs.Finance;

namespace MesTech.Application.Features.Finance.Queries.GetCashFlow;

public record GetCashFlowQuery(Guid TenantId, int Year, int Month) : IRequest<CashFlowDto>;
