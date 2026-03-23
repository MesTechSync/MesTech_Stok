using FluentValidation;

namespace MesTech.Application.Commands.RejectReturn;

public class RejectReturnValidator : AbstractValidator<RejectReturnCommand>
{
    public RejectReturnValidator()
    {
        RuleFor(x => x.ReturnRequestId).NotEmpty();
        RuleFor(x => x.RejectionReason).MaximumLength(1000);
    }
}
