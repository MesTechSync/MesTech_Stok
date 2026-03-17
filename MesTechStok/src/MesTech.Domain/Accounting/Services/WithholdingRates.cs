namespace MesTech.Domain.Accounting.Services;

/// <summary>
/// KDV tevkifat oranlari — GiB (Gelir Idaresi Baskanligi) resmi listesi.
/// KDV Genel Uygulama Tebligi md. I/C-2.1.3.2.
/// Tevkifat: alici KDV'nin bir kismini keserek dogrudan vergi dairesine yatirir.
/// </summary>
public static class WithholdingRates
{
    /// <summary>2/10 tevkifat — Temizlik, cevre, bahce bakim hizmetleri.</summary>
    public const decimal TwoTenths = 0.20m;

    /// <summary>3/10 tevkifat — Yapi denetim hizmetleri (2024+).</summary>
    public const decimal ThreeTenths = 0.30m;

    /// <summary>4/10 tevkifat — Makine, techizat, demirci vb. imalat hizmetleri.</summary>
    public const decimal FourTenths = 0.40m;

    /// <summary>5/10 tevkifat — Isgucu temin, ozel guvenlik hizmetleri.</summary>
    public const decimal FiveTenths = 0.50m;

    /// <summary>7/10 tevkifat — Yapi isleri ve onarim, etut/plan/proje hizmetleri.</summary>
    public const decimal SevenTenths = 0.70m;

    /// <summary>9/10 tevkifat — Hurda metal ve atik teslimi.</summary>
    public const decimal NineTenths = 0.90m;

    /// <summary>
    /// Tum desteklenen tevkifat oranlarini dondurur.
    /// Her oran icin kod, aciklama ve oran degeri icerir.
    /// </summary>
    public static IReadOnlyList<WithholdingRateInfo> GetAll() =>
    [
        new("2/10", "Temizlik, cevre, bahce bakim hizmetleri", TwoTenths),
        new("3/10", "Yapi denetim hizmetleri", ThreeTenths),
        new("4/10", "Makine, techizat imalat hizmetleri", FourTenths),
        new("5/10", "Isgucu temin, ozel guvenlik hizmetleri", FiveTenths),
        new("7/10", "Yapi isleri, etut/plan/proje hizmetleri", SevenTenths),
        new("9/10", "Hurda metal ve atik teslimi", NineTenths)
    ];
}
