using FluentValidation;

namespace MesTech.Application.Features.Logging.Commands.CleanOldLogs;

public sealed class CleanOldLogsValidator : AbstractValidator<CleanOldLogsCommand>
{
    public CleanOldLogsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.DaysToKeep).GreaterThan(0).LessThanOrEqualTo(365);
    }
}
