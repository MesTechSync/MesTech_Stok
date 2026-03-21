using FluentValidation;

namespace MesTech.Application.Features.Accounting.Commands.UpdateFixedExpense;

public class UpdateFixedExpenseValidator : AbstractValidator<UpdateFixedExpenseCommand>
{
    public UpdateFixedExpenseValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
