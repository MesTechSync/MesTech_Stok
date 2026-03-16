using MesTech.Domain.Common;
using MesTech.Domain.Dropshipping.Enums;

namespace MesTech.Domain.Dropshipping.Entities;

/// <summary>
/// Dropshipping tedarikçisi. API bilgileri, kâr marjı ayarları ve otomatik senkronizasyon yapılandırması içerir.
/// ApiKey şifrelenmiş olarak saklanır.
/// </summary>
public class DropshipSupplier : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; private set; } = string.Empty;
    public string? WebsiteUrl { get; private set; }
    public string? ApiEndpoint { get; private set; }
    public string? ApiKey { get; private set; }
    public DropshipMarkupType MarkupType { get; private set; }
    public decimal MarkupValue { get; private set; }
    public bool AutoSync { get; private set; }
    public int SyncIntervalMinutes { get; private set; } = 60;
    public DateTime? LastSyncAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF Core parametresiz ctor
    private DropshipSupplier() { }

    /// <summary>
    /// Factory method — yeni tedarikçi oluşturur.
    /// </summary>
    public static DropshipSupplier Create(
        Guid tenantId,
        string name,
        string? websiteUrl,
        DropshipMarkupType markupType,
        decimal markupValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (markupValue < 0)
            throw new ArgumentException("Markup value cannot be negative.", nameof(markupValue));

        return new DropshipSupplier
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            WebsiteUrl = websiteUrl?.Trim(),
            MarkupType = markupType,
            MarkupValue = markupValue,
            AutoSync = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateMarkup(DropshipMarkupType type, decimal value)
    {
        if (value <= 0)
            throw new ArgumentException("Markup value must be greater than zero.", nameof(value));

        MarkupType = type;
        MarkupValue = value;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordSync()
    {
        LastSyncAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetApiCredentials(string endpoint, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);

        ApiEndpoint = endpoint.Trim();
        ApiKey = apiKey;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EnableAutoSync(int intervalMinutes = 60)
    {
        if (intervalMinutes < 1)
            throw new ArgumentException("Sync interval must be at least 1 minute.", nameof(intervalMinutes));

        AutoSync = true;
        SyncIntervalMinutes = intervalMinutes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableAutoSync()
    {
        AutoSync = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public override string ToString() => $"DropshipSupplier [{Name}] Markup:{MarkupType}={MarkupValue}";
}
