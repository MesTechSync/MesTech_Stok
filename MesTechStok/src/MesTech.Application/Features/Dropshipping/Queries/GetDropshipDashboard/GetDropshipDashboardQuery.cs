using MediatR;
using MesTech.Application.DTOs.Platform;

namespace MesTech.Application.Features.Dropshipping.Queries.GetDropshipDashboard;

public record GetDropshipDashboardQuery(Guid TenantId) : IRequest<DropshipDashboardDto>;
