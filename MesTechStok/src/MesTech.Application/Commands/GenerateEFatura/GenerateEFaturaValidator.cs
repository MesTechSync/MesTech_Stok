using FluentValidation;

namespace MesTech.Application.Commands.GenerateEFatura;

public class GenerateEFaturaValidator : AbstractValidator<GenerateEFaturaCommand>
{
    public GenerateEFaturaValidator()
    {
        RuleFor(x => x.BotUserId).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TenantId).NotEmpty();
    }
}
