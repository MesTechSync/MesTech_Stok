using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.WinDeal;

public class WinDealValidator : AbstractValidator<WinDealCommand>
{
    public WinDealValidator()
    {
        RuleFor(x => x.DealId).NotEmpty();
    }
}
