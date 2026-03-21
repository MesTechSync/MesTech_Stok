using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class ReturnRequestRepository : IReturnRequestRepository
{
    private readonly AppDbContext _context;

    public ReturnRequestRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<ReturnRequest?> GetByIdAsync(Guid id)
        => await _context.ReturnRequests.FindAsync(id).ConfigureAwait(false);

    public async Task<IReadOnlyList<ReturnRequest>> GetByOrderIdAsync(Guid orderId)
        => await _context.ReturnRequests
            .Where(r => r.OrderId == orderId)
            .OrderByDescending(r => r.RequestDate)
            .AsNoTracking().ToListAsync()
            .ConfigureAwait(false);

    public async Task AddAsync(ReturnRequest returnRequest)
        => await _context.ReturnRequests.AddAsync(returnRequest).ConfigureAwait(false);

    public Task UpdateAsync(ReturnRequest returnRequest)
    {
        _context.ReturnRequests.Update(returnRequest);
        return Task.CompletedTask;
    }
}
