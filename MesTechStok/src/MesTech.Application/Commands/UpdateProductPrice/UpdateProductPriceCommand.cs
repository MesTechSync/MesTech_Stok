using MediatR;
using MesTech.Domain.Entities.AI;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.UpdateProductPrice;

public record UpdateProductPriceCommand : IRequest
{
    public Guid ProductId { get; init; }
    public string SKU { get; init; } = string.Empty;
    public decimal RecommendedPrice { get; init; }
    public decimal MinPrice { get; init; }
    public decimal MaxPrice { get; init; }
    public string? Reasoning { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class UpdateProductPriceHandler : IRequestHandler<UpdateProductPriceCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IPriceRecommendationRepository _priceRecommendationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateProductPriceHandler> _logger;

    public UpdateProductPriceHandler(
        IProductRepository productRepository,
        IPriceRecommendationRepository priceRecommendationRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateProductPriceHandler> logger)
    {
        _productRepository = productRepository;
        _priceRecommendationRepository = priceRecommendationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(UpdateProductPriceCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId).ConfigureAwait(false)
                      ?? await _productRepository.GetBySKUAsync(request.SKU).ConfigureAwait(false);
        if (product is null)
        {
            _logger.LogWarning("UpdateProductPrice: Product not found — ProductId={ProductId}, SKU={SKU}", request.ProductId, request.SKU);
            return;
        }

        product.SalePrice = request.RecommendedPrice;
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Save PriceRecommendation history
        var recommendation = new PriceRecommendation
        {
            TenantId = request.TenantId,
            ProductId = product.Id,
            RecommendedPrice = request.RecommendedPrice,
            CurrentPrice = product.SalePrice,
            Confidence = 0, // MesaAiPriceRecommendedEvent does not carry Confidence
            Reasoning = request.Reasoning ?? string.Empty,
            Source = "ai.price.recommended"
        };
        await _priceRecommendationRepository.AddAsync(recommendation).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("UpdateProductPrice: PriceRecommendation saved — SKU={SKU}, Id={Id}", request.SKU, recommendation.Id);
    }
}
