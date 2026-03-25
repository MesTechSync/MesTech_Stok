using FluentValidation;

namespace MesTech.Application.Features.Hr.Commands.ApproveLeave;

public sealed class ApproveLeaveValidator : AbstractValidator<ApproveLeaveCommand>
{
    public ApproveLeaveValidator()
    {
        RuleFor(x => x.LeaveId).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
