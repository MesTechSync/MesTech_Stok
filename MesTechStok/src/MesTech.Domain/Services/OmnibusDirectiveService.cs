using MesTech.Domain.Entities;

namespace MesTech.Domain.Services;

/// <summary>
/// Omnibus Directive (AB 2019/2161, TR Ticaret Bakanlığı 2022) uyumluluğu.
/// İndirimli fiyat gösterilirken son 30 gün içindeki en düşük fiyat referans alınmalı.
/// "Eski Fiyat" olarak gösterilen tutar, son 30 gündeki en düşük satış fiyatı olmalı.
///
/// Kural: Bir ürünün indirimli fiyatı gösterilirken, indirimin uygulandığı tarihten
/// geriye doğru 30 gün içinde uygulanan en düşük fiyat "önceki fiyat" olarak belirtilmeli.
/// İhlal: Yanıltıcı indirim → idari para cezası.
/// </summary>
public sealed class OmnibusDirectiveService
{
    /// <summary>
    /// Son 30 gün içindeki en düşük satış fiyatını hesaplar.
    /// Bu fiyat "Eski Fiyat" olarak gösterilmeli — Omnibus Directive.
    /// </summary>
    public decimal CalculateLowest30DayPrice(
        IReadOnlyList<PriceHistory> priceHistory,
        decimal currentPrice)
    {
        ArgumentNullException.ThrowIfNull(priceHistory);

        var cutoff = DateTime.UtcNow.AddDays(-30);

        var relevantPrices = priceHistory
            .Where(ph => ph.ChangedAt >= cutoff)
            .SelectMany(ph => new[] { ph.OldPrice, ph.NewPrice })
            .Where(p => p > 0)
            .ToList();

        if (relevantPrices.Count == 0)
            return currentPrice;

        return relevantPrices.Min();
    }

    /// <summary>
    /// İndirim yüzdesini Omnibus kuralına uygun hesaplar.
    /// İndirim oranı = (30 gün en düşük - şimdiki fiyat) / 30 gün en düşük * 100
    /// </summary>
    public OmnibusDiscountResult CalculateOmnibusDiscount(
        IReadOnlyList<PriceHistory> priceHistory,
        decimal currentPrice)
    {
        var lowestPrice = CalculateLowest30DayPrice(priceHistory, currentPrice);

        if (lowestPrice <= 0 || currentPrice >= lowestPrice)
        {
            return new OmnibusDiscountResult(
                ReferencePrice: lowestPrice,
                DiscountedPrice: currentPrice,
                DiscountPercent: 0,
                IsDiscounted: false);
        }

        var discountPercent = Math.Round((lowestPrice - currentPrice) / lowestPrice * 100, 2);

        return new OmnibusDiscountResult(
            ReferencePrice: lowestPrice,
            DiscountedPrice: currentPrice,
            DiscountPercent: discountPercent,
            IsDiscounted: true);
    }
}

/// <summary>
/// Omnibus Directive uyumlu indirim hesaplama sonucu.
/// </summary>
public record OmnibusDiscountResult(
    decimal ReferencePrice,
    decimal DiscountedPrice,
    decimal DiscountPercent,
    bool IsDiscounted);
