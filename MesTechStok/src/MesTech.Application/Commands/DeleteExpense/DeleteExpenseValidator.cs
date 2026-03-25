using FluentValidation;

namespace MesTech.Application.Commands.DeleteExpense;

public sealed class DeleteExpenseValidator : AbstractValidator<DeleteExpenseCommand>
{
    public DeleteExpenseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
