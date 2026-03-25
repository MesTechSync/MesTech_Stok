using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreateTaxRecord;

public sealed class CreateTaxRecordValidator : AbstractValidator<CreateTaxRecordCommand>
{
    public CreateTaxRecordValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Period)
            .NotEmpty()
            .MaximumLength(20)
            .WithMessage("Period is required (e.g. '2026-01').");
        RuleFor(x => x.TaxType)
            .NotEmpty()
            .MaximumLength(50)
            .WithMessage("Tax type is required (e.g. 'KDV', 'GelirVergisi').");
        RuleFor(x => x.TaxableAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Taxable amount must be non-negative.");
        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0, 100)
            .WithMessage("Tax rate must be between 0 and 100.");
        RuleFor(x => x.TaxAmount)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Tax amount must be non-negative.");
        RuleFor(x => x.Year)
            .InclusiveBetween(2000, 2100)
            .When(x => x.Year.HasValue)
            .WithMessage("Year must be between 2000 and 2100.");
    }
}
