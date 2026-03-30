using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Product.Commands.AutoCompetePrice;

/// <summary>
/// Otomatik fiyat rekabet handler'ı.
/// 1. Buybox analizi yap (mevcut rakip fiyatlarını al)
/// 2. En düşük rakip fiyatından %1 altına in (FloorPrice'a kadar)
/// 3. Adapter üzerinden platforma push et
/// FMEA: RPN=42 (Severity=6, Occurrence=3, Detection=2.3)
///   Risk: Yanlış fiyat push → zarar. Koruma: FloorPrice + MaxDiscount sınırı.
/// </summary>
public sealed class AutoCompetePriceHandler
    : IRequestHandler<AutoCompetePriceCommand, AutoCompetePriceResult>
{
    private readonly IBuyboxService _buybox;
    private readonly IAdapterFactory _adapterFactory;
    private readonly IProductRepository _productRepo;
    private readonly ILogger<AutoCompetePriceHandler> _logger;

    public AutoCompetePriceHandler(
        IBuyboxService buybox,
        IAdapterFactory adapterFactory,
        IProductRepository productRepo,
        ILogger<AutoCompetePriceHandler> logger)
    {
        _buybox = buybox;
        _adapterFactory = adapterFactory;
        _productRepo = productRepo;
        _logger = logger;
    }

    public async Task<AutoCompetePriceResult> Handle(
        AutoCompetePriceCommand request, CancellationToken cancellationToken)
    {
        // 1. Ürünü bul
        var product = await _productRepo.GetByIdAsync(request.ProductId);
        if (product is null)
            return AutoCompetePriceResult.Failure("Ürün bulunamadı");

        var currentPrice = product.SalePrice;
        var sku = product.SKU;

        // 2. Buybox analizi
        var analysis = await _buybox.AnalyzeCompetitorsAsync(
            sku, currentPrice, request.PlatformCode, cancellationToken);

        if (analysis.HasBuybox)
            return AutoCompetePriceResult.NoChange(currentPrice, "Zaten Buybox'ta — fiyat değişikliğine gerek yok");

        if (analysis.Competitors.Count == 0)
            return AutoCompetePriceResult.NoChange(currentPrice, "Rakip bulunamadı — fiyat korunuyor");

        // 3. Hedef fiyat hesapla: en düşük rakibin %1 altı
        var targetPrice = Math.Round(analysis.LowestCompetitorPrice * 0.99m, 2);

        // 4. Güvenlik kontrolleri
        if (targetPrice < request.FloorPrice)
        {
            _logger.LogWarning(
                "AutoCompete: Hedef fiyat {Target} FloorPrice {Floor} altında — FloorPrice uygulanıyor. SKU={SKU}",
                targetPrice, request.FloorPrice, sku);
            targetPrice = request.FloorPrice;
        }

        var maxDiscount = currentPrice * (1 - request.MaxDiscountPercent / 100m);
        if (targetPrice < maxDiscount)
        {
            _logger.LogWarning(
                "AutoCompete: Hedef fiyat {Target} MaxDiscount sınırı {MaxDiscount} altında — sınır uygulanıyor. SKU={SKU}",
                targetPrice, maxDiscount, sku);
            targetPrice = Math.Round(maxDiscount, 2);
        }

        if (targetPrice >= currentPrice)
            return AutoCompetePriceResult.NoChange(currentPrice,
                $"Hesaplanan fiyat ({targetPrice:F2}) mevcut fiyattan ({currentPrice:F2}) yüksek veya eşit — değişiklik yok");

        // 5. Adapter üzerinden platforma push
        var adapter = _adapterFactory.Resolve(request.PlatformCode);
        if (adapter is null)
            return AutoCompetePriceResult.Failure($"{request.PlatformCode} adapter bulunamadı");

        var pushed = await adapter.PushPriceUpdateAsync(request.ProductId, targetPrice, cancellationToken);
        if (!pushed)
            return AutoCompetePriceResult.Failure("Fiyat platforma gönderilemedi — adapter hatası");

        _logger.LogInformation(
            "AutoCompete: Fiyat güncellendi. SKU={SKU} Platform={Platform} {Old}→{New} (Rakip: {Competitor} {CompPrice})",
            sku, request.PlatformCode, currentPrice, targetPrice,
            analysis.LowestCompetitorName, analysis.LowestCompetitorPrice);

        return AutoCompetePriceResult.Changed(
            currentPrice, targetPrice,
            analysis.LowestCompetitorPrice,
            analysis.LowestCompetitorName,
            $"Rakip {analysis.LowestCompetitorName} ({analysis.LowestCompetitorPrice:F2}) altına inildi → {targetPrice:F2}");
    }
}
