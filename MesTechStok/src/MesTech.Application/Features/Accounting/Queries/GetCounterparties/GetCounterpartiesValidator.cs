using FluentValidation;

namespace MesTech.Application.Features.Accounting.Queries.GetCounterparties;

public sealed class GetCounterpartiesValidator : AbstractValidator<GetCounterpartiesQuery>
{
    public GetCounterpartiesValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
