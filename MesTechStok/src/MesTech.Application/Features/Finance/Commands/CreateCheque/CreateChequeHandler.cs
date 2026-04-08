using MediatR;
using MesTech.Domain.Entities.Finance;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Finance.Commands.CreateCheque;

public sealed class CreateChequeHandler : IRequestHandler<CreateChequeCommand, Guid>
{
    private readonly IChequeRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateChequeHandler(IChequeRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateChequeCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var cheque = Cheque.Create(
            request.TenantId, request.ChequeNumber, request.Amount,
            request.IssueDate, request.MaturityDate,
            request.BankName, request.Type, request.DrawerName);

        await _repository.AddAsync(cheque, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return cheque.Id;
    }
}
