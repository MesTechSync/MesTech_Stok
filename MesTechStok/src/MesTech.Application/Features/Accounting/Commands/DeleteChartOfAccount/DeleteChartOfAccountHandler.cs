using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.DeleteChartOfAccount;

public sealed class DeleteChartOfAccountHandler : IRequestHandler<DeleteChartOfAccountCommand, bool>
{
    private readonly IChartOfAccountsRepository _repository;
    private readonly IUnitOfWork _uow;

    public DeleteChartOfAccountHandler(IChartOfAccountsRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<bool> Handle(DeleteChartOfAccountCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var account = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (account == null) return false;

        // Domain guard: throws InvalidOperationException if IsSystem=true
        account.MarkDeleted(request.DeletedBy);

        await _repository.UpdateAsync(account, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
