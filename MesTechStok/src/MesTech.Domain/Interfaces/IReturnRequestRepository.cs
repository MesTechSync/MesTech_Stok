using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface IReturnRequestRepository
{
    Task<ReturnRequest?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<ReturnRequest>> GetByOrderIdAsync(Guid orderId);
    Task AddAsync(ReturnRequest returnRequest);
    Task UpdateAsync(ReturnRequest returnRequest);
}
