using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Persistence.Repositories;

public class StockMovementRepository : IStockMovementRepository
{
    private readonly AppDbContext _context;

    public StockMovementRepository(AppDbContext context) => _context = context;

    public async Task<StockMovement?> GetByIdAsync(Guid id)
        => await _context.StockMovements.FindAsync(id);

    public async Task<IReadOnlyList<StockMovement>> GetByProductIdAsync(Guid productId)
        => await _context.StockMovements
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.Date)
            .ToListAsync();

    public async Task<IReadOnlyList<StockMovement>> GetByDateRangeAsync(DateTime from, DateTime to)
        => await _context.StockMovements
            .Where(m => m.Date >= from && m.Date <= to)
            .OrderByDescending(m => m.Date)
            .ToListAsync();

    public async Task AddAsync(StockMovement movement)
        => await _context.StockMovements.AddAsync(movement);

    public async Task<int> GetCountAsync()
        => await _context.StockMovements.CountAsync();
}
