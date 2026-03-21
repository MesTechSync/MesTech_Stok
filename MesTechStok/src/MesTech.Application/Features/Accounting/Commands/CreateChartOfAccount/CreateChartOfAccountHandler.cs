using MediatR;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Accounting.Commands.CreateChartOfAccount;

public class CreateChartOfAccountHandler : IRequestHandler<CreateChartOfAccountCommand, Guid>
{
    private readonly IChartOfAccountsRepository _repository;
    private readonly IUnitOfWork _uow;

    public CreateChartOfAccountHandler(IChartOfAccountsRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<Guid> Handle(CreateChartOfAccountCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        // Check for duplicate code within tenant
        var existing = await _repository.GetByCodeAsync(request.TenantId, request.Code, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Account code '{request.Code}' already exists.");

        var account = ChartOfAccounts.Create(
            request.TenantId,
            request.Code,
            request.Name,
            request.AccountType,
            request.ParentId,
            request.Level);

        await _repository.AddAsync(account, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return account.Id;
    }
}
