using FluentValidation;

namespace MesTech.Application.Features.Finance.Commands.MarkExpensePaid;

public class MarkExpensePaidValidator : AbstractValidator<MarkExpensePaidCommand>
{
    public MarkExpensePaidValidator()
    {
        RuleFor(x => x.ExpenseId).NotEmpty();
        RuleFor(x => x.BankAccountId).NotEmpty();
    }
}
