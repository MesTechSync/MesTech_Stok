using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetDeals;

public sealed class GetDealsValidator : AbstractValidator<GetDealsQuery>
{
    public GetDealsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
