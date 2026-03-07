namespace MesTech.Domain.Services;

/// <summary>
/// Fiyatlama domain servisi — saf iş kuralları.
/// </summary>
public class PricingService
{
    /// <summary>
    /// Kar marjı hesaplar (%).
    /// </summary>
    public decimal CalculateProfitMargin(decimal purchasePrice, decimal salePrice)
    {
        if (salePrice <= 0) return 0;
        return ((salePrice - purchasePrice) / salePrice) * 100;
    }

    /// <summary>
    /// İndirimli fiyat hesaplar.
    /// </summary>
    public decimal ApplyDiscount(decimal price, decimal discountRate)
    {
        if (discountRate < 0 || discountRate > 100) return price;
        return price * (1 - discountRate / 100);
    }

    /// <summary>
    /// KDV dahil fiyat hesaplar.
    /// </summary>
    public decimal CalculatePriceWithTax(decimal price, decimal taxRate)
    {
        return price * (1 + taxRate);
    }

    /// <summary>
    /// KDV hariç fiyat hesaplar (tersten).
    /// </summary>
    public decimal CalculatePriceWithoutTax(decimal priceWithTax, decimal taxRate)
    {
        if (taxRate <= -1) return priceWithTax;
        return priceWithTax / (1 + taxRate);
    }
}
