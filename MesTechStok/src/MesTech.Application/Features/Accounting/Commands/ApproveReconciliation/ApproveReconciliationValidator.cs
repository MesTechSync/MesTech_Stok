using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.ApproveReconciliation;

public class ApproveReconciliationValidator : AbstractValidator<ApproveReconciliationCommand>
{
    public ApproveReconciliationValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty();
    }
}
