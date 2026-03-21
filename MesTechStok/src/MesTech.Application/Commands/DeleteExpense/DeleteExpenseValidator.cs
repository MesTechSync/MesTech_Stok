using FluentValidation;

namespace MesTech.Application.Commands.DeleteExpense;

public class DeleteExpenseValidator : AbstractValidator<DeleteExpenseCommand>
{
    public DeleteExpenseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
