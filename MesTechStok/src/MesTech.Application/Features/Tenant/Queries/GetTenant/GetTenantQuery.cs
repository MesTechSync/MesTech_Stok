using MediatR;

namespace MesTech.Application.Features.Tenant.Queries.GetTenant;

public record GetTenantQuery(Guid TenantId) : IRequest<TenantDto?>;

public record TenantDto(Guid Id, string Name, string? TaxNumber, bool IsActive);
