using FluentValidation;

namespace MesTech.Application.Features.Crm.Commands.EarnPoints;

public sealed class EarnPointsValidator : AbstractValidator<EarnPointsCommand>
{
    public EarnPointsValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.OrderAmount).GreaterThan(0);
    }
}
