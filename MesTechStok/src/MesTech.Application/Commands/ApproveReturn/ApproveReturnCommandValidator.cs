using FluentValidation;

namespace MesTech.Application.Commands.ApproveReturn;

public sealed class ApproveReturnCommandValidator : AbstractValidator<ApproveReturnCommand>
{
    public ApproveReturnCommandValidator()
    {
        RuleFor(x => x.ReturnRequestId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli iade talebi ID gerekli.");
    }
}
