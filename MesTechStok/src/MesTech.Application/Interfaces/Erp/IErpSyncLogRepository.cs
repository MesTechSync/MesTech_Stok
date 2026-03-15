using MesTech.Domain.Entities.Erp;
using MesTech.Domain.Enums;

namespace MesTech.Application.Interfaces.Erp;

/// <summary>
/// ErpSyncLog repository — sync log kayitlarinin CRUD islemleri.
/// Dalga 11: ERP entegrasyonu icin eklendi.
/// </summary>
public interface IErpSyncLogRepository
{
    /// <summary>Yeni sync log kaydi ekler.</summary>
    Task AddAsync(ErpSyncLog log, CancellationToken ct = default);

    /// <summary>Mevcut sync log kaydini gunceller.</summary>
    Task UpdateAsync(ErpSyncLog log, CancellationToken ct = default);

    /// <summary>ID ile sync log kaydi getirir.</summary>
    Task<ErpSyncLog?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Belirli bir entity icin son sync log kaydini getirir.</summary>
    Task<ErpSyncLog?> GetLatestByEntityAsync(
        Guid tenantId,
        string entityType,
        Guid entityId,
        CancellationToken ct = default);

    /// <summary>Retry bekleyen (NextRetryAt dolmus) log kayitlarini getirir.</summary>
    Task<IReadOnlyList<ErpSyncLog>> GetPendingRetriesAsync(
        Guid tenantId,
        DateTime asOf,
        CancellationToken ct = default);

    /// <summary>Belirli bir ERP saglayicisi icin basarisiz sync kayitlarini getirir.</summary>
    Task<IReadOnlyList<ErpSyncLog>> GetFailedByProviderAsync(
        Guid tenantId,
        ErpProvider provider,
        int limit = 50,
        CancellationToken ct = default);
}
