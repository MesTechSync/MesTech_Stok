using MediatR;

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

public class ApplyOptimizedPriceHandler : IRequestHandler<ApplyOptimizedPriceCommand>
{
    public Task Handle(ApplyOptimizedPriceCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
