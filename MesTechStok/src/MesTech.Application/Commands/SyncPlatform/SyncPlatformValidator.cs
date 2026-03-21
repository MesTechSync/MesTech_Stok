using FluentValidation;

namespace MesTech.Application.Commands.SyncPlatform;

public class SyncPlatformValidator : AbstractValidator<SyncPlatformCommand>
{
    public SyncPlatformValidator()
    {
        RuleFor(x => x.PlatformCode).NotEmpty().MaximumLength(500);
    }
}
