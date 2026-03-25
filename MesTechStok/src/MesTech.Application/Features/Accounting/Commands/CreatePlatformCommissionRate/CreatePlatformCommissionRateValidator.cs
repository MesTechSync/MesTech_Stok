using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreatePlatformCommissionRate;

public sealed class CreatePlatformCommissionRateValidator : AbstractValidator<CreatePlatformCommissionRateCommand>
{
    public CreatePlatformCommissionRateValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.Rate)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Commission rate must be non-negative.");
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Currency)
            .NotEmpty()
            .MaximumLength(3)
            .WithMessage("Currency code must be at most 3 characters.");
        RuleFor(x => x.CategoryName)
            .MaximumLength(200)
            .When(x => x.CategoryName != null);
        RuleFor(x => x.Notes)
            .MaximumLength(500)
            .When(x => x.Notes != null);
        RuleFor(x => x.MinAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinAmount.HasValue);
        RuleFor(x => x.MaxAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxAmount.HasValue);
        RuleFor(x => x)
            .Must(x => !x.EffectiveTo.HasValue || !x.EffectiveFrom.HasValue || x.EffectiveTo.Value >= x.EffectiveFrom.Value)
            .WithMessage("EffectiveTo must be after EffectiveFrom.");
    }
}
