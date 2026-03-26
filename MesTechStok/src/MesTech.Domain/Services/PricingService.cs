namespace MesTech.Domain.Services;

/// <summary>
/// Fiyatlama domain servisi — saf iş kuralları.
/// </summary>
public sealed class PricingService
{
    /// <summary>
    /// Kar marjı hesaplar (%).
    /// </summary>
    public decimal CalculateProfitMargin(decimal purchasePrice, decimal salePrice)
    {
        if (purchasePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(purchasePrice), "Purchase price cannot be negative.");
        if (salePrice <= 0) return 0;
        return Math.Round((salePrice - purchasePrice) / salePrice * 100, 2);
    }

    /// <summary>
    /// İndirimli fiyat hesaplar.
    /// </summary>
    public decimal ApplyDiscount(decimal price, decimal discountRate)
    {
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        if (discountRate < 0 || discountRate > 100)
            throw new ArgumentOutOfRangeException(nameof(discountRate), "Discount rate must be between 0 and 100.");
        return Math.Round(price * (1 - discountRate / 100), 2);
    }

    /// <summary>
    /// KDV dahil fiyat hesaplar.
    /// </summary>
    public decimal CalculatePriceWithTax(decimal price, decimal taxRate)
    {
        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        if (taxRate < 0 || taxRate > 1)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate must be between 0 and 1.");
        return Math.Round(price * (1 + taxRate), 2);
    }

    /// <summary>
    /// KDV hariç fiyat hesaplar (tersten).
    /// </summary>
    public decimal CalculatePriceWithoutTax(decimal priceWithTax, decimal taxRate)
    {
        if (priceWithTax < 0)
            throw new ArgumentOutOfRangeException(nameof(priceWithTax), "Price cannot be negative.");
        if (taxRate < 0 || taxRate > 1)
            throw new ArgumentOutOfRangeException(nameof(taxRate), "Tax rate must be between 0 and 1.");
        return Math.Round(priceWithTax / (1 + taxRate), 2);
    }
}
