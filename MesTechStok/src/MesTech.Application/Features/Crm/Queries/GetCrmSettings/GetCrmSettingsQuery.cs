using MediatR;

namespace MesTech.Application.Features.Crm.Queries.GetCrmSettings;

public record GetCrmSettingsQuery(Guid TenantId) : IRequest<CrmSettingsDto>;
