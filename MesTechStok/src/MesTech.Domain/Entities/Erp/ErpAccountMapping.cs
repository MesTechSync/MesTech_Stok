using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities.Erp;

/// <summary>
/// MesTech hesap kodu ↔ ERP hesap kodu eslestirmesi.
/// G420: ErpAccountMapping entity for Avalonia ErpAccountMappingView.
/// </summary>
public sealed class ErpAccountMapping : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public ErpProvider Provider { get; private set; }

    // MesTech tarafı
    public string MesTechAccountCode { get; private set; } = string.Empty;
    public string MesTechAccountName { get; private set; } = string.Empty;
    public string MesTechAccountType { get; private set; } = string.Empty;

    // ERP tarafı
    public string ErpAccountCode { get; private set; } = string.Empty;
    public string ErpAccountName { get; private set; } = string.Empty;

    // Sync durumu
    public bool IsActive { get; private set; } = true;
    public DateTime? LastSyncAt { get; private set; }

    private ErpAccountMapping() { }

    public static ErpAccountMapping Create(
        Guid tenantId, ErpProvider provider,
        string mesTechCode, string mesTechName, string mesTechType,
        string erpCode, string erpName)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(mesTechCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(erpCode);

        return new ErpAccountMapping
        {
            TenantId = tenantId,
            Provider = provider,
            MesTechAccountCode = mesTechCode,
            MesTechAccountName = mesTechName,
            MesTechAccountType = mesTechType,
            ErpAccountCode = erpCode,
            ErpAccountName = erpName,
            IsActive = true
        };
    }

    public void UpdateErpAccount(string erpCode, string erpName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(erpCode);
        ErpAccountCode = erpCode;
        ErpAccountName = erpName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkSynced() { LastSyncAt = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
}
