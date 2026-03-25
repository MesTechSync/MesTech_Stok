using FluentValidation;

namespace MesTech.Application.Commands.CreateExpense;

public sealed class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ExpenseType).IsInEnum();
        RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note != null);
        RuleFor(x => x.RecurrencePeriod).MaximumLength(500).When(x => x.RecurrencePeriod != null);
    }
}
