using FluentValidation;

namespace MesTech.Application.Features.System.Commands.TriggerBackup;

public sealed class TriggerBackupValidator : AbstractValidator<TriggerBackupCommand>
{
    public TriggerBackupValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}
