using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Platform attribute deger eslestirmesi — ic deger ↔ platform ID.
/// Ornek: "Kirmizi" (internal) → Trendyol attributeId=338, valueId=6980.
/// Her platformun farkli attribute ID sistemi var (Trendyol int, HB string, N11 SOAP field).
/// </summary>
public sealed class PlatformAttributeValueMapping : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string InternalValue { get; private set; } = string.Empty;
    public string InternalAttributeName { get; private set; } = string.Empty;
    public PlatformType PlatformType { get; private set; }
    public int? PlatformAttributeId { get; private set; }
    public int? PlatformValueId { get; private set; }
    public string? PlatformValueName { get; private set; }
    public bool IsAutoMapped { get; private set; }
    public bool IsSlicer { get; private set; }
    public bool IsVarianter { get; private set; }
    public bool IsLockedAfterApproval { get; private set; }

    private PlatformAttributeValueMapping() { }

    public static PlatformAttributeValueMapping Create(
        Guid tenantId,
        string internalAttributeName,
        string internalValue,
        PlatformType platform,
        int? platformAttributeId,
        int? platformValueId,
        string? platformValueName = null,
        bool isSlicer = false,
        bool isVarianter = false)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId bos olamaz.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(internalAttributeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(internalValue);

        return new PlatformAttributeValueMapping
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InternalAttributeName = internalAttributeName,
            InternalValue = internalValue,
            PlatformType = platform,
            PlatformAttributeId = platformAttributeId,
            PlatformValueId = platformValueId,
            PlatformValueName = platformValueName,
            IsSlicer = isSlicer,
            IsVarianter = isVarianter,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetAutoMapped(bool isAuto)
    {
        IsAutoMapped = isAuto;
        UpdatedAt = DateTime.UtcNow;
    }

    public void LockAfterApproval()
    {
        IsLockedAfterApproval = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePlatformIds(int? attributeId, int? valueId, string? valueName)
    {
        if (IsLockedAfterApproval)
            throw new InvalidOperationException("Onay sonrasi platform ID degistirilemez.");
        PlatformAttributeId = attributeId;
        PlatformValueId = valueId;
        PlatformValueName = valueName;
        UpdatedAt = DateTime.UtcNow;
    }
}
