using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IQuotationRepository
{
    Task<Quotation?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Quotation?> GetByIdWithLinesAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Quotation>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Quotation>> GetByStatusAsync(QuotationStatus status, CancellationToken ct = default);
    Task AddAsync(Quotation quotation, CancellationToken ct = default);
    Task UpdateAsync(Quotation quotation, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
