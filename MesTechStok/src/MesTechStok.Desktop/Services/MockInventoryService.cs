using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreModels = MesTechStok.Core.Data.Models;

namespace MesTechStok.Desktop.Services
{
    [Obsolete("DI'da kullanilmiyor — gercek InventoryService (Core) aktif. Dalga 2'de kaldirilacak.")]
    public class MockInventoryService : IInventoryService
    {
        private readonly List<CoreModels.StockMovement> _stockMovements;

        private static readonly Guid _mockProductId1 = Guid.Parse("00000001-0000-0000-0000-000000000001");
        private static readonly Guid _mockProductId2 = Guid.Parse("00000002-0000-0000-0000-000000000002");
        private static readonly Guid _mockProductId4 = Guid.Parse("00000004-0000-0000-0000-000000000004");

        public MockInventoryService()
        {
            _stockMovements = new List<CoreModels.StockMovement>
            {
                new CoreModels.StockMovement
                {
                    Id = 1,
                    ProductId = _mockProductId1,
                    Quantity = 50,
                    NewStockLevel = 50,
                    MovementType = "IN",
                    Reason = "İlk stok girişi",
                    Date = DateTime.Now.AddDays(-10),
                    ProcessedBy = "System"
                },
                new CoreModels.StockMovement
                {
                    Id = 2,
                    ProductId = _mockProductId2,
                    Quantity = 30,
                    NewStockLevel = 30,
                    MovementType = "IN",
                    Reason = "İlk stok girişi",
                    Date = DateTime.Now.AddDays(-9),
                    ProcessedBy = "System"
                },
                new CoreModels.StockMovement
                {
                    Id = 3,
                    ProductId = _mockProductId1,
                    Quantity = -5,
                    NewStockLevel = 45,
                    MovementType = "OUT",
                    Reason = "Satış",
                    Date = DateTime.Now.AddDays(-2),
                    ProcessedBy = "User1"
                },
                new CoreModels.StockMovement
                {
                    Id = 4,
                    ProductId = _mockProductId4,
                    Quantity = -8,
                    NewStockLevel = 2,
                    MovementType = "OUT",
                    Reason = "Satış",
                    Date = DateTime.Now.AddDays(-1),
                    ProcessedBy = "User2"
                }
            };
        }

        public async Task<IEnumerable<CoreModels.StockMovement>> GetStockMovementsAsync()
        {
            await Task.Delay(100);
            return _stockMovements.OrderByDescending(sm => sm.Date).ToList();
        }

        public async Task<bool> AddStockMovementAsync(CoreModels.StockMovement movement)
        {
            await Task.Delay(50);

            movement.Id = _stockMovements.Max(sm => sm.Id) + 1;
            movement.Date = DateTime.Now;
            _stockMovements.Add(movement);

            return true;
        }

        public async Task<int> GetStockAsync(Guid productId)
        {
            await Task.Delay(30);

            var latestMovement = _stockMovements
                .Where(sm => sm.ProductId == productId)
                .OrderByDescending(sm => sm.Date)
                .FirstOrDefault();

            return latestMovement?.NewStockLevel ?? 0;
        }

        public async Task<bool> UpdateStockAsync(Guid productId, int newStock, string reason)
        {
            await Task.Delay(50);

            var Stock = await GetStockAsync(productId);
            var quantity = newStock - Stock;

            var movement = new CoreModels.StockMovement
            {
                Id = _stockMovements.Max(sm => sm.Id) + 1,
                ProductId = productId,
                Quantity = quantity,
                NewStockLevel = newStock,
                MovementType = quantity > 0 ? "IN" : "OUT",
                Reason = reason,
                Date = DateTime.Now,
                ProcessedBy = "System"
            };

            _stockMovements.Add(movement);
            return true;
        }

        public async Task<IEnumerable<CoreModels.StockMovement>> GetStockMovementsByProductAsync(Guid productId)
        {
            await Task.Delay(50);
            return _stockMovements
                .Where(sm => sm.ProductId == productId)
                .OrderByDescending(sm => sm.Date)
                .ToList();
        }

        public async Task<IEnumerable<CoreModels.StockMovement>> GetStockMovementsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            await Task.Delay(50);
            return _stockMovements
                .Where(sm => sm.Date >= startDate && sm.Date <= endDate)
                .OrderByDescending(sm => sm.Date)
                .ToList();
        }
    }
}