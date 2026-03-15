namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Mutabakat eslestirme skorlama servisi arayuzu.
/// Banka hareketi ile hesap kesimi arasindaki benzerlik skorunu hesaplar.
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
    /// </summary>
    decimal AutoMatchThreshold { get; }
}
