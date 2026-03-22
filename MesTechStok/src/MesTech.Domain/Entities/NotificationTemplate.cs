using MesTech.Domain.Common;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Entities;

/// <summary>
/// Bildirim sablonu entity'si.
/// Mustache-style placeholder destekli ({{productName}}, {{currentStock}}, vb.).
/// Kanal bazli (Email/Push/SMS/InApp) sablon yonetimi saglar.
/// </summary>
public class NotificationTemplate : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>
    /// Sablon adi — benzersiz tanimlayici.
    /// Ornek: "LowStockAlert", "OrderReceived", "DunningWarning".
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Bildirim konusu — Mustache placeholder destekli.
    /// Ornek: "Stok uyarisi: {{productName}}"
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Bildirim govdesi — Mustache placeholder destekli.
    /// Ornek: "{{productName}} stogu {{currentStock}} adede dustu. Kritik seviye: {{minStock}}"
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Bildirim kanali: Email, Push, SMS, InApp.
    /// </summary>
    public NotificationChannel Channel { get; set; } = NotificationChannel.Email;

    /// <summary>
    /// Sablon aktif mi? Devre disi sablonlar gonderim yapilmaz.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sablon dili — ISO 639-1 (varsayilan: "tr").
    /// </summary>
    public string Language { get; set; } = "tr";

    // ORM icin parametresiz constructor
    private NotificationTemplate() { }

    /// <summary>
    /// Yeni bildirim sablonu olusturur.
    /// </summary>
    public static NotificationTemplate Create(
        Guid tenantId,
        string templateName,
        string subject,
        string body,
        NotificationChannel channel,
        string language = "tr")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(body);

        return new NotificationTemplate
        {
            TenantId = tenantId,
            TemplateName = templateName,
            Subject = subject,
            Body = body,
            Channel = channel,
            Language = language,
            IsActive = true
        };
    }

    /// <summary>
    /// Sablonu devre disi birakir.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Sablonu tekrar aktif eder.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    public override string ToString() =>
        $"[{Channel}] {TemplateName} ({Language}) — Active={IsActive}";
}
