using MesTech.Domain.Enums;
using MediatR;

namespace MesTech.Application.Features.Settings.Queries.GetErpSettings;

public record GetErpSettingsQuery(Guid TenantId) : IRequest<ErpSettingsDto>;

public sealed class ErpSettingsDto
{
    public ErpProvider ActiveProvider { get; set; } = ErpProvider.None;
    public bool IsConnected { get; set; }
    public bool AutoSyncStock { get; set; }
    public bool AutoSyncInvoice { get; set; }
    public int StockSyncPeriodMinutes { get; set; }
    public int PriceSyncPeriodMinutes { get; set; }
    public List<ErpSyncHistoryItemDto> RecentSyncHistory { get; set; } = new();
}

public sealed class ErpSyncHistoryItemDto
{
    public DateTime SyncDate { get; set; }
    public string SyncType { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
}
