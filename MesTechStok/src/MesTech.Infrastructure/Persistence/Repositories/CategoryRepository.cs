using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<Category>> GetActiveAsync()
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .AsNoTracking().ToListAsync();
    }

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
    }

    public async Task UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.Categories.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
        }
    }
}
