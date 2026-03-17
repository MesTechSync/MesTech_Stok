using MesTech.Domain.Accounting.Entities;

namespace MesTech.Application.Interfaces.Accounting;

public interface ISalaryRecordRepository
{
    Task<SalaryRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<SalaryRecord>> GetAllAsync(Guid tenantId, int? year = null, int? month = null, CancellationToken ct = default);
    Task AddAsync(SalaryRecord record, CancellationToken ct = default);
    Task UpdateAsync(SalaryRecord record, CancellationToken ct = default);
}
