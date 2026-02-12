using System.Collections.Generic;
using System.Threading.Tasks;

namespace MesTechStok.Desktop.Services
{
    public interface IInventoryDataService
    {
        Task<PagedResult<InventoryItem>> GetInventoryPagedAsync(int page, int pageSize, string? searchTerm, StockStatusFilter statusFilter, InventorySortOrder sortOrder);
        Task<InventoryItem?> GetInventoryByBarcodeAsync(string barcode);
        Task<bool> UpdateStockAsync(int inventoryId, int adjustment, string movementType, string? notes = null);
        Task<InventoryStatistics> GetInventoryStatisticsAsync();
        Task<List<StockMovement>> GetRecentMovementsAsync(int count = 20);
    }
}


