using MediatR;

namespace MesTech.Application.Commands.UpdateDocumentMetadata;

public record UpdateDocumentMetadataCommand : IRequest
{
    public Guid DocumentId { get; init; }
    public string ProcessedJson { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
    public decimal? ExtractedAmount { get; init; }
    public string? ExtractedVKN { get; init; }
    public string? ExtractedCategory { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class UpdateDocumentMetadataHandler : IRequestHandler<UpdateDocumentMetadataCommand>
{
    public Task Handle(UpdateDocumentMetadataCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
