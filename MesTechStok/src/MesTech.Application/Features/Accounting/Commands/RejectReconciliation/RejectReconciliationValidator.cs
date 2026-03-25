using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.RejectReconciliation;

public sealed class RejectReconciliationValidator : AbstractValidator<RejectReconciliationCommand>
{
    public RejectReconciliationValidator()
    {
        RuleFor(x => x.MatchId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason != null);
    }
}
