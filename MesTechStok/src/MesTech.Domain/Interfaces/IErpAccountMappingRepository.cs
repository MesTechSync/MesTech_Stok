using MesTech.Domain.Entities.Erp;

namespace MesTech.Domain.Interfaces;

public interface IErpAccountMappingRepository
{
    Task<IReadOnlyList<ErpAccountMapping>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task<ErpAccountMapping?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ErpAccountMapping?> FindByMesTechCodeAsync(Guid tenantId, string mesTechCode, CancellationToken ct = default);
    Task<ErpAccountMapping?> FindByErpCodeAsync(Guid tenantId, string erpCode, CancellationToken ct = default);
    Task AddAsync(ErpAccountMapping mapping, CancellationToken ct = default);
    void Remove(ErpAccountMapping mapping);
}
