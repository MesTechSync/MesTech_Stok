using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class CategoryPlatformMappingRepository : ICategoryPlatformMappingRepository
{
    private readonly AppDbContext _context;

    public CategoryPlatformMappingRepository(AppDbContext context)
        => _context = context ?? throw new ArgumentNullException(nameof(context));

    public async Task<CategoryPlatformMapping?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Set<CategoryPlatformMapping>()
            .AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<CategoryPlatformMapping>> GetByTenantAsync(
        Guid tenantId, PlatformType? platform = null, CancellationToken ct = default)
    {
        var query = _context.Set<CategoryPlatformMapping>()
            .Where(m => m.TenantId == tenantId);

        if (platform.HasValue)
            query = query.Where(m => m.PlatformType == platform.Value);

        return await query.AsNoTracking().ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<CategoryPlatformMapping?> GetByCategoryAndPlatformAsync(
        Guid tenantId, Guid categoryId, PlatformType platform, CancellationToken ct = default)
        => await _context.Set<CategoryPlatformMapping>()
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.TenantId == tenantId
                                   && m.CategoryId == categoryId
                                   && m.PlatformType == platform, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(CategoryPlatformMapping mapping, CancellationToken ct = default)
        => await _context.Set<CategoryPlatformMapping>().AddAsync(mapping, ct).ConfigureAwait(false);

    public Task UpdateAsync(CategoryPlatformMapping mapping, CancellationToken ct = default)
    {
        _context.Set<CategoryPlatformMapping>().Update(mapping);
        return Task.CompletedTask;
    }
}
