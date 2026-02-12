using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;

namespace MesTechStok.Desktop.Services
{
    public class SqlBackedInventoryService : IInventoryDataService
    {
        private readonly AppDbContext _db;

        public SqlBackedInventoryService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PagedResult<InventoryItem>> GetInventoryPagedAsync(int page, int pageSize, string? searchTerm, StockStatusFilter statusFilter, InventorySortOrder sortOrder)
        {
            var query = _db.Products.AsNoTracking().Where(p => p.IsActive);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Barcode.Contains(searchTerm) || (p.Category != null && p.Category.Name.Contains(searchTerm)));
            }

            query = statusFilter switch
            {
                StockStatusFilter.Normal => query.Where(p => p.Stock > p.MinimumStock),
                StockStatusFilter.Low => query.Where(p => p.Stock <= p.MinimumStock && p.Stock > 5),
                StockStatusFilter.Critical => query.Where(p => p.Stock <= 5 && p.Stock > 0),
                StockStatusFilter.OutOfStock => query.Where(p => p.Stock == 0),
                _ => query
            };

            query = sortOrder switch
            {
                InventorySortOrder.ProductName => query.OrderBy(p => p.Name),
                InventorySortOrder.ProductNameDesc => query.OrderByDescending(p => p.Name),
                InventorySortOrder.Stock => query.OrderBy(p => p.Stock),
                InventorySortOrder.StockDesc => query.OrderByDescending(p => p.Stock),
                InventorySortOrder.Category => query.OrderBy(p => p.Category!.Name).ThenBy(p => p.Name),
                InventorySortOrder.Location => query.OrderBy(p => p.Location).ThenBy(p => p.Name),
                InventorySortOrder.LastMovement => query.OrderBy(p => p.ModifiedDate),
                InventorySortOrder.LastMovementDesc => query.OrderByDescending(p => p.ModifiedDate),
                _ => query.OrderBy(p => p.Name)
            };

            var totalItems = await query.CountAsync();
            var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            var resultItems = items.Select(p => new InventoryItem
            {
                Id = p.Id,
                Barcode = p.Barcode,
                ProductName = p.Name,
                Category = p.Category?.Name ?? "",
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                Location = p.Location ?? "",
                Price = p.SalePrice,
                LastMovement = p.ModifiedDate ?? p.CreatedDate,
                RecentMovements = new List<StockMovement>()
            }).ToList();

            return new PagedResult<InventoryItem>
            {
                Items = resultItems,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            };
        }

        public async Task<InventoryItem?> GetInventoryByBarcodeAsync(string barcode)
        {
            var p = await _db.Products.AsNoTracking().Include(x => x.Category).FirstOrDefaultAsync(x => x.Barcode == barcode);
            if (p == null) return null;
            return new InventoryItem
            {
                Id = p.Id,
                Barcode = p.Barcode,
                ProductName = p.Name,
                Category = p.Category?.Name ?? "",
                Stock = p.Stock,
                MinimumStock = p.MinimumStock,
                Location = p.Location ?? "",
                Price = p.SalePrice,
                LastMovement = p.ModifiedDate ?? p.CreatedDate,
                RecentMovements = new List<StockMovement>()
            };
        }

        public async Task<bool> UpdateStockAsync(int inventoryId, int adjustment, string movementType, string? notes = null)
        {
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == inventoryId);
            if (product == null) return false;
            var previous = product.Stock;
            var next = Math.Max(0, previous + adjustment);
            product.Stock = next;
            product.ModifiedDate = DateTime.UtcNow;
            _db.StockMovements.Add(new MesTechStok.Core.Data.Models.StockMovement
            {
                ProductId = product.Id,
                MovementType = adjustment >= 0 ? "IN" : "OUT",
                Quantity = Math.Abs(adjustment),
                PreviousStock = previous,
                NewStock = next,
                Notes = notes,
                Date = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<InventoryStatistics> GetInventoryStatisticsAsync()
        {
            var totalValue = await _db.Products.Where(p => p.IsActive).SumAsync(p => p.Stock * p.SalePrice);
            var low = await _db.Products.CountAsync(p => p.IsActive && p.Stock <= p.MinimumStock && p.Stock > 0);
            var crit = await _db.Products.CountAsync(p => p.IsActive && p.Stock <= 5 && p.Stock > 0);
            var oos = await _db.Products.CountAsync(p => p.IsActive && p.Stock == 0);
            var today = await _db.StockMovements.CountAsync(m => m.Date.Date == DateTime.UtcNow.Date);
            return new InventoryStatistics
            {
                TotalInventoryValue = totalValue,
                LowStockCount = low,
                CriticalStockCount = crit,
                OutOfStockCount = oos,
                TodayMovements = today,
                StockAccuracy = 98.5m,
                TotalItems = await _db.Products.CountAsync(p => p.IsActive)
            };
        }

        public async Task<List<StockMovement>> GetRecentMovementsAsync(int count = 20)
        {
            var list = await _db.StockMovements.AsNoTracking().OrderByDescending(m => m.Date).Take(count).ToListAsync();
            return list.Select(m => new StockMovement
            {
                Id = m.Id,
                ProductName = m.Product?.Name ?? string.Empty,
                MovementType = m.MovementType,
                Quantity = m.Quantity,
                Date = m.Date,
                Notes = m.Notes ?? string.Empty,
                MovementIcon = m.Quantity >= 0 ? "➕" : "➖"
            }).ToList();
        }
    }
}


