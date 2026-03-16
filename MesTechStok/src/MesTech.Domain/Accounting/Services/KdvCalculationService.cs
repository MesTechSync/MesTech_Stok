using System.Globalization;
using System.Text;

namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// KDV (Katma Deger Vergisi) hesaplama servisi.
/// Pazaryeri satis senaryosu icin tam vergi akisini hesaplar.
///
/// Ornek senaryo (1000 TL satis, %20 KDV, %12.99 komisyon, %0 stopaj):
///   1. Brut Satis (KDV haric): 1.000,00 TL
///   2. KDV Tutari: 1.000 * 0.20 = 200,00 TL
///   3. KDV Dahil Toplam: 1.200,00 TL
///   4. Komisyon: 1.000 * 0.1299 = 129,90 TL
///   5. Komisyon KDV: 129,90 * 0.20 = 25,98 TL
///   6. Stopaj: 0,00 TL
///   7. Saticiya Net: 1.200,00 - 129,90 - 25,98 - 0,00 = 1.044,12 TL
///
/// Stopajli senaryo (1000 TL satis, %20 KDV, %12.99 komisyon, %20 stopaj):
///   6. Stopaj: 1.000 * 0.20 = 200,00 TL (matrah: KDV haric tutar)
///   7. Saticiya Net: 1.200,00 - 129,90 - 25,98 - 200,00 = 844,12 TL
///
/// Turk vergi mevzuati gercek oranlari:
///   KDV: %1, %10, %20
///   Stopaj: %0, %10, %20 (9284 CB, matrah = KDV haric tutar)
///   Komisyon: platform bazli (%10-%25 arasi)
/// </summary>
public class KdvCalculationService : IKdvCalculationService
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <inheritdoc />
    public KdvCalculationResult Calculate(KdvCalculationInput input)
    {
        ValidateInput(input);

        // 1. Brut satis (KDV haric)
        var grossSale = input.GrossSaleAmount;

        // 2. KDV hesaplama
        var kdvAmount = Math.Round(grossSale * input.KdvRate, 2);

        // 3. KDV dahil toplam
        var totalWithKdv = grossSale + kdvAmount;

        // 4. Komisyon (brut satis uzerinden)
        var commission = Math.Round(grossSale * input.CommissionRate, 2);

        // 5. Komisyon KDV (komisyon uzerindeki KDV)
        var commissionKdv = Math.Round(commission * input.KdvRate, 2);

        // 6. Stopaj (matrah: KDV haric tutar — 9284 CB kurali)
        var withholding = Math.Round(grossSale * input.WithholdingRate, 2);

        // 7. Saticiya net tutar
        // Net = KDV dahil toplam - Komisyon - Komisyon KDV - Stopaj
        var netToSeller = totalWithKdv - commission - commissionKdv - withholding;

        // Insan tarafindan okunabilir hesaplama aciklamasi
        var breakdown = BuildBreakdown(
            grossSale, input.KdvRate, kdvAmount, totalWithKdv,
            input.CommissionRate, commission, commissionKdv,
            input.WithholdingRate, withholding, netToSeller);

        return new KdvCalculationResult(
            GrossSale: grossSale,
            KdvAmount: kdvAmount,
            TotalWithKdv: totalWithKdv,
            Commission: commission,
            CommissionKdv: commissionKdv,
            Withholding: withholding,
            NetToSeller: netToSeller,
            Breakdown: breakdown);
    }

    private static void ValidateInput(KdvCalculationInput input)
    {
        if (input.GrossSaleAmount < 0)
            throw new ArgumentOutOfRangeException(
                nameof(input),
                "Brut satis tutari negatif olamaz.");

        if (input.KdvRate < 0 || input.KdvRate > 1)
            throw new ArgumentOutOfRangeException(
                nameof(input),
                "KDV orani 0 ile 1 arasinda olmalidir.");

        if (input.CommissionRate < 0 || input.CommissionRate > 1)
            throw new ArgumentOutOfRangeException(
                nameof(input),
                "Komisyon orani 0 ile 1 arasinda olmalidir.");

        if (input.WithholdingRate < 0 || input.WithholdingRate > 1)
            throw new ArgumentOutOfRangeException(
                nameof(input),
                "Stopaj orani 0 ile 1 arasinda olmalidir.");
    }

    private static string BuildBreakdown(
        decimal grossSale,
        decimal kdvRate,
        decimal kdvAmount,
        decimal totalWithKdv,
        decimal commissionRate,
        decimal commission,
        decimal commissionKdv,
        decimal withholdingRate,
        decimal withholding,
        decimal netToSeller)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== KDV Hesaplama Detayi ===");
        sb.AppendLine(Inv, $"1. Brut Satis (KDV haric): {grossSale:N2} TL");
        sb.AppendLine(Inv, $"2. KDV (%{kdvRate * 100:N0}): {grossSale:N2} x {kdvRate:N4} = {kdvAmount:N2} TL");
        sb.AppendLine(Inv, $"3. KDV Dahil Toplam: {totalWithKdv:N2} TL");
        sb.AppendLine(Inv, $"4. Komisyon (%{commissionRate * 100:N2}): {grossSale:N2} x {commissionRate:N4} = {commission:N2} TL");
        sb.AppendLine(Inv, $"5. Komisyon KDV (%{kdvRate * 100:N0}): {commission:N2} x {kdvRate:N4} = {commissionKdv:N2} TL");
        AppendWithholdingLine(sb, withholdingRate, grossSale, withholding);
        sb.AppendLine(Inv, $"7. Saticiya Net: {totalWithKdv:N2} - {commission:N2} - {commissionKdv:N2} - {withholding:N2} = {netToSeller:N2} TL");
        sb.AppendLine("============================");

        return sb.ToString();
    }

    private static void AppendWithholdingLine(
        StringBuilder sb,
        decimal withholdingRate,
        decimal grossSale,
        decimal withholding)
    {
        if (withholdingRate > 0)
        {
            sb.AppendLine(Inv, $"6. Stopaj (%{withholdingRate * 100:N0}): {grossSale:N2} x {withholdingRate:N4} = {withholding:N2} TL");
            sb.AppendLine("   (Matrah: KDV haric tutar -- 9284 CB kurali)");
        }
        else
        {
            sb.AppendLine("6. Stopaj: Uygulanmiyor (oran = %0)");
        }
    }
}
