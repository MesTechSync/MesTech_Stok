using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;

namespace MesTech.Infrastructure.Integration.FeedParsers;

/// <summary>
/// Feed parse sonucu ile mevcut DB kaydını karşılaştırır.
/// Sadece değişen ürünler döner — full resync yerine delta sync.
/// </summary>
public static class FeedDeltaDetector
{
    /// <summary>
    /// Yeni parse sonucu ile mevcut DB kaydını karşılaştırır.
    /// Değişiklik yoksa false döner → DB'ye yazma atlanır.
    /// </summary>
    /// <param name="incoming">Feed'den yeni parse edilen ürün.</param>
    /// <param name="existing">Mevcut DB Product kaydı.</param>
    /// <param name="markedUpPrice">Markup uygulanmış satış fiyatı (ApplyMarkup sonucu).</param>
    public static bool HasChanged(
        ParsedProduct incoming,
        Product existing,
        decimal? markedUpPrice = null)
    {
        // Stok değişti mi?
        if (incoming.Quantity.HasValue && incoming.Quantity.Value != existing.Stock)
            return true;

        // Fiyat değişti mi? (0.01 TL tolerans)
        var incomingPrice = markedUpPrice ?? incoming.Price;
        if (incomingPrice.HasValue
            && Math.Abs(incomingPrice.Value - existing.SalePrice) > 0.01m)
            return true;

        // Aktiflik değişti mi? (stok 0 → deaktif, stok > 0 → aktif)
        if (incoming.Quantity.HasValue)
        {
            var incomingActive = incoming.Quantity.Value > 0;
            if (incomingActive != existing.IsActive)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Toplu karşılaştırma — sadece değişenleri döner.
    /// </summary>
    /// <param name="incoming">Feed'den parse edilmiş tüm ürünler.</param>
    /// <param name="existingBySku">SKU → mevcut Product sözlüğü.</param>
    /// <param name="markupFunc">Opsiyonel markup hesaplama fonksiyonu (feed.ApplyMarkup).</param>
    public static IEnumerable<(ParsedProduct Incoming, Product? Existing)>
        GetChangedProducts(
            IEnumerable<ParsedProduct> incoming,
            IDictionary<string, Product> existingBySku,
            Func<decimal, decimal>? markupFunc = null)
    {
        foreach (var product in incoming)
        {
            var sku = product.SKU ?? string.Empty;

            if (!existingBySku.TryGetValue(sku, out var existing))
            {
                // Yeni ürün — her zaman ekle
                yield return (product, null);
                continue;
            }

            var markedUpPrice = markupFunc != null && product.Price.HasValue
                ? markupFunc(product.Price.Value)
                : product.Price;

            if (HasChanged(product, existing, markedUpPrice))
                yield return (product, existing);
            // Değişiklik yok → atla
        }
    }
}
