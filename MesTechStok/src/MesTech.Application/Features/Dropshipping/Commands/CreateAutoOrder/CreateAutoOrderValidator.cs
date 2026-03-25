using FluentValidation;

namespace MesTech.Application.Features.Dropshipping.Commands.CreateAutoOrder;

public sealed class CreateAutoOrderValidator : AbstractValidator<CreateAutoOrderCommand>
{
    public CreateAutoOrderValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
    }
}
