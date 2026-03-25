using FluentValidation;

namespace MesTech.Application.Commands.MarkNotificationDelivered;

public sealed class MarkNotificationDeliveredValidator : AbstractValidator<MarkNotificationDeliveredCommand>
{
    public MarkNotificationDeliveredValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Channel).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Recipient).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TemplateName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(500);
    }
}
