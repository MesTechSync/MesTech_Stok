using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Commands.UpdateExpense;

public class UpdateExpenseHandler : IRequestHandler<UpdateExpenseCommand>
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
        if (request.Amount.HasValue) expense.Amount = request.Amount.Value;
        if (request.ExpenseType.HasValue) expense.ExpenseType = request.ExpenseType.Value;
        if (request.PaymentStatus.HasValue) expense.PaymentStatus = request.PaymentStatus.Value;
        if (request.Note is not null) expense.Note = request.Note;

        await _repository.UpdateAsync(expense);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
