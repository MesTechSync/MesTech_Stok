using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.LoseDeal;

public class LoseDealValidator : AbstractValidator<LoseDealCommand>
{
    public LoseDealValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
