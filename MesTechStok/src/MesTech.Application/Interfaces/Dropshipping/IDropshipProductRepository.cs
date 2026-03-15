using MesTech.Domain.Dropshipping.Entities;

namespace MesTech.Application.Interfaces.Dropshipping;

/// <summary>
/// Dropshipping ürün veri erişim arayüzü.
/// </summary>
public interface IDropshipProductRepository
{
    Task<DropshipProduct?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DropshipProduct>> GetByTenantAsync(Guid tenantId, bool? isLinked = null, CancellationToken ct = default);
    Task<IReadOnlyList<DropshipProduct>> GetBySupplierAsync(Guid supplierId, CancellationToken ct = default);
    Task AddAsync(DropshipProduct product, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<DropshipProduct> products, CancellationToken ct = default);
    Task UpdateAsync(DropshipProduct product, CancellationToken ct = default);
}
