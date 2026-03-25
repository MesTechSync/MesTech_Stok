using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.RedeemPoints;

public sealed class RedeemPointsValidator : AbstractValidator<RedeemPointsCommand>
{
    public RedeemPointsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.PointsToRedeem).GreaterThan(0);
    }
}
