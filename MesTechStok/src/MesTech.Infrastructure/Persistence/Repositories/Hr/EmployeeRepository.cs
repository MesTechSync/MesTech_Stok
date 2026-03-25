using Microsoft.EntityFrameworkCore;
using MesTech.Domain.Entities.Hr;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Persistence;

namespace MesTech.Infrastructure.Persistence.Repositories.Hr;

public sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _context;
    public EmployeeRepository(AppDbContext context) => _context = context;

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Employees.FindAsync([id], ct);

    public async Task<IReadOnlyList<Employee>> GetByTenantAsync(
        Guid tenantId, EmployeeStatus? status = null, CancellationToken ct = default)
    {
        var q = _context.Employees.Where(e => e.TenantId == tenantId);
        if (status.HasValue) q = q.Where(e => e.Status == status.Value);
        return await q.OrderBy(e => e.EmployeeCode).AsNoTracking().ToListAsync(ct);
    }

    public async Task AddAsync(Employee employee, CancellationToken ct = default)
        => await _context.Employees.AddAsync(employee, ct);
}
