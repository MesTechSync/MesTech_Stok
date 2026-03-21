using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.RunReconciliation;

public class RunReconciliationValidator : AbstractValidator<RunReconciliationCommand>
{
    public RunReconciliationValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
