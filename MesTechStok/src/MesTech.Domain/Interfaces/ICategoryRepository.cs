using MesTech.Domain.Entities;

namespace MesTech.Domain.Interfaces;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Category>> GetAllAsync();
    Task<IReadOnlyList<Category>> GetActiveAsync();
    Task AddAsync(Category category);
    Task UpdateAsync(Category category);
    Task DeleteAsync(Guid id);
}
