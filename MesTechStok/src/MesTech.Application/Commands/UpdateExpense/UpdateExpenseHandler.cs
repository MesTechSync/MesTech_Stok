using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.UpdateExpense;

public sealed class UpdateExpenseHandler : IRequestHandler<UpdateExpenseCommand>
{
    private readonly IExpenseRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateExpenseHandler(IExpenseRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(UpdateExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _repository.GetByIdAsync(request.Id)
            ?? throw new KeyNotFoundException($"Expense {request.Id} not found.");

        if (request.Description is not null) expense.Description = request.Description;
        if (request.Amount.HasValue) expense.SetAmount(request.Amount.Value);
        if (request.ExpenseType.HasValue) expense.ExpenseType = request.ExpenseType.Value;
        if (request.Note is not null) expense.Note = request.Note;
        // PaymentStatus: use domain methods (MarkAsProcessing/MarkAsCompleted/Cancel) instead of direct set

        await _repository.UpdateAsync(expense).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
