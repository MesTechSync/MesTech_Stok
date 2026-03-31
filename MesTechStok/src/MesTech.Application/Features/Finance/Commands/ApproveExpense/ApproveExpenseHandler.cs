using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Commands.ApproveExpense;

public sealed class ApproveExpenseHandler : IRequestHandler<ApproveExpenseCommand, Unit>
{
    private readonly IFinanceExpenseRepository _expenses;
    private readonly IUnitOfWork _uow;

    public ApproveExpenseHandler(IFinanceExpenseRepository expenses, IUnitOfWork uow)
        => (_expenses, _uow) = (expenses, uow);

    public async Task<Unit> Handle(ApproveExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _expenses.GetByIdAsync(request.ExpenseId, cancellationToken)
            ?? throw new InvalidOperationException($"Expense {request.ExpenseId} not found.");

        expense.Approve(request.ApproverUserId);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
