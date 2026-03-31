using MesTech.Domain.Interfaces;
using MediatR;

namespace MesTech.Application.Features.Erp.Queries.GetErpAccountMappings;

public sealed class GetErpAccountMappingsHandler
    : IRequestHandler<GetErpAccountMappingsQuery, IReadOnlyList<ErpAccountMappingDto>>
{
    private readonly IErpAccountMappingRepository _repo;

    public GetErpAccountMappingsHandler(IErpAccountMappingRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<ErpAccountMappingDto>> Handle(
        GetErpAccountMappingsQuery request, CancellationToken cancellationToken)
    {
        var mappings = await _repo.GetByTenantAsync(request.TenantId, cancellationToken).ConfigureAwait(false);

        return mappings
            .OrderBy(m => m.MesTechAccountCode, StringComparer.Ordinal)
            .Select(m => new ErpAccountMappingDto
            {
                Id = m.Id,
                MesTechAccountCode = m.MesTechAccountCode,
                MesTechAccountName = m.MesTechAccountName,
                MesTechAccountType = m.MesTechAccountType,
                ErpAccountCode = m.ErpAccountCode,
                ErpAccountName = m.ErpAccountName,
                IsActive = m.IsActive,
                LastSyncAt = m.LastSyncAt,
                CreatedAt = m.CreatedAt
            })
            .ToList()
            .AsReadOnly();
    }
}
