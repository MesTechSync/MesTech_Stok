using MediatR;
using MesTech.Domain.Entities.AI;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTech.Application.Commands.UpdateStockForecast;

public record UpdateStockForecastCommand : IRequest
{
    public Guid ProductId { get; init; }
    public string SKU { get; init; } = string.Empty;
    public int PredictedDemand7d { get; init; }
    public int PredictedDemand14d { get; init; }
    public int PredictedDemand30d { get; init; }
    public int DaysUntilStockout { get; init; }
    public int ReorderSuggestion { get; init; }
    public double Confidence { get; init; }
    public string? Reasoning { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class UpdateStockForecastHandler : IRequestHandler<UpdateStockForecastCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly IStockPredictionRepository _stockPredictionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateStockForecastHandler> _logger;

    public UpdateStockForecastHandler(
        IProductRepository productRepository,
        IStockPredictionRepository stockPredictionRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateStockForecastHandler> logger)
    {
        _productRepository = productRepository;
        _stockPredictionRepository = stockPredictionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(UpdateStockForecastCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId).ConfigureAwait(false)
                      ?? await _productRepository.GetBySKUAsync(request.SKU).ConfigureAwait(false);
        if (product is null)
        {
            _logger.LogWarning("UpdateStockForecast: Product not found — ProductId={ProductId}, SKU={SKU}", request.ProductId, request.SKU);
            return;
        }

        product.UpdateAiStockSnapshot(request.PredictedDemand7d, request.DaysUntilStockout);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Save StockPrediction history
        var prediction = new StockPrediction
        {
            TenantId = request.TenantId,
            ProductId = product.Id,
            PredictedDemand7d = request.PredictedDemand7d,
            PredictedDemand14d = request.PredictedDemand14d,
            PredictedDemand30d = request.PredictedDemand30d,
            DaysUntilStockout = request.DaysUntilStockout,
            ReorderSuggestion = request.ReorderSuggestion,
            Confidence = request.Confidence,
            Reasoning = request.Reasoning ?? string.Empty
        };
        await _stockPredictionRepository.AddAsync(prediction).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("UpdateStockForecast: StockPrediction saved — SKU={SKU}, Id={Id}", request.SKU, prediction.Id);
    }
}
