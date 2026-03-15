using MesTech.Domain.Dropshipping.Entities;

namespace MesTech.Application.Interfaces.Dropshipping;

/// <summary>
/// Dropshipping tedarikçi veri erişim arayüzü.
/// </summary>
public interface IDropshipSupplierRepository
{
    Task<DropshipSupplier?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DropshipSupplier>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(DropshipSupplier supplier, CancellationToken ct = default);
    Task UpdateAsync(DropshipSupplier supplier, CancellationToken ct = default);
}
