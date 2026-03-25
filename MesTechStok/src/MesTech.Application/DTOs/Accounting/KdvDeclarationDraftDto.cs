namespace MesTech.Application.DTOs.Accounting;

/// <summary>
/// KDV beyanname taslak raporu — aylik KDV1/KDV2 draft hesaplama.
/// Hesaplanan KDV (output) - Indirilecek KDV (input) = Odenecek/Devreden KDV.
/// </summary>
public sealed class KdvDeclarationDraftDto
{
    /// <summary>Donem (yyyy-MM formati, ornegin "2026-03").</summary>
    public string Period { get; set; } = string.Empty;

    // ═══ Output KDV (Hesaplanan) ═══

    /// <summary>Satis KDV tutari (391 Hesaplanan KDV hesabindan).</summary>
    public decimal SalesKdv { get; set; }

    /// <summary>Iade KDV duzeltme tutari (satis iadelerinden).</summary>
    public decimal ReturnKdvAdjustment { get; set; }

    /// <summary>Net cikti KDV = SalesKdv - ReturnKdvAdjustment.</summary>
    public decimal NetOutputKdv { get; set; }

    // ═══ Input KDV (Indirilecek) ═══

    /// <summary>Alis KDV tutari (191 Indirilecek KDV hesabindan).</summary>
    public decimal PurchaseKdv { get; set; }

    /// <summary>Komisyon uzerinden odenen KDV (platform komisyon faturalarindan).</summary>
    public decimal CommissionKdv { get; set; }

    /// <summary>Net girdi KDV = PurchaseKdv + CommissionKdv.</summary>
    public decimal NetInputKdv { get; set; }

    // ═══ Tevkifat (Withholding) ═══

    /// <summary>KDV tevkifat tutari (9015 kodu, kismi tevkifat).</summary>
    public decimal WithholdingKdv { get; set; }

    // ═══ Sonuc ═══

    /// <summary>
    /// Odenecek KDV = NetOutputKdv - NetInputKdv - WithholdingKdv.
    /// Negatifse devreden KDV olarak 190 hesabina kaydedilir.
    /// </summary>
    public decimal PayableKdv { get; set; }

    /// <summary>Onceki donemden devreden KDV (190 hesabindan).</summary>
    public decimal CarryForwardKdv { get; set; }

    /// <summary>Donem sonu net odenecek = PayableKdv - CarryForwardKdv.</summary>
    public decimal FinalPayableKdv { get; set; }

    /// <summary>Insan tarafindan okunabilir rapor metni (tr-TR format).</summary>
    public string ReportText { get; set; } = string.Empty;
}
