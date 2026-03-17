using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;

public class CreateAccountingExpenseValidator : AbstractValidator<CreateAccountingExpenseCommand>
{
    public CreateAccountingExpenseValidator()
    {
        RuleFor(x => x.TenantId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(300)
            .WithMessage("Title is required and must be at most 300 characters.");
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be positive.");
        RuleFor(x => x.Source).IsInEnum();
        RuleFor(x => x.Category)
            .MaximumLength(100)
            .When(x => x.Category != null);
    }
}
