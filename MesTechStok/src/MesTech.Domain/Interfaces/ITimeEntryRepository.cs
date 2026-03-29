using MesTech.Domain.Entities.Tasks;

namespace MesTech.Domain.Interfaces;

public interface ITimeEntryRepository
{
    Task<IReadOnlyList<TimeEntry>> GetByTenantAsync(
        Guid tenantId, DateTime from, DateTime to, Guid? userId = null,
        int page = 1, int pageSize = 50, CancellationToken ct = default);

    Task<TimeEntry?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(TimeEntry entry, CancellationToken ct = default);
    Task UpdateAsync(TimeEntry entry, CancellationToken ct = default);
}
