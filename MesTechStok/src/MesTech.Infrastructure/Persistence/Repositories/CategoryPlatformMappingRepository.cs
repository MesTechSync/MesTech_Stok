using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core backed category-platform mapping repository.
/// </summary>
public sealed class CategoryPlatformMappingRepository : ICategoryPlatformMappingRepository
{
    private readonly AppDbContext _dbContext;

    public CategoryPlatformMappingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CategoryPlatformMapping?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbContext.CategoryPlatformMappings
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<IReadOnlyList<CategoryPlatformMapping>> GetByTenantAsync(
        Guid tenantId, PlatformType? platform = null, CancellationToken ct = default)
    {
        var query = _dbContext.CategoryPlatformMappings
            .Where(m => m.TenantId == tenantId);

        if (platform.HasValue)
            query = query.Where(m => m.PlatformType == platform.Value);

        return await query.ToListAsync(ct);
    }

    public async Task<CategoryPlatformMapping?> GetByCategoryAndPlatformAsync(
        Guid tenantId, Guid categoryId, PlatformType platform, CancellationToken ct = default)
    {
        return await _dbContext.CategoryPlatformMappings
            .FirstOrDefaultAsync(m => m.TenantId == tenantId && m.CategoryId == categoryId && m.PlatformType == platform, ct);
    }

    public async Task AddAsync(CategoryPlatformMapping mapping, CancellationToken ct = default)
    {
        await _dbContext.CategoryPlatformMappings.AddAsync(mapping, ct);
    }

    public Task UpdateAsync(CategoryPlatformMapping mapping, CancellationToken ct = default)
    {
        _dbContext.CategoryPlatformMappings.Update(mapping);
        return Task.CompletedTask;
    }
}
