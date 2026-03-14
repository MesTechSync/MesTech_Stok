using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface ICrmDealRepository
{
    Task<Deal?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Deal>> GetByPipelineAsync(
        Guid tenantId, Guid pipelineId, DealStatus? status, CancellationToken ct = default);
    Task<IReadOnlyList<Deal>> GetByContactAsync(Guid contactId, CancellationToken ct = default);
    Task AddAsync(Deal deal, CancellationToken ct = default);
}
