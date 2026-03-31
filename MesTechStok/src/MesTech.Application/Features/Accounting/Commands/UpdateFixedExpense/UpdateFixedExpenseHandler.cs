using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.UpdateFixedExpense;

public sealed class UpdateFixedExpenseHandler : IRequestHandler<UpdateFixedExpenseCommand>
{
    private readonly IFixedExpenseRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateFixedExpenseHandler(IFixedExpenseRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task Handle(UpdateFixedExpenseCommand request, CancellationToken cancellationToken)
    {
        var expense = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"FixedExpense {request.Id} not found.");

        if (request.MonthlyAmount.HasValue)
            expense.UpdateAmount(request.MonthlyAmount.Value);

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value)
                expense.Activate();
            else
                expense.Deactivate();
        }

        await _repository.UpdateAsync(expense, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
