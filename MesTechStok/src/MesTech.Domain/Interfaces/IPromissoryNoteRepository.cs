using MesTech.Domain.Entities.Finance;

namespace MesTech.Domain.Interfaces;

public interface IPromissoryNoteRepository
{
    Task<PromissoryNote?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PromissoryNote>> GetByTenantAsync(Guid tenantId, NoteStatus? status = null, CancellationToken ct = default);
    Task<IReadOnlyList<PromissoryNote>> GetOverdueAsync(Guid tenantId, CancellationToken ct = default);
    Task AddAsync(PromissoryNote note, CancellationToken ct = default);
    Task UpdateAsync(PromissoryNote note, CancellationToken ct = default);
}
