using FluentValidation;

namespace MesTech.Application.Features.Finance.Queries.GetProfitLoss;

public sealed class GetProfitLossValidator : AbstractValidator<GetProfitLossQuery>
{
    public GetProfitLossValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(2020, 2099);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}
