namespace MesTech.Domain.Accounting.Enums;

/// <summary>
/// Ceza kaynagi — platform veya resmi kurum.
/// </summary>
public enum PenaltySource
{
    Trendyol = 0,
    Hepsiburada = 1,
    N11 = 2,
    Ciceksepeti = 3,
    Amazon = 4,
    eBay = 5,
    TaxAuthority = 10,
    SGK = 11,
    Customs = 12,
    Other = 99
}
