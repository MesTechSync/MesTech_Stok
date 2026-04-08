using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateTaxCalendarItem;

public sealed class CreateTaxCalendarItemHandler : IRequestHandler<CreateTaxCalendarItemCommand, Guid>
{
    private readonly ITaxCalendarItemRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateTaxCalendarItemHandler(ITaxCalendarItemRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateTaxCalendarItemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var item = TaxCalendarItem.Create(
            request.TenantId,
            request.TaxType,
            request.DueDay,
            request.DueMonth,
            request.Description,
            request.Frequency,
            request.IsAutoCalculated);

        await _repository.AddAsync(item, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return item.Id;
    }
}
