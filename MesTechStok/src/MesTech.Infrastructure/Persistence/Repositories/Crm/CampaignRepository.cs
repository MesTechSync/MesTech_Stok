using MesTech.Domain.Entities.Crm;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories.Crm;

public class CampaignRepository : ICampaignRepository
{
    private readonly AppDbContext _context;

    public CampaignRepository(AppDbContext context) => _context = context;

    public async Task<Campaign?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Set<Campaign>()
            .AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct);

    public async Task<IReadOnlyList<Campaign>> GetByTenantAsync(
        Guid tenantId, bool? activeOnly = null, CancellationToken ct = default)
    {
        var query = _context.Set<Campaign>()
            .AsNoTracking()
            .Where(c => c.TenantId == tenantId && !c.IsDeleted);

        if (activeOnly == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(c => c.IsActive && c.StartDate <= now && c.EndDate >= now);
        }

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Campaign campaign, CancellationToken ct = default)
    {
        await _context.Set<Campaign>().AddAsync(campaign, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Campaign campaign, CancellationToken ct = default)
    {
        _context.Set<Campaign>().Update(campaign);
        await _context.SaveChangesAsync(ct);
    }
}
