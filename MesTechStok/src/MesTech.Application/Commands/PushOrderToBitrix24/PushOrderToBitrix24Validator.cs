using FluentValidation;

namespace MesTech.Application.Commands.PushOrderToBitrix24;

public sealed class PushOrderToBitrix24Validator : AbstractValidator<PushOrderToBitrix24Command>
{
    public PushOrderToBitrix24Validator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
    }
}
