using MesTech.Domain.Common;

namespace MesTech.Domain.Entities.Reporting;

/// <summary>
/// Kaydedilmis rapor sablonu entity'si.
/// Kullanicilar sik kullandiklari rapor filtrelerini kaydedip tekrar calistirabilir.
/// ReportType: Sales, Profitability, Stock, Commission, BaBs.
/// FilterJson: Rapor parametreleri JSON olarak saklanir.
/// </summary>
public sealed class SavedReport : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>
    /// Rapor adi — kullanici tarafindan verilen tanimlayici.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Rapor tipi: "Sales", "Profitability", "Stock", "Commission", "BaBs".
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Kaydedilmis sorgu parametreleri JSON olarak.
    /// Ornek: {"from":"2026-01-01","to":"2026-03-31","platform":"Trendyol"}
    /// </summary>
    public string FilterJson { get; set; } = "{}";

    /// <summary>
    /// Raporu olusturan kullanici ID'si.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Varsayilan rapor olarak isaretlenip isaretlenmedigini belirtir.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Raporun en son calistirilma zamani.
    /// </summary>
    public DateTime? LastExecutedAt { get; set; }

    // ORM icin parametresiz constructor
    private SavedReport() { }

    /// <summary>
    /// Yeni kaydedilmis rapor olusturur.
    /// </summary>
    public static SavedReport Create(
        Guid tenantId,
        string name,
        string reportType,
        string filterJson,
        Guid userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportType);

        return new SavedReport
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            ReportType = reportType,
            FilterJson = filterJson ?? "{}",
            CreatedByUserId = userId,
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Son calistirilma zamanini gunceller.
    /// </summary>
    public void MarkExecuted()
    {
        LastExecutedAt = DateTime.UtcNow;
    }

    public override string ToString() =>
        $"SavedReport: {Name} ({ReportType}) — Default={IsDefault}";
}
