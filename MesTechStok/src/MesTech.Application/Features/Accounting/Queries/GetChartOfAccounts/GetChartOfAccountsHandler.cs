using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Interfaces.Accounting;

namespace MesTech.Application.Features.Accounting.Queries.GetChartOfAccounts;

public sealed class GetChartOfAccountsHandler : IRequestHandler<GetChartOfAccountsQuery, IReadOnlyList<ChartOfAccountsDto>>
{
    private readonly IChartOfAccountsRepository _repository;

    public GetChartOfAccountsHandler(IChartOfAccountsRepository repository)
        => _repository = repository;

    public async Task<IReadOnlyList<ChartOfAccountsDto>> Handle(GetChartOfAccountsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var accounts = await _repository.GetAllAsync(request.TenantId, request.IsActive, cancellationToken).ConfigureAwait(false);
        return accounts.Select(a => new ChartOfAccountsDto
        {
            Id = a.Id,
            Code = a.Code,
            Name = a.Name,
            AccountType = a.AccountType.ToString(),
            ParentId = a.ParentId,
            IsActive = a.IsActive,
            IsSystem = a.IsSystem,
            Level = a.Level
        }).ToList().AsReadOnly();
    }
}
