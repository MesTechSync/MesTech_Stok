using FluentValidation;

namespace MesTech.Application.Features.System.UserNotifications.Commands.MarkAllNotificationsRead;

public sealed class MarkAllUserNotificationsReadValidator : AbstractValidator<MarkAllUserNotificationsReadCommand>
{
    public MarkAllUserNotificationsReadValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
