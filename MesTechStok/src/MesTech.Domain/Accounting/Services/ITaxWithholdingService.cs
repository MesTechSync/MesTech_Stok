namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// Stopaj (tevkifat) hesaplama servisi arayuzu.
/// 9284 CB kurali: matrah = KDV HARIC tutar; komisyon/kargo matrahi DEGISTIRMEZ.
/// </summary>
public interface ITaxWithholdingService
{
    /// <summary>
    /// KDV haric tutar uzerinden stopaj tutarini hesaplar.
    /// </summary>
    /// <param name="taxExclusiveAmount">KDV haric tutar (matrah).</param>
    /// <param name="rate">Stopaj orani (0-1 arasi).</param>
    /// <returns>Stopaj tutari.</returns>
    decimal CalculateWithholding(decimal taxExclusiveAmount, decimal rate);

    /// <summary>
    /// KDV dahil tutardan KDV haric tutari cikarir.
    /// </summary>
    /// <param name="taxInclusiveAmount">KDV dahil tutar.</param>
    /// <param name="kdvRate">KDV orani (ornegin 0.20 = %20).</param>
    /// <returns>KDV haric tutar.</returns>
    decimal ExtractTaxExclusiveAmount(decimal taxInclusiveAmount, decimal kdvRate);
}
