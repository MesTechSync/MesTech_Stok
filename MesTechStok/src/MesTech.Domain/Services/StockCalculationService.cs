using MesTech.Domain.Entities;
using MesTech.Domain.Exceptions;

namespace MesTech.Domain.Services;

/// <summary>
/// Stok hesaplama domain servisi — WAC (Weighted Average Cost), FEFO, seviye kontrol.
/// Saf iş kuralları, altyapı bağımlılığı yok.
/// </summary>
public sealed class StockCalculationService
{
    /// <summary>
    /// Weighted Average Cost (Ağırlıklı Ortalama Maliyet) hesaplar.
    /// </summary>
    public decimal CalculateWAC(int currentStock, decimal currentAvgCost, int addedQty, decimal addedUnitCost)
    {
        if (currentStock + addedQty <= 0) return 0;

        var totalValue = (currentStock * currentAvgCost) + (addedQty * addedUnitCost);
        return totalValue / (currentStock + addedQty);
    }

    /// <summary>
    /// Stok çıkışı yapılabilir mi kontrol eder.
    /// </summary>
    public void ValidateStockSufficiency(Product product, int requiredQuantity)
    {
        if (product.Stock < requiredQuantity)
            throw new InsufficientStockException(product.SKU, product.Stock, requiredQuantity);
    }

    /// <summary>
    /// FEFO sırasıyla lot seçer (First Expire First Out).
    /// </summary>
    public IReadOnlyList<InventoryLot> SelectLotsForConsumption(
        IEnumerable<InventoryLot> availableLots, decimal requiredQty)
    {
        var selected = new List<InventoryLot>();
        var remaining = requiredQty;

        var orderedLots = availableLots
            .Where(l => l.Status == Enums.LotStatus.Open && l.RemainingQty > 0)
            .OrderBy(l => l.ExpiryDate ?? DateTime.MaxValue)
            .ThenBy(l => l.CreatedDate);

        foreach (var lot in orderedLots)
        {
            if (remaining <= 0) break;
            selected.Add(lot);
            remaining -= lot.RemainingQty;
        }

        return selected.AsReadOnly();
    }

    /// <summary>
    /// Toplam envanter değeri hesaplar.
    /// </summary>
    public decimal CalculateInventoryValue(IEnumerable<Product> products)
    {
        return products.Sum(p => p.Stock * p.PurchasePrice);
    }
}
