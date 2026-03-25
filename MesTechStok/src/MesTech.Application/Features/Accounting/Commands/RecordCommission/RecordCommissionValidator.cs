using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.RecordCommission;

public sealed class RecordCommissionValidator : AbstractValidator<RecordCommissionCommand>
{
    public RecordCommissionValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Platform).NotEmpty().MaximumLength(500);
        RuleFor(x => x.GrossAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CommissionRate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CommissionAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ServiceFee).GreaterThanOrEqualTo(0);
        RuleFor(x => x.OrderId).MaximumLength(500).When(x => x.OrderId != null);
        RuleFor(x => x.Category).MaximumLength(500).When(x => x.Category != null);
    }
}
