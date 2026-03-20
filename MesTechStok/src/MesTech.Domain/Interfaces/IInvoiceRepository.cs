using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(Guid id);
    Task<Invoice?> GetByOrderIdAsync(Guid orderId);
    Task AddAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
}
