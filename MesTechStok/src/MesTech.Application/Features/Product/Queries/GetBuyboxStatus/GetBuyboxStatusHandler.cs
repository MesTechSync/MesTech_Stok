using MediatR;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Product.Queries.GetBuyboxStatus;

public sealed class GetBuyboxStatusHandler
    : IRequestHandler<GetBuyboxStatusQuery, BuyboxStatusResult>
{
    private readonly IProductRepository _productRepo;
    private readonly IBuyboxService _buyboxService;

    public GetBuyboxStatusHandler(IProductRepository productRepo, IBuyboxService buyboxService)
    {
        _productRepo = productRepo;
        _buyboxService = buyboxService;
    }

    public async Task<BuyboxStatusResult> Handle(
        GetBuyboxStatusQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepo.GetByIdAsync(request.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (product is null)
            return new BuyboxStatusResult { ProductId = request.ProductId, Recommendation = "Urun bulunamadi" };

        var positions = await _buyboxService.CheckBuyboxPositionsAsync(
                product.TenantId, request.PlatformCode ?? "trendyol", cancellationToken)
            .ConfigureAwait(false);

        var buybox = positions.FirstOrDefault(p => string.Equals(p.SKU, product.SKU, StringComparison.Ordinal));
        if (buybox is null)
            return new BuyboxStatusResult { ProductId = product.Id, Recommendation = "Buybox verisi bulunamadi" };

        var ourPrice = product.SalePrice;
        var diff = buybox.PriceDiff;

        var recommendation = diff switch
        {
            > 0 => $"Fiyatiniz buybox'tan {diff:F2} TL yuksek — indirim onerilir",
            < 0 => $"Fiyatiniz buybox'tan {Math.Abs(diff):F2} TL dusuk — buybox avantaji",
            _ => "Buybox fiyati ile esitsiniz"
        };

        return new BuyboxStatusResult
        {
            ProductId = product.Id,
            IsWinner = buybox.HasBuybox,
            OurPrice = ourPrice,
            BuyboxPrice = buybox.BuyboxPrice,
            PriceDifference = diff,
            Recommendation = recommendation
        };
    }
}
