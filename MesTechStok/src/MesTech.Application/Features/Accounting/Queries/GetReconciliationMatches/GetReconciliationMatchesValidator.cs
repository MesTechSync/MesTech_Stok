using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetReconciliationMatches;

public sealed class GetReconciliationMatchesValidator : AbstractValidator<GetReconciliationMatchesQuery>
{
    public GetReconciliationMatchesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
