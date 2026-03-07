using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Queries.GetStoresByTenant;

public record GetStoresByTenantQuery(int TenantId) : IRequest<IReadOnlyList<StoreDto>>;
