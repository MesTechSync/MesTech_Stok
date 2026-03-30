using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetKdvDeclarationDraft;

public sealed class GetKdvDeclarationDraftValidator : AbstractValidator<GetKdvDeclarationDraftQuery>
{
    public GetKdvDeclarationDraftValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Period).NotEmpty()
            .Matches(@"^\d{4}-\d{2}$").WithMessage("Period must be in yyyy-MM format.");
    }
}
