using MediatR;

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
    public Task Handle(UpdateProductPriceCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
