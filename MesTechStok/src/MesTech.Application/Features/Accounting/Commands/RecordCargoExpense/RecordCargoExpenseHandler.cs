using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.RecordCargoExpense;

public sealed class RecordCargoExpenseHandler : IRequestHandler<RecordCargoExpenseCommand, Guid>
{
    private readonly ICargoExpenseRepository _repository;
    private readonly IUnitOfWork _uow;

    public RecordCargoExpenseHandler(ICargoExpenseRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(RecordCargoExpenseCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var expense = CargoExpense.Create(
            request.TenantId, request.CarrierName, request.Cost, request.OrderId, request.TrackingNumber);

        await _repository.AddAsync(expense, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return expense.Id;
    }
}
