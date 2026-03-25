namespace MesTech.Application.Interfaces.Accounting;

/// <summary>
/// Ba/Bs Form rapor servisi arayuzu.
/// Ba formu: 5.000 TL ustu alislar (tedarikci bazli).
/// Bs formu: 5.000 TL ustu satislar (musteri bazli).
/// </summary>
public interface IBaBsReportService
{
    /// <summary>
    /// Belirtilen ay/yil icin Ba/Bs formunu olusturur.
    /// </summary>
    Task<BaBsReportDto> GenerateBaBsReportAsync(
        Guid tenantId,
        int year,
        int month,
        CancellationToken ct = default);
}

/// <summary>
/// Ba/Bs form rapor sonucu.
/// </summary>
public sealed class BaBsReportDto
{
    public Guid TenantId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }

    /// <summary>
    /// Ba formu — 5.000 TL ustu alislar (tedarikci bazli).
    /// </summary>
    public List<BaBsCounterpartyDto> BaEntries { get; set; } = new();

    /// <summary>
    /// Bs formu — 5.000 TL ustu satislar (musteri bazli).
    /// </summary>
    public List<BaBsCounterpartyDto> BsEntries { get; set; } = new();

    public decimal TotalBaAmount => BaEntries.Sum(e => e.TotalAmount);
    public decimal TotalBsAmount => BsEntries.Sum(e => e.TotalAmount);
}

/// <summary>
/// Ba/Bs form'undaki karsi taraf bilgisi.
/// </summary>
public sealed class BaBsCounterpartyDto
{
    public string Name { get; set; } = string.Empty;
    public string VKN { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int DocumentCount { get; set; }
}
