using FluentValidation;

namespace MesTech.Application.Features.Finance.Commands.ApproveExpense;

public sealed class ApproveExpenseValidator : AbstractValidator<ApproveExpenseCommand>
{
    public ApproveExpenseValidator()
    {
        RuleFor(x => x.ExpenseId).NotEmpty();
        RuleFor(x => x.ApproverUserId).NotEmpty();
    }
}
