using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateAccountingExpense;

public class CreateAccountingExpenseHandler : IRequestHandler<CreateAccountingExpenseCommand, Guid>
{
    private readonly IPersonalExpenseRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateAccountingExpenseHandler(IPersonalExpenseRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateAccountingExpenseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var expense = PersonalExpense.Create(
            request.TenantId, request.Title, request.Amount, request.ExpenseDate, request.Source, request.Category);

        await _repository.AddAsync(expense, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return expense.Id;
    }
}
