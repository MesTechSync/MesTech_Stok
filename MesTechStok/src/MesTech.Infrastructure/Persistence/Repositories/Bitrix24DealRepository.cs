using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class Bitrix24DealRepository : IBitrix24DealRepository
{
    private readonly AppDbContext _context;

    public Bitrix24DealRepository(AppDbContext context) => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<Bitrix24Deal?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Bitrix24Deals.FindAsync([id], ct).ConfigureAwait(false);

    public async Task<Bitrix24Deal?> GetByOrderIdAsync(Guid orderId, CancellationToken ct = default)
        => await _context.Bitrix24Deals
            .AsNoTracking().FirstOrDefaultAsync(d => d.OrderId == orderId, ct).ConfigureAwait(false);

    public async Task<Bitrix24Deal?> GetByExternalDealIdAsync(string externalDealId, CancellationToken ct = default)
        => await _context.Bitrix24Deals
            .AsNoTracking().FirstOrDefaultAsync(d => d.ExternalDealId == externalDealId, ct).ConfigureAwait(false);

    public async Task AddAsync(Bitrix24Deal deal, CancellationToken ct = default)
        => await _context.Bitrix24Deals.AddAsync(deal, ct).ConfigureAwait(false);

    public Task UpdateAsync(Bitrix24Deal deal, CancellationToken ct = default)
    {
        _context.Bitrix24Deals.Update(deal);
        return Task.CompletedTask;
    }
}
