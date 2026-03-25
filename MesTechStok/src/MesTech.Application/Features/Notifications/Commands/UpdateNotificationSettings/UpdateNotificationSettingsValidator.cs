using FluentValidation;

namespace MesTech.Application.Features.Notifications.Commands.UpdateNotificationSettings;

public sealed class UpdateNotificationSettingsValidator : AbstractValidator<UpdateNotificationSettingsCommand>
{
    public UpdateNotificationSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ChannelAddress).MaximumLength(500).When(x => x.ChannelAddress != null);
        RuleFor(x => x.LowStockThreshold).GreaterThanOrEqualTo(0);
        RuleFor(x => x.QuietHoursStart).MaximumLength(500).When(x => x.QuietHoursStart != null);
        RuleFor(x => x.QuietHoursEnd).MaximumLength(500).When(x => x.QuietHoursEnd != null);
        RuleFor(x => x.PreferredLanguage).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DigestTime).MaximumLength(500).When(x => x.DigestTime != null);
    }
}
