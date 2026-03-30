using FluentValidation;

namespace MesTech.Application.Features.System.UserNotifications.Queries.GetUserNotifications;

public sealed class GetUserNotificationsValidator : AbstractValidator<GetUserNotificationsQuery>
{
    public GetUserNotificationsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.UserId).NotEqual(Guid.Empty)
            .WithMessage("Kullanici kimlik bilgisi bos olamaz.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
