using MediatR;
using MesTech.Application.DTOs.Finance;

namespace MesTech.Application.Features.Finance.Queries.GetProfitLoss;

public record GetProfitLossQuery(Guid TenantId, int Year, int Month) : IRequest<ProfitLossDto>;
