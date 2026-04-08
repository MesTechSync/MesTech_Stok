using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class LoyaltyTransactionRepository : ILoyaltyTransactionRepository
{
    private readonly AppDbContext _context;

    public LoyaltyTransactionRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<IReadOnlyList<LoyaltyTransaction>> GetByCustomerAsync(
        Guid tenantId, Guid customerId, CancellationToken ct = default)
        => await _context.LoyaltyTransactions
            .Where(t => t.TenantId == tenantId && t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(1000) // G485: pagination guard
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<LoyaltyTransaction>> GetByCustomerPagedAsync(
        Guid tenantId, Guid customerId, int take, CancellationToken ct = default)
        => await _context.LoyaltyTransactions
            .Where(t => t.TenantId == tenantId && t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(Math.Clamp(take, 1, 100))
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<int> GetPointsSumByTypeAsync(
        Guid tenantId, Guid customerId, LoyaltyTransactionType type, CancellationToken ct = default)
        => await _context.LoyaltyTransactions
            .Where(t => t.TenantId == tenantId && t.CustomerId == customerId && t.Type == type)
            .SumAsync(t => t.Points, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<LoyaltyTransaction>> GetExpirableEarnTransactionsAsync(
        DateTime olderThan, CancellationToken ct = default)
        => await _context.LoyaltyTransactions
            .Where(t => t.Type == LoyaltyTransactionType.Earn
                     && t.CreatedAt < olderThan)
            .OrderBy(t => t.CreatedAt)
            .Take(1000) // G485: pagination guard
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task AddAsync(LoyaltyTransaction transaction, CancellationToken ct = default)
    {
        await _context.LoyaltyTransactions.AddAsync(transaction, ct).ConfigureAwait(false);
    }
}
