using FluentValidation;

namespace MesTech.Application.Commands.FinalizeErpReconciliation;

public sealed class FinalizeErpReconciliationValidator : AbstractValidator<FinalizeErpReconciliationCommand>
{
    public FinalizeErpReconciliationValidator()
    {
        RuleFor(x => x.ErpProvider).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
