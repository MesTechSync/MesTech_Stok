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

    public async Task<Guid> Handle(CreateExpenseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var expense = FinanceExpense.Create(request.TenantId, request.Title, request.Amount, request.Category,
            request.ExpenseDate, request.SubmittedByUserId, request.Notes, request.StoreId);
        await _repository.AddAsync(expense, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return expense.Id;
    }
}
