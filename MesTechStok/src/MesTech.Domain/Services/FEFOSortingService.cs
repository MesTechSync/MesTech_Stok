namespace MesTech.Domain.Services;

/// <summary>
/// FEFO (First Expired First Out) siralama domain servisi.
/// Depo stok cikislarinda son kullanma tarihine gore onceliklendirme yapar.
/// Saf is kurallari, altyapi bagimliligi yok.
/// </summary>
public sealed class FEFOSortingService : IFEFOSortingService
{
    /// <summary>
    /// Stok kalemlerini FEFO kuralina gore siralar.
    /// 1. Son kullanma tarihi olan kalemler: erken tarih once
    /// 2. Ayni tarihliler: lokasyona gore (alfabetik)
    /// 3. Son kullanma tarihi olmayan kalemler: en sona
    /// </summary>
    public IReadOnlyList<FEFOStockItem> Sort(IEnumerable<FEFOStockItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return items
            .Where(i => i.Quantity > 0)
            .OrderBy(i => i.ExpirationDate.HasValue ? 0 : 1)
            .ThenBy(i => i.ExpirationDate ?? DateTime.MaxValue)
            .ThenBy(i => i.Location, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Belirtilen miktar icin FEFO sirasina gore kalem secer.
    /// Her kalemden ne kadar alinacagi hesaplanir, toplam miktar karsilanana kadar devam eder.
    /// </summary>
    public IReadOnlyList<FEFOPickResult> PickForConsumption(
        IEnumerable<FEFOStockItem> items, decimal requiredQuantity)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (requiredQuantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(requiredQuantity), "Required quantity must be greater than zero.");

        var sorted = Sort(items);
        var results = new List<FEFOPickResult>();
        var remaining = requiredQuantity;

        foreach (var item in sorted)
        {
            if (remaining <= 0) break;

            var pickQty = Math.Min(item.Quantity, remaining);
            results.Add(new FEFOPickResult(item, pickQty));
            remaining -= pickQty;
        }

        return results.AsReadOnly();
    }
}
