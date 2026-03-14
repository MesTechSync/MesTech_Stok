using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface ICrmLeadRepository
{
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<Lead> Items, int TotalCount)> GetPagedAsync(
        Guid tenantId, LeadStatus? status, Guid? assignedToUserId,
        int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Lead lead, CancellationToken ct = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);
}
