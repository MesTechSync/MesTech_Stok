using FluentValidation;

namespace MesTech.Application.Features.Documents.Queries.GetDocuments;

public sealed class GetDocumentsValidator : AbstractValidator<GetDocumentsQuery>
{
    public GetDocumentsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
