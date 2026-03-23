using MediatR;

namespace MesTech.Application.Features.Tenant.Commands.UpdateTenant;

public record UpdateTenantCommand(Guid TenantId, string Name, string? TaxNumber, bool IsActive) : IRequest<bool>;
