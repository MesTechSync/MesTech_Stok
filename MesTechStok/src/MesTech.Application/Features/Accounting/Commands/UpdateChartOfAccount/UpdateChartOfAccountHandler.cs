using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;

public sealed class UpdateChartOfAccountHandler : IRequestHandler<UpdateChartOfAccountCommand, bool>
{
    private readonly IChartOfAccountsRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateChartOfAccountHandler(IChartOfAccountsRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<bool> Handle(UpdateChartOfAccountCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var account = await _repository.GetByIdAsync(request.Id, cancellationToken).ConfigureAwait(false);
        if (account == null) return false;

        // Domain guard: throws if IsSystem=true
        account.UpdateName(request.Name);

        await _repository.UpdateAsync(account, cancellationToken).ConfigureAwait(false);
        await _uow.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }
}
