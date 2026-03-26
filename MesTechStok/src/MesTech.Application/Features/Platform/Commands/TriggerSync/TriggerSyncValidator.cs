using FluentValidation;

namespace MesTech.Application.Features.Platform.Commands.TriggerSync;

public sealed class TriggerSyncValidator : AbstractValidator<TriggerSyncCommand>
{
    public TriggerSyncValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.PlatformCode).NotEmpty().MaximumLength(50);
    }
}
