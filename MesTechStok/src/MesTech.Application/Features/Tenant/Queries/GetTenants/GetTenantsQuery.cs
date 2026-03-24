using MediatR;
using MesTech.Application.DTOs;

namespace MesTech.Application.Features.Tenant.Queries.GetTenants;

public record GetTenantsQuery(int Page = 1, int PageSize = 50) : IRequest<GetTenantsResult>;

public record GetTenantsResult(IReadOnlyList<TenantDto> Items, int TotalCount, int Page, int PageSize);
