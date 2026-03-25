using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;

public sealed class ApproveReconciliationValidator : AbstractValidator<ApproveReconciliationCommand>
{
    public ApproveReconciliationValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty();
    }
}
