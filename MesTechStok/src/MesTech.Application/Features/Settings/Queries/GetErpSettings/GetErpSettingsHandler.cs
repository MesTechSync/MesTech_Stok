using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Settings.Queries.GetErpSettings;

public sealed class GetErpSettingsHandler : IRequestHandler<GetErpSettingsQuery, ErpSettingsDto>
{
    private readonly IErpSyncLogRepository _syncLogRepo;
    private readonly ICompanySettingsRepository _settingsRepo;

    public GetErpSettingsHandler(
        IErpSyncLogRepository syncLogRepo,
        ICompanySettingsRepository settingsRepo)
    {
        _syncLogRepo = syncLogRepo ?? throw new ArgumentNullException(nameof(syncLogRepo));
        _settingsRepo = settingsRepo ?? throw new ArgumentNullException(nameof(settingsRepo));
    }

    public async Task<ErpSettingsDto> Handle(GetErpSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _settingsRepo.GetByTenantIdAsync(request.TenantId, cancellationToken);
        var recentLogs = await _syncLogRepo.GetByTenantPagedAsync(request.TenantId, page: 1, pageSize: 10, cancellationToken);

        return new ErpSettingsDto
        {
            ActiveProvider = settings?.ErpProvider ?? ErpProvider.None,
            IsConnected = settings?.IsErpConnected ?? false,
            AutoSyncStock = settings?.AutoSyncStock ?? true,
            AutoSyncInvoice = settings?.AutoSyncInvoice ?? true,
            StockSyncPeriodMinutes = settings?.StockSyncPeriodMinutes ?? 30,
            PriceSyncPeriodMinutes = settings?.PriceSyncPeriodMinutes ?? 60,
            RecentSyncHistory = recentLogs.Select(log => new ErpSyncHistoryItemDto
            {
                SyncDate = log.AttemptedAt,
                SyncType = log.EntityType,
                RecordCount = log.TotalRecords,
                Status = log.Success ? "Basarili" : $"Hata: {log.ErrorMessage}",
                Duration = $"{log.DurationMs / 1000}s"
            }).ToList()
        };
    }
}
