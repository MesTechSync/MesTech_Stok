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
        Task<int> GetStockAsync(int productId);
        Task<bool> UpdateStockAsync(int productId, int newStock, string reason);
        Task<IEnumerable<CoreModels.StockMovement>> GetStockMovementsByProductAsync(int productId);
        Task<IEnumerable<CoreModels.StockMovement>> GetStockMovementsByDateRangeAsync(DateTime startDate, DateTime endDate);
    }
}