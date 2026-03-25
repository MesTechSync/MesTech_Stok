using FluentValidation;

namespace MesTech.Application.Commands.ApproveReturn;

public sealed class ApproveReturnValidator : AbstractValidator<ApproveReturnCommand>
{
    public ApproveReturnValidator()
    {
        RuleFor(x => x.ReturnRequestId).NotEmpty();
    }
}
