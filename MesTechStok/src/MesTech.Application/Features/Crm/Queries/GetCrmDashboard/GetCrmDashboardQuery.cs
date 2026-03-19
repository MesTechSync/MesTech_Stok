using MediatR;
using MesTech.Application.DTOs.Crm;

namespace MesTech.Application.Features.Crm.Queries.GetCrmDashboard;

public record GetCrmDashboardQuery(Guid TenantId) : IRequest<CrmDashboardDto>;
