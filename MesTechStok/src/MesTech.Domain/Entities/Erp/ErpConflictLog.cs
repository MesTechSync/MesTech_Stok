using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Erp;

/// <summary>
/// Audit trail for ERP sync conflict resolutions.
/// Every detected conflict is logged with both values and the resolution decision.
/// </summary>
public class ErpConflictLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>ERP provider involved in the conflict.</summary>
    public ErpProvider Provider { get; private set; }

    /// <summary>Entity type: "Stock", "Price", "Account", "Order", "Invoice".</summary>
    public string EntityType { get; private set; } = string.Empty;

    /// <summary>Entity identifier code (product code, account code, etc.).</summary>
    public string EntityCode { get; private set; } = string.Empty;

    /// <summary>MesTech-side value at time of conflict.</summary>
    public string MestechValue { get; private set; } = string.Empty;

    /// <summary>ERP-side value at time of conflict.</summary>
    public string ErpValue { get; private set; } = string.Empty;

    /// <summary>Winner: "MesTech" or "Erp".</summary>
    public string Winner { get; private set; } = string.Empty;

    /// <summary>Resolution method: "Auto" or "Manual".</summary>
    public string Resolution { get; private set; } = "Auto";

    /// <summary>Timestamp when the conflict was resolved.</summary>
    public DateTimeOffset ResolvedAt { get; private set; }

    private ErpConflictLog() { }

    public static ErpConflictLog Create(
        Guid tenantId,
        ErpProvider provider,
        string entityType,
        string entityCode,
        string mestechValue,
        string erpValue,
        string winner,
        string resolution = "Auto")
    {
        return new ErpConflictLog
        {
            TenantId = tenantId,
            Provider = provider,
            EntityType = entityType,
            EntityCode = entityCode,
            MestechValue = mestechValue,
            ErpValue = erpValue,
            Winner = winner,
            Resolution = resolution,
            ResolvedAt = DateTimeOffset.UtcNow
        };
    }
}
