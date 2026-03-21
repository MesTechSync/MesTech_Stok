using FluentValidation;

namespace MesTech.Application.Commands.UpdateBotNotificationStatus;

public class UpdateBotNotificationStatusValidator : AbstractValidator<UpdateBotNotificationStatusCommand>
{
    public UpdateBotNotificationStatusValidator()
    {
        RuleFor(x => x.Channel).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Recipient).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
