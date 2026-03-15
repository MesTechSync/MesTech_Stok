using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Accounting.Repositories;

public class CargoExpenseRepository : ICargoExpenseRepository
{
    private readonly AppDbContext _context;
    public CargoExpenseRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<CargoExpense>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.CargoExpenses
            .Where(e => e.TenantId == tenantId && e.CreatedAt >= from && e.CreatedAt <= to)
            .OrderByDescending(e => e.CreatedAt)
            .AsNoTracking().ToListAsync(ct);

    public async Task<decimal> GetTotalCostAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken ct = default)
        => await _context.CargoExpenses
            .Where(e => e.TenantId == tenantId && e.CreatedAt >= from && e.CreatedAt <= to)
            .SumAsync(e => e.Cost, ct);

    public async Task AddAsync(CargoExpense expense, CancellationToken ct = default)
        => await _context.CargoExpenses.AddAsync(expense, ct);
}
