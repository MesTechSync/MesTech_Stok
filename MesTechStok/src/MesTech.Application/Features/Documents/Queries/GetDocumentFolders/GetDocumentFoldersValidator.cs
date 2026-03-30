using FluentValidation;

namespace MesTech.Application.Features.Documents.Queries.GetDocumentFolders;

public sealed class GetDocumentFoldersValidator : AbstractValidator<GetDocumentFoldersQuery>
{
    public GetDocumentFoldersValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
