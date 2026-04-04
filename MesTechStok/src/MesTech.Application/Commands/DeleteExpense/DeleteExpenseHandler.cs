using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.DeleteExpense;

public sealed class DeleteExpenseHandler : IRequestHandler<DeleteExpenseCommand>
{
    private readonly IExpenseRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteExpenseHandler(IExpenseRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(DeleteExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Expense {request.Id} not found.");

        expense.IsDeleted = true;
        expense.DeletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(expense, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
