using MediatR;

namespace MesTech.Application.Commands.UpdateProductContent;

public record UpdateProductContentCommand : IRequest
{
    public Guid ProductId { get; init; }
    public string SKU { get; init; } = string.Empty;
    public string GeneratedContent { get; init; } = string.Empty;
    public string AiProvider { get; init; } = string.Empty;
    public Guid TenantId { get; init; }
}

public sealed class UpdateProductContentHandler : IRequestHandler<UpdateProductContentCommand>
{
    public Task Handle(UpdateProductContentCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
