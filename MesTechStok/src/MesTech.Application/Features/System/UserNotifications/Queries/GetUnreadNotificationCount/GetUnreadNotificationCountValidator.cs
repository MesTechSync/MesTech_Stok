using FluentValidation;

namespace MesTech.Application.Features.System.UserNotifications.Queries.GetUnreadNotificationCount;

public sealed class GetUnreadNotificationCountValidator : AbstractValidator<GetUnreadNotificationCountQuery>
{
    public GetUnreadNotificationCountValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEqual(Guid.Empty)
            .WithMessage("Kullanici kimlik bilgisi bos olamaz.");
    }
}
