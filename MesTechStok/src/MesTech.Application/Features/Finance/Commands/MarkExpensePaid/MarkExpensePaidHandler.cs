using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Commands.MarkExpensePaid;

public sealed class MarkExpensePaidHandler : IRequestHandler<MarkExpensePaidCommand, Unit>
{
    private readonly IFinanceExpenseRepository _expenses;
    private readonly IUnitOfWork _uow;

    public MarkExpensePaidHandler(IFinanceExpenseRepository expenses, IUnitOfWork uow)
        => (_expenses, _uow) = (expenses, uow);

    public async Task<Unit> Handle(MarkExpensePaidCommand request, CancellationToken cancellationToken)
    {
        var expense = await _expenses.GetByIdAsync(request.ExpenseId, cancellationToken)
            ?? throw new InvalidOperationException($"Expense {request.ExpenseId} not found.");

        expense.MarkAsPaid(request.BankAccountId);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
