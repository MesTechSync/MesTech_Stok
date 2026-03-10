using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id);
    Task AddAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
}
