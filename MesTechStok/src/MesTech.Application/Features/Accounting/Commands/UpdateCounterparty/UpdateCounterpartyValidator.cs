using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.UpdateCounterparty;

public sealed class UpdateCounterpartyValidator : AbstractValidator<UpdateCounterpartyCommand>
{
    public UpdateCounterpartyValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(500);
        RuleFor(x => x.VKN).MaximumLength(500).When(x => x.VKN != null);
        RuleFor(x => x.Phone).MaximumLength(500).When(x => x.Phone != null);
        RuleFor(x => x.Email).MaximumLength(500).When(x => x.Email != null);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address != null);
        RuleFor(x => x.Platform).MaximumLength(500).When(x => x.Platform != null);
    }
}
