using MediatR;
using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipProfitability;

public record GetDropshipProfitabilityQuery(Guid TenantId) : IRequest<List<DropshipProfitDto>>;
