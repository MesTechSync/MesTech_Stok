using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Features.Product.Commands.AutoCompetePrice;

/// <summary>
/// Toplu otomatik fiyat rekabet handler'ı.
/// Tenant'ın aktif ürünlerini alır, her biri için AutoCompetePriceCommand gönderir.
/// FloorPrice = CostPrice * (1 + FloorMarginPercent/100) ile maliyet koruması sağlar.
/// </summary>
public sealed class BulkAutoCompetePriceHandler
    : IRequestHandler<BulkAutoCompetePriceCommand, BulkAutoCompetePriceResult>
{
    private readonly IProductRepository _productRepo;
    private readonly ISender _mediator;
    private readonly ILogger<BulkAutoCompetePriceHandler> _logger;

    public BulkAutoCompetePriceHandler(
        IProductRepository productRepo,
        ISender mediator,
        ILogger<BulkAutoCompetePriceHandler> logger)
    {
        _productRepo = productRepo;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<BulkAutoCompetePriceResult> Handle(
        BulkAutoCompetePriceCommand request, CancellationToken cancellationToken)
    {
        var allProducts = await _productRepo.GetAllAsync()
            .ConfigureAwait(false);
        var products = allProducts;

        var activeProducts = products.Where(p => p.IsActive && p.SalePrice > 0).ToList();

        _logger.LogInformation(
            "[BulkAutoCompete] Tenant={TenantId} Platform={Platform} Products={Count}",
            request.TenantId, request.PlatformCode ?? "ALL", activeProducts.Count);

        var details = new List<AutoCompetePriceResult>();
        var changed = 0;
        var skipped = 0;
        var failed = 0;

        // Platform listesi — belirtilmişse sadece o platform, yoksa tümü
        var platforms = request.PlatformCode is not null
            ? new[] { request.PlatformCode }
            : new[] { "trendyol", "hepsiburada", "n11", "ciceksepati", "amazon" };

        foreach (var product in activeProducts)
        {
            foreach (var platform in platforms)
            {
                try
                {
                    // FloorPrice = maliyet × (1 + marj%)
                    var costPrice = product.PurchasePrice > 0 ? product.PurchasePrice : product.SalePrice * 0.6m;
                    var floorPrice = Math.Round(costPrice * (1 + request.FloorMarginPercent / 100m), 2);

                    var result = await _mediator.Send(new AutoCompetePriceCommand(
                        request.TenantId,
                        product.Id,
                        platform,
                        floorPrice,
                        request.MaxDiscountPercent), cancellationToken).ConfigureAwait(false);

                    details.Add(result);

                    if (result.PriceChanged)
                        changed++;
                    else if (result.IsSuccess)
                        skipped++;
                    else
                        failed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "[BulkAutoCompete] Error for Product={ProductId} Platform={Platform}",
                        product.Id, platform);
                    failed++;
                    details.Add(AutoCompetePriceResult.Failure($"Exception: {ex.Message}"));
                }
            }
        }

        _logger.LogInformation(
            "[BulkAutoCompete] Done. Changed={Changed} Skipped={Skipped} Failed={Failed}",
            changed, skipped, failed);

        return new BulkAutoCompetePriceResult
        {
            TotalProcessed = details.Count,
            PriceChanged = changed,
            Skipped = skipped,
            Failed = failed,
            Details = details
        };
    }
}
