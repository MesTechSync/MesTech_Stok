using FluentValidation;

namespace MesTech.Application.Features.Settings.Commands.SaveApiSettings;

public sealed class SaveApiSettingsValidator : AbstractValidator<SaveApiSettingsCommand>
{
    public SaveApiSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.ApiBaseUrl)
            .NotEmpty()
            .MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("ApiBaseUrl must be a valid absolute URL.");
        RuleFor(x => x.RateLimitPerMinute).InclusiveBetween(1, 10000);
        RuleFor(x => x.WebhookSecret)
            .MaximumLength(256)
            .When(x => x.WebhookSecret != null);
    }
}
