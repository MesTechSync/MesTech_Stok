using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateFixedExpense;

public class CreateFixedExpenseHandler : IRequestHandler<CreateFixedExpenseCommand, Guid>
{
    private readonly IFixedExpenseRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateFixedExpenseHandler(IFixedExpenseRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateFixedExpenseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var expense = FixedExpense.Create(
            request.TenantId,
            request.Name,
            request.MonthlyAmount,
            request.DayOfMonth,
            request.StartDate,
            request.Currency,
            request.EndDate,
            request.SupplierName,
            request.SupplierId,
            request.Notes);

        await _repository.AddAsync(expense, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return expense.Id;
    }
}
