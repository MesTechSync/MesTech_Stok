using FluentValidation;

namespace MesTech.Application.Features.Dashboard.Queries.GetRecentOrders;

public sealed class GetRecentOrdersValidator : AbstractValidator<GetRecentOrdersQuery>
{
    public GetRecentOrdersValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
    }
}
