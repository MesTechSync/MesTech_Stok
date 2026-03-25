using FluentValidation;

namespace MesTech.Application.Commands.UpdateDocumentMetadata;

public sealed class UpdateDocumentMetadataValidator : AbstractValidator<UpdateDocumentMetadataCommand>
{
    public UpdateDocumentMetadataValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.ProcessedJson).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
