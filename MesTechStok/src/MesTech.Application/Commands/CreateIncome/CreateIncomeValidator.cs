using FluentValidation;

namespace MesTech.Application.Commands.CreateIncome;

public sealed class CreateIncomeValidator : AbstractValidator<CreateIncomeCommand>
{
    public CreateIncomeValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.IncomeType).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note != null);
    }
}
