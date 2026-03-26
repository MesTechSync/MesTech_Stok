using MediatR;
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
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ApplyOptimizedPriceHandler> _logger;

    public ApplyOptimizedPriceHandler(IProductRepository productRepository, IUnitOfWork unitOfWork, ILogger<ApplyOptimizedPriceHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(ApplyOptimizedPriceCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId).ConfigureAwait(false);
        if (product is null)
        {
            _logger.LogWarning("ApplyOptimizedPrice: Product {ProductId} not found", request.ProductId);
            return;
        }

        // Guard: only apply if confidence > 60% and price within bounds
        if (request.Confidence < 0.6)
        {
            _logger.LogInformation("ApplyOptimizedPrice: Skipped {SKU} — confidence {Confidence:P0} too low", request.SKU, request.Confidence);
            return;
        }

        var clampedPrice = Math.Clamp(request.RecommendedPrice, request.MinPrice, request.MaxPrice);
        product.SalePrice = clampedPrice;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("ApplyOptimizedPrice: {SKU} price → {Price:C} (confidence={Confidence:P0})", request.SKU, clampedPrice, request.Confidence);
    }
}
