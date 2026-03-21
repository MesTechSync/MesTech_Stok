using MediatR;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Commands.CreateCashRegister;

public class CreateCashRegisterHandler : IRequestHandler<CreateCashRegisterCommand, Guid>
{
    private readonly ICashRegisterRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateCashRegisterHandler(ICashRegisterRepository repo, IUnitOfWork uow)
        => (_repository, _uow) = (repo, uow);

    public async Task<Guid> Handle(CreateCashRegisterCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var cashRegister = CashRegister.Create(
            request.TenantId, request.Name, request.CurrencyCode,
            request.IsDefault, request.OpeningBalance);
        await _repository.AddAsync(cashRegister, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return cashRegister.Id;
    }
}
