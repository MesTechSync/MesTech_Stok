using MediatR;
using MesTech.Application.DTOs.Dropshipping;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipOrders;

public record GetDropshipOrdersQuery(Guid TenantId)
    : IRequest<IReadOnlyList<DropshipOrderDto>>;
