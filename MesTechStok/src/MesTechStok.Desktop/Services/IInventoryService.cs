using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CoreModels = MesTechStok.Core.Data.Models;

namespace MesTechStok.Desktop.Services
{
    public interface IInventoryService
    {
        Task<IEnumerable<CoreModels.StockMovement>> GetStockMovementsAsync();
        Task<bool> AddStockMovementAsync(CoreModels.StockMovement movement);
        Task<int> GetStockAsync(Guid productId);
        Task<bool> UpdateStockAsync(Guid productId, int newStock, string reason);
        Task<IEnumerable<CoreModels.StockMovement>> GetStockMovementsByProductAsync(Guid productId);
        Task<IEnumerable<CoreModels.StockMovement>> GetStockMovementsByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}