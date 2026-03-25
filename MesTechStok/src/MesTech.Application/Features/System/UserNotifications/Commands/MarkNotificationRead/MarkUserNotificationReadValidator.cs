using FluentValidation;

namespace MesTech.Application.Features.System.UserNotifications.Commands.MarkNotificationRead;

public sealed class MarkUserNotificationReadValidator : AbstractValidator<MarkUserNotificationReadCommand>
{
    public MarkUserNotificationReadValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
