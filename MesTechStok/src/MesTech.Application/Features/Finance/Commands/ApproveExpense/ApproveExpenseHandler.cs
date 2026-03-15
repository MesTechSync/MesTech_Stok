using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Commands.ApproveExpense;

public class ApproveExpenseHandler : IRequestHandler<ApproveExpenseCommand, Unit>
{
    private readonly IFinanceExpenseRepository _expenses;
    private readonly IUnitOfWork _uow;

    public ApproveExpenseHandler(IFinanceExpenseRepository expenses, IUnitOfWork uow)
        => (_expenses, _uow) = (expenses, uow);

    public async Task<Unit> Handle(ApproveExpenseCommand req, CancellationToken ct)
    {
        var expense = await _expenses.GetByIdAsync(req.ExpenseId, ct)
            ?? throw new InvalidOperationException($"Expense {req.ExpenseId} not found.");

        expense.Approve(req.ApproverUserId);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
