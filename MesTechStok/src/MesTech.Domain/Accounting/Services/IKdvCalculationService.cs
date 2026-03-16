#pragma warning disable MA0048 // File name must match type name — companion records colocated with interface
namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// KDV (Katma Deger Vergisi) hesaplama servisi arayuzu.
/// Pazaryeri satis senaryosu: brut satis → KDV → komisyon → stopaj → net tutar.
/// Turk vergi mevzuatina uygun hesaplama: KDV %1, %10, %20.
/// </summary>
public interface IKdvCalculationService
{
    /// <summary>
    /// Pazaryeri satis senaryosu icin tam vergi akisini hesaplar.
    /// Senaryo: GrossSale → KDV → Commission → CommissionKDV → Stopaj → Net.
    /// </summary>
    /// <param name="input">Hesaplama girdileri.</param>
    /// <returns>Hesaplama sonucu — tum kalemler ve aciklama.</returns>
    KdvCalculationResult Calculate(KdvCalculationInput input);
}

/// <summary>
/// KDV hesaplama girdileri.
/// </summary>
/// <param name="GrossSaleAmount">Brut satis tutari (KDV haric), ornegin 1000 TL.</param>
/// <param name="KdvRate">KDV orani, ornegin 0.20 (%20).</param>
/// <param name="CommissionRate">Komisyon orani, ornegin 0.1299 (%12.99).</param>
/// <param name="WithholdingRate">Stopaj orani, ornegin 0.0 veya 0.20 (%20). 0 ise stopaj uygulanmaz.</param>
public record KdvCalculationInput(
    decimal GrossSaleAmount,
    decimal KdvRate,
    decimal CommissionRate,
    decimal WithholdingRate);

/// <summary>
/// KDV hesaplama sonucu — tum vergi kalemleri.
/// </summary>
/// <param name="GrossSale">Brut satis (KDV haric).</param>
/// <param name="KdvAmount">Hesaplanan KDV tutari.</param>
/// <param name="TotalWithKdv">KDV dahil toplam (GrossSale + KdvAmount).</param>
/// <param name="Commission">Komisyon tutari (GrossSale * CommissionRate).</param>
/// <param name="CommissionKdv">Komisyon uzerindeki KDV (Commission * KdvRate).</param>
/// <param name="Withholding">Stopaj tutari (GrossSale * WithholdingRate).</param>
/// <param name="NetToSeller">Saticiya kalan net tutar.</param>
/// <param name="Breakdown">Insan tarafindan okunabilir hesaplama aciklamasi.</param>
public record KdvCalculationResult(
    decimal GrossSale,
    decimal KdvAmount,
    decimal TotalWithKdv,
    decimal Commission,
    decimal CommissionKdv,
    decimal Withholding,
    decimal NetToSeller,
    string Breakdown);
