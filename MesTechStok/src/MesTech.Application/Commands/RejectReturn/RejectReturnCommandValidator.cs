using FluentValidation;

namespace MesTech.Application.Commands.RejectReturn;

public class RejectReturnCommandValidator : AbstractValidator<RejectReturnCommand>
{
    public RejectReturnCommandValidator()
    {
        RuleFor(x => x.ReturnRequestId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli iade talebi ID gerekli.");

        RuleFor(x => x.RejectionReason)
            .MaximumLength(500).WithMessage("Red nedeni en fazla 500 karakter.")
            .When(x => x.RejectionReason is not null);
    }
}
