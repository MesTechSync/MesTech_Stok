using FluentValidation;

namespace MesTech.Application.Features.Logging.Commands.CreateLogEntry;

public sealed class CreateLogEntryValidator : AbstractValidator<CreateLogEntryCommand>
{
    private static readonly string[] ValidLevels = { "Info", "Warning", "Error", "Debug" };

    public CreateLogEntryValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Level).NotEmpty().MaximumLength(20)
            .Must(l => ValidLevels.Contains(l))
            .WithMessage("Level must be one of: Info, Warning, Error, Debug");
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
    }
}
