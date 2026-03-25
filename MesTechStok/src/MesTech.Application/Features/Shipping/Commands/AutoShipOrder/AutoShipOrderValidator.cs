using FluentValidation;

namespace MesTech.Application.Features.Shipping.Commands.AutoShipOrder;

public sealed class AutoShipOrderValidator : AbstractValidator<AutoShipOrderCommand>
{
    public AutoShipOrderValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
