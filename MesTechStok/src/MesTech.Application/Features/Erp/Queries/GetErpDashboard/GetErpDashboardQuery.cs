using MediatR;

namespace MesTech.Application.Features.Erp.Queries.GetErpDashboard;

public record GetErpDashboardQuery(Guid TenantId) : IRequest<ErpDashboardDto>;

public record ErpDashboardDto(
    int ConnectedProviders,
    int TotalSyncToday,
    int FailedSyncToday,
    int PendingRetries,
    DateTime? LastSyncAt);
