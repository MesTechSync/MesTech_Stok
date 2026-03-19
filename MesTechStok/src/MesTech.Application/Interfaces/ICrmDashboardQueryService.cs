using MesTech.Application.DTOs.Crm;

namespace MesTech.Application.Interfaces;

/// <summary>
/// CRM dashboard icin cross-entity sorgulama servisi.
/// Infrastructure katmaninda EF Core ile implement edilir.
/// </summary>
public interface ICrmDashboardQueryService
{
    Task<CrmDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken ct = default);
    Task<(IReadOnlyList<CustomerCrmDto> Items, int TotalCount)> GetCustomersPagedAsync(
        Guid tenantId, bool? isVip, bool? isActive, string? searchTerm,
        int page, int pageSize, CancellationToken ct = default);
    Task<(IReadOnlyList<SupplierCrmDto> Items, int TotalCount)> GetSuppliersPagedAsync(
        Guid tenantId, bool? isActive, bool? isPreferred, string? searchTerm,
        int page, int pageSize, CancellationToken ct = default);
}
