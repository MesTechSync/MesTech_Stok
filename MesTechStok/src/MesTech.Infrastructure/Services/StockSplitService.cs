using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MesTech.Infrastructure.Services;

public sealed class StockSplitService : IStockSplitService
{
    private readonly AppDbContext _context;

    public StockSplitService(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<ProductWarehouseStock>> GetStockByProductAsync(
        Guid productId, CancellationToken ct = default)
        => await _context.ProductWarehouseStocks
            .Where(s => s.ProductId == productId)
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<int> GetTotalAvailableAsync(Guid productId, CancellationToken ct = default)
        => await _context.ProductWarehouseStocks
            .Where(s => s.ProductId == productId)
            .SumAsync(s => s.AvailableQuantity, ct).ConfigureAwait(false);

    public async Task UpdateFulfillmentStockAsync(
        Guid productId,
        FulfillmentCenter center,
        int quantity,
        CancellationToken ct = default)
    {
        var centerName = center.ToString();
        var existing = await _context.ProductWarehouseStocks
            .FirstOrDefaultAsync(
                s => s.ProductId == productId && s.FulfillmentCenter == centerName, ct).ConfigureAwait(false);

        if (existing is not null)
        {
            existing.UpdateStock(quantity, existing.ReservedQuantity, existing.InboundQuantity);
        }
        else
        {
            var stock = ProductWarehouseStock.Create(productId, Guid.NewGuid(), centerName);
            stock.UpdateStock(quantity, 0, 0);
            await _context.ProductWarehouseStocks.AddAsync(stock, ct).ConfigureAwait(false);
        }

        await _context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<Dictionary<Guid, int>> GetTotalAvailableBulkAsync(
        IEnumerable<Guid> productIds, CancellationToken ct = default)
    {
        var ids = productIds.ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, int>();

        return await _context.ProductWarehouseStocks
            .Where(s => ids.Contains(s.ProductId))
            .GroupBy(s => s.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(s => s.AvailableQuantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Total, ct)
            .ConfigureAwait(false);
    }
}
