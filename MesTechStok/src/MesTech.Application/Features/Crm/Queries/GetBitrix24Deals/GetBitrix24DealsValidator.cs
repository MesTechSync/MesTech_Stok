using FluentValidation;

namespace MesTech.Application.Features.Crm.Queries.GetBitrix24Deals;

public sealed class GetBitrix24DealsValidator : AbstractValidator<GetBitrix24DealsQuery>
{
    public GetBitrix24DealsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
