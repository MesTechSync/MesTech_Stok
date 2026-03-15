using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateCounterparty;

public class CreateCounterpartyHandler : IRequestHandler<CreateCounterpartyCommand, Guid>
{
    private readonly ICounterpartyRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateCounterpartyHandler(ICounterpartyRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateCounterpartyCommand request, CancellationToken cancellationToken)
    {
        var counterparty = Counterparty.Create(
            request.TenantId, request.Name, request.CounterpartyType,
            request.VKN, request.Phone, request.Email, request.Address, request.Platform);

        await _repository.AddAsync(counterparty, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return counterparty.Id;
    }
}
