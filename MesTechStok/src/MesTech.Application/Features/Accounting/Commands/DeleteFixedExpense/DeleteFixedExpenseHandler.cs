using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.DeleteFixedExpense;

public class DeleteFixedExpenseHandler : IRequestHandler<DeleteFixedExpenseCommand>
{
    private readonly IFixedExpenseRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteFixedExpenseHandler(IFixedExpenseRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(DeleteFixedExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"FixedExpense {request.Id} not found.");

        expense.IsDeleted = true;
        expense.DeletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(expense, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
