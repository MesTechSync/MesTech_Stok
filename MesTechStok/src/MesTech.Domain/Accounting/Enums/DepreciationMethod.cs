namespace MesTech.Domain.Accounting.Enums;

/// <summary>
/// Amortisman yontemi — VUK md. 315.
/// </summary>
public enum DepreciationMethod
{
    /// <summary>
    /// Normal (Esit Payli) Amortisman — Maliyet / Faydali Omur.
    /// </summary>
    StraightLine = 0,

    /// <summary>
    /// Azalan Bakiyeler (Cift Azalan) — (Kalan Deger * 2) / Faydali Omur.
    /// VUK md. 315: Son yil kalan deger tamamen amortize edilir.
    /// </summary>
    DecliningBalance = 1
}
