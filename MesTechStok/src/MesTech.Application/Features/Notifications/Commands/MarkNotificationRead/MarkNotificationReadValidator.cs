using FluentValidation;

namespace MesTech.Application.Features.Notifications.Commands.MarkNotificationRead;

public sealed class MarkNotificationReadValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.NotificationId).NotEmpty();
    }
}
