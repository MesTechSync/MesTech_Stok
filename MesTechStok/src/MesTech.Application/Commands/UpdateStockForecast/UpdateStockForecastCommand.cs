using MediatR;

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
    public Task Handle(UpdateStockForecastCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
