using FluentValidation;

namespace MesTech.Application.Features.SocialFeed.Commands.RefreshSocialFeed;

public sealed class RefreshSocialFeedValidator : AbstractValidator<RefreshSocialFeedCommand>
{
    public RefreshSocialFeedValidator()
    {
        RuleFor(x => x.ConfigId)
            .NotEqual(Guid.Empty).WithMessage("Geçerli yapılandırma ID gerekli.");
    }
}
