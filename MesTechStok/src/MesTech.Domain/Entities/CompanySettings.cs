using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

public sealed class CompanySettings : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    // ERP Settings
    public ErpProvider ErpProvider { get; set; } = ErpProvider.None;
    public bool IsErpConnected { get; set; }
    public bool AutoSyncStock { get; set; } = true;
    public bool AutoSyncInvoice { get; set; } = true;
    public int StockSyncPeriodMinutes { get; set; } = 30;
    public int PriceSyncPeriodMinutes { get; set; } = 60;
}
