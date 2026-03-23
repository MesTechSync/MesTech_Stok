using MediatR;
using MesTech.Application.Interfaces.Erp;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Erp.Queries.GetErpDashboard;

public class GetErpDashboardHandler : IRequestHandler<GetErpDashboardQuery, ErpDashboardDto>
{
    private readonly IErpSyncLogRepository _syncRepo;
    private readonly ILogger<GetErpDashboardHandler> _logger;

    public GetErpDashboardHandler(IErpSyncLogRepository syncRepo, ILogger<GetErpDashboardHandler> logger)
    {
        _syncRepo = syncRepo;
        _logger = logger;
    }

    public async Task<ErpDashboardDto> Handle(GetErpDashboardQuery request, CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var pendingRetries = await _syncRepo.GetPendingRetriesAsync(request.TenantId, DateTime.UtcNow, ct);

        return new ErpDashboardDto(
            ConnectedProviders: 0,
            TotalSyncToday: 0,
            FailedSyncToday: 0,
            PendingRetries: pendingRetries.Count,
            LastSyncAt: null);
    }
}
