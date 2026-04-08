using MesTech.Domain.Entities.Erp;

namespace MesTech.Application.Interfaces;

public interface IErpFieldMappingRepository
{
    Task<ErpFieldMapping?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ErpFieldMapping>> GetByErpTypeAsync(Guid tenantId, string erpType, CancellationToken ct = default);
    Task AddAsync(ErpFieldMapping mapping, CancellationToken ct = default);
    Task UpdateAsync(ErpFieldMapping mapping, CancellationToken ct = default);
}
