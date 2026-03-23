using MediatR;

namespace MesTech.Application.Features.Tenant.Commands.CreateTenant;

public record CreateTenantCommand(string Name, string? TaxNumber) : IRequest<Guid>;
