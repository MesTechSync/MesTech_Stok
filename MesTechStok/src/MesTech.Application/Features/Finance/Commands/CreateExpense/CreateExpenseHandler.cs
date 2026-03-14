using MediatR;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Commands.CreateExpense;

public class CreateExpenseHandler : IRequestHandler<CreateExpenseCommand, Guid>
{
    private readonly IFinanceExpenseRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateExpenseHandler(IFinanceExpenseRepository repo, IUnitOfWork uow)
        => (_repository, _uow) = (repo, uow);

    public async Task<Guid> Handle(CreateExpenseCommand req, CancellationToken ct)
    {
        var expense = FinanceExpense.Create(req.TenantId, req.Title, req.Amount, req.Category,
            req.ExpenseDate, req.SubmittedByUserId, req.Notes, req.StoreId);
        await _repository.AddAsync(expense, ct);
        await _uow.SaveChangesAsync(ct);
        return expense.Id;
    }
}
