using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.UpdateChartOfAccount;

public class UpdateChartOfAccountHandler : IRequestHandler<UpdateChartOfAccountCommand, bool>
{
    private readonly IChartOfAccountsRepository _repository;
    private readonly IUnitOfWork _uow;

    public UpdateChartOfAccountHandler(IChartOfAccountsRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<bool> Handle(UpdateChartOfAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (account == null) return false;

        // Domain guard: throws if IsSystem=true
        account.UpdateName(request.Name);

        await _repository.UpdateAsync(account, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
