using MediatR;
using MesTech.Domain.Interfaces;

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
    private readonly IUnitOfWork _unitOfWork;

    public UpdateStockForecastHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdateStockForecastCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId).ConfigureAwait(false);
        if (product is null) return;

        product.UpdateAiStockSnapshot(request.PredictedDemand7d, request.DaysUntilStockout);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
