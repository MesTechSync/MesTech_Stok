using FluentValidation;

namespace MesTech.Application.Features.Orders.Queries.GetStaleOrders;

public sealed class GetStaleOrdersQueryValidator : AbstractValidator<GetStaleOrdersQuery>
{
    public GetStaleOrdersQueryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
