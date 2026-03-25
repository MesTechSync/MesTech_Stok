using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.UpdatePlatformCommissionRate;

public sealed class UpdatePlatformCommissionRateValidator : AbstractValidator<UpdatePlatformCommissionRateCommand>
{
    public UpdatePlatformCommissionRateValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Rate)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Rate.HasValue)
            .WithMessage("Commission rate must be non-negative.");
        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue);
        RuleFor(x => x.Currency)
            .MaximumLength(3)
            .When(x => x.Currency != null);
        RuleFor(x => x.MinAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinAmount.HasValue);
        RuleFor(x => x.MaxAmount)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxAmount.HasValue);
    }
}
