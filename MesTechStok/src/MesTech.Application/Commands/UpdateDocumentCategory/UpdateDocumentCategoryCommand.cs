using MediatR;

namespace MesTech.Application.Commands.UpdateDocumentCategory;

public record UpdateDocumentCategoryCommand : IRequest
{
    public Guid DocumentId { get; init; }
    public string DocumentType { get; init; } = string.Empty;
    public decimal Confidence { get; init; }
    public decimal? ExtractedAmount { get; init; }
    public string? ExtractedVKN { get; init; }
    public Guid TenantId { get; init; }
}

public sealed class UpdateDocumentCategoryHandler : IRequestHandler<UpdateDocumentCategoryCommand>
{
    public Task Handle(UpdateDocumentCategoryCommand request, CancellationToken cancellationToken)
    {
        // Minimal handler — domain logic lives in consumer, to be migrated in future sprints
        return Task.CompletedTask;
    }
}
