using FluentValidation;

namespace MesTech.Application.Features.Tenant.Queries.GetTenants;

public sealed class GetTenantsValidator : AbstractValidator<GetTenantsQuery>
{
    public GetTenantsValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
