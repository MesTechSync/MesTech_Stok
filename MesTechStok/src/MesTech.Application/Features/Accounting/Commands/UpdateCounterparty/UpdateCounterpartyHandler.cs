using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.UpdateCounterparty;

public class UpdateCounterpartyHandler : IRequestHandler<UpdateCounterpartyCommand, bool>
{
    private readonly ICounterpartyRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateCounterpartyHandler(ICounterpartyRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<bool> Handle(UpdateCounterpartyCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var counterparty = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (counterparty == null) return false;

        counterparty.Update(request.Name, request.VKN, request.Phone, request.Email, request.Address, request.Platform);

        await _repository.UpdateAsync(counterparty, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
