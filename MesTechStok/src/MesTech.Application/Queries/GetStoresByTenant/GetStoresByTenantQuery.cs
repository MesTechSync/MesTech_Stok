using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.GetStoresByTenant;

public record GetStoresByTenantQuery(Guid TenantId) : IRequest<IReadOnlyList<StoreDto>>;
