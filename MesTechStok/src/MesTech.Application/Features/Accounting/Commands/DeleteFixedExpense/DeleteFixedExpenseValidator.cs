using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.DeleteFixedExpense;

public sealed class DeleteFixedExpenseValidator : AbstractValidator<DeleteFixedExpenseCommand>
{
    public DeleteFixedExpenseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
