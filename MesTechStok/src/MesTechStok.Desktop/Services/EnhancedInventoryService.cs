using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Services
{
    public class EnhancedInventoryService
    {
        private readonly List<InventoryItem> _allInventory;
        private readonly Random _random = new();

        public EnhancedInventoryService()
        {
            _allInventory = GenerateInventoryData();
        }

        #region Public Methods

        public async Task<PagedResult<InventoryItem>> GetInventoryPagedAsync(
            int page = 1,
            int pageSize = 50,
            string? searchTerm = null,
            StockStatusFilter statusFilter = StockStatusFilter.All,
            InventorySortOrder sortOrder = InventorySortOrder.ProductName)
        {
            await Task.Delay(30); // Simulate network delay

            var filteredInventory = FilterInventory(searchTerm, statusFilter);
            var sortedInventory = SortInventory(filteredInventory, sortOrder);

            var totalItems = sortedInventory.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var items = sortedInventory
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<InventoryItem>
            {
                Items = items,
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            };
        }

        public async Task<InventoryItem?> GetInventoryByBarcodeAsync(string barcode)
        {
            await Task.Delay(25);
            return _allInventory.FirstOrDefault(i => i.Barcode == barcode);
        }

        public async Task<bool> UpdateStockAsync(int inventoryId, int adjustment, string movementType, string? notes = null)
        {
            await Task.Delay(50);

            var item = _allInventory.FirstOrDefault(i => i.Id == inventoryId);
            if (item == null) return false;

            var newStock = Math.Max(0, item.Stock + adjustment);
            item.Stock = newStock;
            item.LastMovement = DateTime.Now;

            // Add to movement history
            item.RecentMovements.Insert(0, new StockMovement
            {
                Id = _random.Next(1000, 9999),
                MovementType = movementType,
                Quantity = adjustment,
                Date = DateTime.Now,
                Notes = notes ?? "",
                MovementIcon = adjustment > 0 ? "➕" : "➖"
            });

            // Keep only last 10 movements
            if (item.RecentMovements.Count > 10)
            {
                item.RecentMovements.RemoveAt(item.RecentMovements.Count - 1);
            }

            return true;
        }

        public async Task<InventoryStatistics> GetInventoryStatisticsAsync()
        {
            await Task.Delay(75);

            var totalValue = _allInventory.Sum(i => i.Stock * i.Price);
            var lowStockCount = _allInventory.Count(i => i.Stock <= i.MinimumStock && i.Stock > 0);
            var criticalStockCount = _allInventory.Count(i => i.Stock <= 5);
            var outOfStockCount = _allInventory.Count(i => i.Stock == 0);
            var todayMovements = _allInventory.SelectMany(i => i.RecentMovements)
                .Count(m => m.Date.Date == DateTime.Today);

            return new InventoryStatistics
            {
                TotalInventoryValue = totalValue,
                LowStockCount = lowStockCount,
                CriticalStockCount = criticalStockCount,
                OutOfStockCount = outOfStockCount,
                TodayMovements = todayMovements,
                StockAccuracy = 98.5m, // Simulated
                TotalItems = _allInventory.Count
            };
        }

        public async Task<List<StockMovement>> GetRecentMovementsAsync(int count = 20)
        {
            await Task.Delay(40);

            return _allInventory
                .SelectMany(i => i.RecentMovements)
                .OrderByDescending(m => m.Date)
                .Take(count)
                .ToList();
        }

        #endregion

        #region Private Methods

        private IEnumerable<InventoryItem> FilterInventory(string? searchTerm, StockStatusFilter statusFilter)
        {
            var inventory = _allInventory.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                inventory = inventory.Where(i =>
                    i.ProductName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    i.Barcode.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    i.Category.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    i.Location.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            inventory = statusFilter switch
            {
                StockStatusFilter.Normal => inventory.Where(i => i.Stock > i.MinimumStock),
                StockStatusFilter.Low => inventory.Where(i => i.Stock <= i.MinimumStock && i.Stock > 5),
                StockStatusFilter.Critical => inventory.Where(i => i.Stock <= 5 && i.Stock > 0),
                StockStatusFilter.OutOfStock => inventory.Where(i => i.Stock == 0),
                _ => inventory
            };

            return inventory;
        }

        private IEnumerable<InventoryItem> SortInventory(IEnumerable<InventoryItem> inventory, InventorySortOrder sortOrder)
        {
            return sortOrder switch
            {
                InventorySortOrder.ProductName => inventory.OrderBy(i => i.ProductName),
                InventorySortOrder.ProductNameDesc => inventory.OrderByDescending(i => i.ProductName),
                InventorySortOrder.Stock => inventory.OrderBy(i => i.Stock),
                InventorySortOrder.StockDesc => inventory.OrderByDescending(i => i.Stock),
                InventorySortOrder.Category => inventory.OrderBy(i => i.Category).ThenBy(i => i.ProductName),
                InventorySortOrder.Location => inventory.OrderBy(i => i.Location).ThenBy(i => i.ProductName),
                InventorySortOrder.LastMovement => inventory.OrderBy(i => i.LastMovement),
                InventorySortOrder.LastMovementDesc => inventory.OrderByDescending(i => i.LastMovement),
                _ => inventory.OrderBy(i => i.ProductName)
            };
        }

        private List<InventoryItem> GenerateInventoryData()
        {
            var inventory = new List<InventoryItem>();
            var categories = new[] { "Elektronik", "İçecek", "Atıştırmalık", "Kozmetik", "Spor", "Gıda", "Oyuncak", "Ev Gereçleri", "Kırtasiye", "Sağlık" };
            var locations = new[] { "A-01", "A-02", "A-03", "B-01", "B-02", "B-03", "C-01", "C-02", "DEPO-1", "DEPO-2", "DEPO-3", "VİTRİN-A", "VİTRİN-B" };

            var productNames = new Dictionary<string, string[]>
            {
                ["Elektronik"] = new[] { "Samsung Galaxy S24", "iPhone 15 Pro", "MacBook Pro 14\"", "Sony WH-1000XM5", "iPad Air", "Apple Watch Series 9", "Dell XPS 13", "AirPods Pro", "PlayStation 5", "Nintendo Switch", "MSI Gaming Laptop", "Asus ROG Phone", "LG OLED TV 55\"", "Canon EOS R6", "Bose QuietComfort" },
                ["İçecek"] = new[] { "Coca Cola 330ml", "Fanta Portakal 330ml", "Sprite 330ml", "Monster Energy 500ml", "Red Bull 250ml", "Çay 100 Adet", "Türk Kahvesi 100gr", "Su 1.5L", "Ayran 200ml", "Meyve Suyu 1L", "Ice Tea 500ml", "Pepsi 330ml", "7UP 330ml", "Gazoz 200ml", "Limonata 250ml" },
                ["Atıştırmalık"] = new[] { "Doritos Nacho 150g", "Pringles Klasik", "Haribo Jöle", "Ülker Çikolata", "Biskuit 200g", "Fındık 500g", "Çerez Karışımı", "Kuru Üzüm 250g", "Badem 300g", "Fıstık 400g", "Crackers 250g", "Popcorn 100g", "Gofret 45g", "Çubuk Kraker", "Simit 6'lı" },
                ["Kozmetik"] = new[] { "Nivea Krem 100ml", "L'Oreal Şampuan", "Johnson's Baby Şampuan", "Garnier Saç Maskesi", "Maybelline Maskara", "Ruj Kırmızı", "Parfüm 50ml", "Nemlendirici", "Güneş Kremi", "Yüz Temizleyici", "Saç Kremi 200ml", "Duş Jeli 250ml", "Deodorant Spray", "Tırnak Cilası", "Makyaj Bazı" },
                ["Spor"] = new[] { "Adidas Spor Ayakkabı", "Nike Air Max", "Protein Tozu 1kg", "Yoga Matı", "Dumbbell 5kg", "Spor Çantası", "Koşu Bandı", "Fitness Eldiveni", "Su Matarası", "Spor T-Shirt", "Futbol Topu", "Tenis Raketi", "Basketbol", "Voleybol", "Ping Pong Raketi" }
            };

            int idCounter = 1;

            // Her kategoriden ürünler ekle
            foreach (var category in categories)
            {
                var names = productNames.ContainsKey(category) ? productNames[category] : new[] { $"{category} Ürünü" };

                foreach (var name in names)
                {
                    var Stock = _random.Next(0, 150);
                    var minimumStock = _random.Next(5, 25);

                    var inventoryItem = new InventoryItem
                    {
                        Id = idCounter++,
                        Barcode = GenerateBarcode(),
                        ProductName = name,
                        Category = category,
                        Stock = Stock,
                        MinimumStock = minimumStock,
                        Location = locations[_random.Next(locations.Length)],
                        Price = GenerateRandomPrice(category),
                        LastMovement = DateTime.Now.AddDays(-_random.Next(0, 30)),
                        RecentMovements = GenerateRecentMovements(name, 3)
                    };

                    inventory.Add(inventoryItem);
                }
            }

            // Ekstra test ürünleri ekle
            for (int i = 0; i < 100; i++)
            {
                var category = categories[i % categories.Length];
                var names = productNames.ContainsKey(category) ? productNames[category] : new[] { $"{category} Ürünü" };
                var baseName = names[i % names.Length];

                var Stock = _random.Next(0, 200);
                var minimumStock = _random.Next(5, 30);

                var inventoryItem = new InventoryItem
                {
                    Id = idCounter++,
                    Barcode = GenerateBarcode(),
                    ProductName = $"{baseName} - Model {i + 1}",
                    Category = category,
                    Stock = Stock,
                    MinimumStock = minimumStock,
                    Location = locations[_random.Next(locations.Length)],
                    Price = GenerateRandomPrice(category),
                    LastMovement = DateTime.Now.AddDays(-_random.Next(0, 60)),
                    RecentMovements = GenerateRecentMovements($"{baseName} - Model {i + 1}", _random.Next(1, 8))
                };

                inventory.Add(inventoryItem);
            }

            return inventory;
        }

        private List<StockMovement> GenerateRecentMovements(string productName, int count)
        {
            var movements = new List<StockMovement>();
            var movementTypes = new[] { "Giriş", "Çıkış", "Transfer", "Sayım", "İade", "Satış" };
            var icons = new[] { "➕", "➖", "🔄", "📊", "↩️", "💰" };

            for (int i = 0; i < count; i++)
            {
                var typeIndex = _random.Next(movementTypes.Length);
                var isPositive = typeIndex == 0 || typeIndex == 4; // Giriş veya İade

                movements.Add(new StockMovement
                {
                    Id = _random.Next(1000, 9999),
                    MovementType = movementTypes[typeIndex],
                    Quantity = isPositive ? _random.Next(1, 50) : -_random.Next(1, 30),
                    Date = DateTime.Now.AddDays(-_random.Next(0, 14)),
                    Notes = $"Sistem kaydı - {movementTypes[typeIndex]}",
                    MovementIcon = icons[typeIndex]
                });
            }

            return movements.OrderByDescending(m => m.Date).ToList();
        }

        private string GenerateBarcode()
        {
            return _random.NextInt64(1000000000000, 9999999999999).ToString();
        }

        private decimal GenerateRandomPrice(string category)
        {
            return category switch
            {
                "Elektronik" => (decimal)_random.Next(500, 80000),
                "İçecek" => (decimal)(_random.Next(300, 2000) / 100.0),
                "Atıştırmalık" => (decimal)(_random.Next(500, 3000) / 100.0),
                "Kozmetik" => (decimal)(_random.Next(1000, 10000) / 100.0),
                "Spor" => (decimal)_random.Next(5000, 200000) / 100,
                "Gıda" => (decimal)(_random.Next(1000, 15000) / 100.0),
                "Oyuncak" => (decimal)(_random.Next(2000, 50000) / 100.0),
                "Ev Gereçleri" => (decimal)(_random.Next(5000, 100000) / 100.0),
                "Kırtasiye" => (decimal)(_random.Next(200, 5000) / 100.0),
                "Sağlık" => (decimal)(_random.Next(1000, 25000) / 100.0),
                _ => (decimal)(_random.Next(1000, 10000) / 100.0)
            };
        }

        /// <summary>
        /// İstatistik bilgilerini hesapla
        /// </summary>
        private InventoryStatistics CalculateStatistics()
        {
            return new InventoryStatistics
            {
                TotalItems = _allInventory.Count,
                TotalInventoryValue = _allInventory.Sum(i => i.Stock * i.Price),
                LowStockCount = _allInventory.Count(i => i.Stock <= i.MinimumStock && i.Stock > 0),
                OutOfStockCount = _allInventory.Count(i => i.Stock == 0),
                StockAccuracy = 100m // placeholder metric
            };
        }

        #endregion
    }

    #region Supporting Classes

    public class InventoryItem
    {
        public int Id { get; set; }
        public string Barcode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Category { get; set; } = "";
        public int Stock { get; set; }
        public int MinimumStock { get; set; }
        public string Location { get; set; } = "";
        public decimal Price { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime LastMovement { get; set; }
        public List<StockMovement> RecentMovements { get; set; } = new();

        public StockStatus StockStatus
        {
            get
            {
                if (Stock == 0) return Models.StockStatus.OutOfStock;
                if (Stock <= 5) return Models.StockStatus.Critical;
                if (Stock <= MinimumStock) return Models.StockStatus.Low;
                return Models.StockStatus.Normal;
            }
        }

        public string StatusIcon
        {
            get
            {
                return StockStatus switch
                {
                    Models.StockStatus.OutOfStock => "❌",
                    Models.StockStatus.Critical => "🚨",
                    Models.StockStatus.Low => "⚠️",
                    _ => "✅"
                };
            }
        }

        public decimal TotalValue => Stock * Price;
    }

    public class StockMovement
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = "";
        public string MovementType { get; set; } = "";
        public int Quantity { get; set; }
        public DateTime Date { get; set; }
        public string Notes { get; set; } = "";
        public string MovementIcon { get; set; } = "📦";
        public string MovementDescription => $"{MovementType} - {Notes}";
    }

    public class InventoryStatistics
    {
        public decimal TotalInventoryValue { get; set; }
        public int LowStockCount { get; set; }
        public int CriticalStockCount { get; set; }
        public int OutOfStockCount { get; set; }
        public int TodayMovements { get; set; }
        public decimal StockAccuracy { get; set; }
        public int TotalItems { get; set; }
    }

    public enum StockStatusFilter
    {
        All,
        Normal,
        Low,
        Critical,
        OutOfStock
    }

    public enum InventorySortOrder
    {
        ProductName,
        ProductNameDesc,
        Stock,
        StockDesc,
        Category,
        Location,
        LastMovement,
        LastMovementDesc
    }

    #endregion
}