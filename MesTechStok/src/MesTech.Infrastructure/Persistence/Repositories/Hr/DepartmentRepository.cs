using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Infrastructure.Persistence.Repositories.Hr;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _context;
    public DepartmentRepository(AppDbContext context) => _context = context;

    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Departments.FindAsync([id], ct);

    public async Task<IReadOnlyList<Department>> GetByTenantAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.Departments.Where(d => d.TenantId == tenantId)
                         .OrderBy(d => d.Name).AsNoTracking().ToListAsync(ct);

    public async Task AddAsync(Department department, CancellationToken ct = default)
        => await _context.Departments.AddAsync(department, ct);
}
