using FluentValidation;

namespace MesTech.Application.Features.Orders.Queries.GetOrderList;

public sealed class GetOrderListValidator : AbstractValidator<GetOrderListQuery>
{
    public GetOrderListValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Count).InclusiveBetween(1, 100);
    }
}
