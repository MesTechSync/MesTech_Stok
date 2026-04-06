using MediatR;
using MesTech.Domain.Entities.AI;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.ApplyOptimizedPrice;

public record ApplyOptimizedPriceCommand : IRequest
{
    public Guid ProductId { get; init; }
    public string SKU { get; init; } = string.Empty;
    public decimal RecommendedPrice { get; init; }
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public decimal? CompetitorMinPrice { get; init; }
    public double Confidence { get; init; }
    public string? Reasoning { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class ApplyOptimizedPriceHandler : IRequestHandler<ApplyOptimizedPriceCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IPriceRecommendationRepository _priceRecommendationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApplyOptimizedPriceHandler> _logger;

    public ApplyOptimizedPriceHandler(
        IProductRepository productRepository,
        IPriceRecommendationRepository priceRecommendationRepository,
        IUnitOfWork unitOfWork,
        ILogger<ApplyOptimizedPriceHandler> logger)
    {
        _productRepository = productRepository;
        _priceRecommendationRepository = priceRecommendationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ApplyOptimizedPriceCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false)
                      ?? await _productRepository.GetBySKUAsync(request.SKU, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            _logger.LogWarning("ApplyOptimizedPrice: Product not found — ProductId={ProductId}, SKU={SKU}", request.ProductId, request.SKU);
            return;
        }

        // Guard: only apply if confidence > 60% and price within bounds
        if (request.Confidence < 0.6)
        {
            _logger.LogInformation("ApplyOptimizedPrice: Skipped {SKU} — confidence {Confidence:P0} too low", request.SKU, request.Confidence);
            return;
        }

        var clampedPrice = Math.Clamp(request.RecommendedPrice, request.MinPrice, request.MaxPrice);
        var previousPrice = product.SalePrice;
        product.UpdatePrice(clampedPrice);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("ApplyOptimizedPrice: {SKU} price -> {Price:C} (confidence={Confidence:P0})", request.SKU, clampedPrice, request.Confidence);

        // Save PriceRecommendation history
        var recommendation = new PriceRecommendation
        {
            TenantId = request.TenantId,
            ProductId = product.Id,
            RecommendedPrice = request.RecommendedPrice,
            CurrentPrice = previousPrice,
            CompetitorMinPrice = request.CompetitorMinPrice,
            Confidence = request.Confidence,
            Reasoning = request.Reasoning ?? string.Empty,
            Source = "ai.price.optimized"
        };
        await _priceRecommendationRepository.AddAsync(recommendation, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("ApplyOptimizedPrice: PriceRecommendation saved — SKU={SKU}, Id={Id}", request.SKU, recommendation.Id);

        // Price deviation alert
        if (product.SalePrice > 0)
        {
            var deviationPct = Math.Abs((double)(request.RecommendedPrice - product.SalePrice) / (double)product.SalePrice);
            if (deviationPct > 0.20)
            {
                _logger.LogWarning(
                    "ApplyOptimizedPrice: PRICE ALERT — SKU={SKU} deviation {Pct:P1}, current={Current:N2}, recommended={Recommended:N2}",
                    request.SKU, deviationPct, product.SalePrice, request.RecommendedPrice);
            }
        }
    }
}
