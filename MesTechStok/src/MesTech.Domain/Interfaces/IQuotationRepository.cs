using MesTech.Domain.Entities;
using MesTech.Domain.Enums;

namespace MesTech.Domain.Interfaces;

public interface IQuotationRepository
{
    Task<Quotation?> GetByIdAsync(Guid id);
    Task<Quotation?> GetByIdWithLinesAsync(Guid id);
    Task<IReadOnlyList<Quotation>> GetAllAsync();
    Task<IReadOnlyList<Quotation>> GetByStatusAsync(QuotationStatus status);
    Task AddAsync(Quotation quotation);
    Task UpdateAsync(Quotation quotation);
    Task DeleteAsync(Guid id);
}
