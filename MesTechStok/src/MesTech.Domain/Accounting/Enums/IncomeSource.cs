namespace MesTech.Domain.Accounting.Enums;

/// <summary>
/// Gelir kaynak kanali — platform bazli veya genel.
/// </summary>
public enum IncomeSource
{
    Manual = 0,
    Trendyol = 1,
    Hepsiburada = 2,
    N11 = 3,
    Ciceksepeti = 4,
    Amazon = 5,
    eBay = 6,
    Shopify = 7,
    WooCommerce = 8,
    OpenCart = 9,
    Pttavm = 10,
    Etsy = 11,
    DirectSale = 12,
    Service = 13,
    Interest = 14,
    Other = 99
}
