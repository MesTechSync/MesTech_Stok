using FluentValidation;

namespace MesTech.Application.Features.Notifications.Queries.GetNotificationSettings;

public sealed class GetNotificationSettingsValidator : AbstractValidator<GetNotificationSettingsQuery>
{
    public GetNotificationSettingsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEqual(Guid.Empty)
            .WithMessage("Kullanici kimlik bilgisi bos olamaz.");
    }
}
