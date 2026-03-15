using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Commands.MarkExpensePaid;

public class MarkExpensePaidHandler : IRequestHandler<MarkExpensePaidCommand, Unit>
{
    private readonly IFinanceExpenseRepository _expenses;
    private readonly IUnitOfWork _uow;

    public MarkExpensePaidHandler(IFinanceExpenseRepository expenses, IUnitOfWork uow)
        => (_expenses, _uow) = (expenses, uow);

    public async Task<Unit> Handle(MarkExpensePaidCommand req, CancellationToken ct)
    {
        var expense = await _expenses.GetByIdAsync(req.ExpenseId, ct)
            ?? throw new InvalidOperationException($"Expense {req.ExpenseId} not found.");

        expense.MarkAsPaid(req.BankAccountId);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
