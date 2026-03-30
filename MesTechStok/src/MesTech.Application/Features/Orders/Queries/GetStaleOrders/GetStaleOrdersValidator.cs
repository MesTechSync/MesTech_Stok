using FluentValidation;

namespace MesTech.Application.Features.Orders.Queries.GetStaleOrders;

public sealed class GetStaleOrdersValidator : AbstractValidator<GetStaleOrdersQuery>
{
    public GetStaleOrdersValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
