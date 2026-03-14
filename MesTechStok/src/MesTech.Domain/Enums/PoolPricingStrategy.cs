namespace MesTech.Domain.Enums;

/// <summary>
/// Dropshipping havuzunda fiyat belirleme stratejisi.
/// </summary>
public enum PoolPricingStrategy
{
    /// <summary>
    /// Maliyet fiyatına yüzde ekle (ör. %30 kâr marjı).
    /// </summary>
    Markup = 0,

    /// <summary>
    /// Her ürün için sabit satış fiyatı kullan.
    /// </summary>
    Fixed = 1,

    /// <summary>
    /// Piyasa koşullarına göre dinamik fiyatlandırma.
    /// </summary>
    Dynamic = 2
}
