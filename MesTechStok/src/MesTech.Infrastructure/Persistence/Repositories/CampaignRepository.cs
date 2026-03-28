using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public sealed class CampaignRepository : ICampaignRepository
{
    private readonly AppDbContext _context;

    public CampaignRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Campaign?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Campaigns
            .Include(c => c.Products)
            .AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Campaign>> GetActiveByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Campaigns
            .Where(c => c.TenantId == tenantId && c.IsActive)
            .OrderByDescending(c => c.StartDate)
            .AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<Campaign>> GetActiveByProductIdAsync(Guid productId, CancellationToken ct = default)
        => await _context.Campaigns
            .Include(c => c.Products)
            .Where(c => c.IsActive && c.Products.Any(p => p.ProductId == productId))
            .AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(Campaign campaign, CancellationToken ct = default)
        => await _context.Campaigns.AddAsync(campaign, ct);
}
