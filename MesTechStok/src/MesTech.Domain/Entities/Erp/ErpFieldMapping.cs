using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Erp;

/// <summary>
/// ERP alan eslestirmesi — MesTech alani ↔ ERP alani donusumu.
/// Parasut/Logo/Netsis/Nebim farkli alan isimleri kullanir.
/// TransformExpression ile format donusumu (tarih formati, para birimi vb.).
/// </summary>
public sealed class ErpFieldMapping : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string ErpType { get; private set; } = string.Empty;
    public string MesTechField { get; private set; } = string.Empty;
    public string ErpField { get; private set; } = string.Empty;
    public string? TransformExpression { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ErpFieldMapping() { }

    public static ErpFieldMapping Create(
        Guid tenantId, string erpType, string mesTechField, string erpField,
        bool isRequired = false, string? transformExpression = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId boş olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(erpType);
        ArgumentException.ThrowIfNullOrWhiteSpace(mesTechField);
        ArgumentException.ThrowIfNullOrWhiteSpace(erpField);

        return new ErpFieldMapping
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ErpType = erpType,
            MesTechField = mesTechField,
            ErpField = erpField,
            IsRequired = isRequired,
            TransformExpression = transformExpression,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateMapping(string erpField, string? transform)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(erpField);
        ErpField = erpField;
        TransformExpression = transform;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}
