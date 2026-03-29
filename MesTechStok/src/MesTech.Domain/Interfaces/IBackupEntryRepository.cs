using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IBackupEntryRepository
{
    Task<IReadOnlyList<BackupEntry>> GetByTenantAsync(Guid tenantId, int limit = 20, CancellationToken ct = default);
    Task AddAsync(BackupEntry entry, CancellationToken ct = default);
}
