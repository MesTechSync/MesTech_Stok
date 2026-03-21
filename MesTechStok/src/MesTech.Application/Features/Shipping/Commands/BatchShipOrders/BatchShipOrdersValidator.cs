using FluentValidation;

namespace MesTech.Application.Features.Shipping.Commands.BatchShipOrders;

public class BatchShipOrdersValidator : AbstractValidator<BatchShipOrdersCommand>
{
    public BatchShipOrdersValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
