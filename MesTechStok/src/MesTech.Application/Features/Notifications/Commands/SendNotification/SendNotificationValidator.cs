using FluentValidation;

namespace MesTech.Application.Features.Notifications.Commands.SendNotification;

public sealed class SendNotificationValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Channel).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Recipient).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TemplateName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(500);
    }
}
