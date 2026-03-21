using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.RecordTaxWithholding;

public class RecordTaxWithholdingValidator : AbstractValidator<RecordTaxWithholdingCommand>
{
    public RecordTaxWithholdingValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.TaxExclusiveAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Rate).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TaxType).NotEmpty().MaximumLength(500);
    }
}
