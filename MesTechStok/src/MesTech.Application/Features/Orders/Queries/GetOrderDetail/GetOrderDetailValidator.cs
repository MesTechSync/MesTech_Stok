using FluentValidation;

namespace MesTech.Application.Features.Orders.Queries.GetOrderDetail;

public sealed class GetOrderDetailValidator : AbstractValidator<GetOrderDetailQuery>
{
    public GetOrderDetailValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.OrderId).NotEqual(Guid.Empty).WithMessage("Geçerli sipariş ID gerekli.");
    }
}
