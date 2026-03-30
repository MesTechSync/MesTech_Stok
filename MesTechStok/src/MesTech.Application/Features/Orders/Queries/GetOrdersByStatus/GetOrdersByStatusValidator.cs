using FluentValidation;

namespace MesTech.Application.Features.Orders.Queries.GetOrdersByStatus;

public sealed class GetOrdersByStatusValidator : AbstractValidator<GetOrdersByStatusQuery>
{
    public GetOrdersByStatusValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
