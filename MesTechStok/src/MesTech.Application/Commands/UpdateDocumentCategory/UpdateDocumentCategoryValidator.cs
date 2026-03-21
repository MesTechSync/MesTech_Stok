using FluentValidation;

namespace MesTech.Application.Commands.UpdateDocumentCategory;

public class UpdateDocumentCategoryValidator : AbstractValidator<UpdateDocumentCategoryCommand>
{
    public UpdateDocumentCategoryValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
        RuleFor(x => x.DocumentType).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
