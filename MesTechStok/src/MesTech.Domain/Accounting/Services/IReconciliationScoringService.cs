namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Mutabakat eslestirme skorlama servisi arayuzu.
/// Banka hareketi ile hesap kesimi arasindaki benzerlik skorunu hesaplar.
/// 4 bileskenli skor: tutar (%40), tarih (%25), aciklama (%20), karsi taraf (%15).
/// </summary>
public interface IReconciliationScoringService
{
    /// <summary>
    /// Banka hareketi ile hesap kesimi arasindaki eslestirme guven skorunu hesaplar.
    /// </summary>
    /// <param name="bankAmount">Banka hareketi tutari.</param>
    /// <param name="settlementAmount">Hesap kesimi tutari.</param>
    /// <param name="bankDate">Banka hareketi tarihi.</param>
    /// <param name="settlementDate">Hesap kesimi tarihi.</param>
    /// <param name="bankDescription">Banka hareketi aciklamasi.</param>
    /// <param name="settlementPlatform">Platform adi.</param>
    /// <returns>0-1 arasi guven skoru.</returns>
    decimal CalculateConfidence(
        decimal bankAmount,
        decimal settlementAmount,
        DateTime bankDate,
        DateTime settlementDate,
        string? bankDescription = null,
        string? settlementPlatform = null);

    /// <summary>
    /// Otomatik eslestirme icin minimum guven esik degerini dondurur.
    /// >= 0.95 AutoMatched, 0.70-0.94 NeedsReview, &lt; 0.70 Unmatched.
    /// </summary>
    decimal AutoMatchThreshold { get; }

    /// <summary>
    /// Manuel inceleme icin minimum guven esik degerini dondurur.
    /// </summary>
    decimal ReviewThreshold { get; }

    /// <summary>
    /// Tutar benzerlik skorunu hesaplar (%40 agirlik).
    /// |delta| / expectedAmount tolerans: +/-0.5%.
    /// </summary>
    decimal CalculateAmountScore(decimal expected, decimal actual);

    /// <summary>
    /// Tarih benzerlik skorunu hesaplar (%25 agirlik).
    /// Platform-specific odeme penceresi: Trendyol T+7, Amazon T+14, vb.
    /// </summary>
    decimal CalculateDateScore(DateTime periodEnd, DateTime txDate, string platform);

    /// <summary>
    /// Aciklama benzerlik skorunu hesaplar (%20 agirlik).
    /// PayoutRef, platform adi, magaza kodu icerik eslesmesi.
    /// </summary>
    decimal CalculateTextScore(string? payoutRef, string? platform, string? txDescription);

    /// <summary>
    /// Karsi taraf benzerlik skorunu hesaplar (%15 agirlik).
    /// Bilinen platform odeme yapan firmalari ile eslestirme.
    /// </summary>
    decimal CalculateCounterpartyScore(string? platform, string? txCounterpartyName);

    /// <summary>
    /// Platform icin beklenen odeme penceresi gununu dondurur (T+N).
    /// </summary>
    int GetPlatformPaymentWindow(string platform);

    /// <summary>
    /// Bilinen platform odeme yapan firma isimlerini dondurur.
    /// </summary>
    IReadOnlyDictionary<string, string[]> GetKnownPlatformPayers();
}
