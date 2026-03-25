using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreateFinancialGoal;

public sealed class CreateFinancialGoalValidator : AbstractValidator<CreateFinancialGoalCommand>
{
    public CreateFinancialGoalValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.TargetAmount).GreaterThanOrEqualTo(0);
    }
}
