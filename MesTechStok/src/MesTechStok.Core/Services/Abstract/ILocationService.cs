using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract
{
    /// <summary>
    /// Stok Yerleşim Sistemi için konum yönetimi servisi
    /// </summary>
    public interface ILocationService
    {
        // Konum Yönetimi
        Task<WarehouseBin> GetBinByCodeAsync(string binCode);
        Task<ProductLocation> PlaceProductAsync(int productId, int binId, int quantity, string notes);
        Task<ProductLocation> MoveProductAsync(int productId, int fromBinId, int toBinId, int quantity);
        Task<ProductLocation> RemoveProductAsync(int productId, int binId, int quantity);

        // Konum Arama
        Task<List<WarehouseBin>> FindAvailableBinsAsync(Product product, int quantity);
        Task<List<WarehouseBin>> FindBinsByProductAsync(int productId);
        Task<List<ProductLocation>> GetProductLocationsAsync(int productId);

        // Optimizasyon
        Task<WarehouseBin> GetOptimalBinAsync(Product product, int quantity);
        Task<List<WarehouseBin>> GetNearbyBinsAsync(int binId, int radius);

        // Raporlama
        Task<LocationReport> GetLocationReportAsync(int warehouseId);
        Task<BinUtilizationReport> GetBinUtilizationReportAsync(int warehouseId);

        // Depo Organizasyonu
        Task<List<WarehouseZone>> GetWarehouseZonesAsync(int warehouseId);
        Task<List<WarehouseRack>> GetRacksByZoneAsync(int zoneId);
        Task<List<WarehouseShelf>> GetShelvesByRackAsync(int rackId);
        Task<List<WarehouseBin>> GetBinsByShelfAsync(int shelfId);

        // Gelişmiş Konum Yönetimi
        Task<WarehouseZone> CreateZoneAsync(WarehouseZone zone);
        Task<WarehouseRack> CreateRackAsync(WarehouseRack rack);
        Task<WarehouseShelf> CreateShelfAsync(WarehouseShelf shelf);
        Task<WarehouseBin> CreateBinAsync(WarehouseBin bin);

        // Konum Güncelleme
        Task<bool> UpdateZoneAsync(WarehouseZone zone);
        Task<bool> UpdateRackAsync(WarehouseRack rack);
        Task<bool> UpdateShelfAsync(WarehouseShelf shelf);
        Task<bool> UpdateBinAsync(WarehouseBin bin);

        // Konum Silme (Soft Delete)
        Task<bool> DeactivateZoneAsync(int zoneId);
        Task<bool> DeactivateRackAsync(int rackId);
        Task<bool> DeactivateShelfAsync(int shelfId);
        Task<bool> DeactivateBinAsync(int binId);

        // Akıllı Konum Önerisi
        Task<List<SmartLocationSuggestion>> GetSmartLocationSuggestionsAsync(int productId, int quantity);
        Task<LocationEfficiencyScore> CalculateLocationEfficiencyAsync(int binId);

        // Toplu İşlemler
        Task<bool> BulkMoveProductsAsync(List<BulkMoveRequest> requests);
        Task<bool> BulkPlaceProductsAsync(List<BulkPlaceRequest> requests);

        // Konum Geçmişi
        Task<List<LocationMovement>> GetLocationHistoryAsync(int binId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<List<LocationMovement>> GetProductMovementHistoryAsync(int productId, DateTime? fromDate = null, DateTime? toDate = null);
    }

    /// <summary>
    /// Konum raporu modeli
    /// </summary>
    public class LocationReport
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        // Genel İstatistikler
        public int TotalZones { get; set; }
        public int TotalRacks { get; set; }
        public int TotalShelves { get; set; }
        public int TotalBins { get; set; }

        // Doluluk Oranları
        public decimal ZoneUtilization { get; set; }
        public decimal RackUtilization { get; set; }
        public decimal ShelfUtilization { get; set; }
        public decimal BinUtilization { get; set; }

        // Konum Analizi
        public List<ZoneUtilization> ZoneUtilizations { get; set; } = new();
        public List<RackUtilization> RackUtilizations { get; set; } = new();
        public List<BinUtilization> BinUtilizations { get; set; } = new();

        // Optimizasyon Önerileri
        public List<OptimizationSuggestion> Suggestions { get; set; } = new();
    }

    /// <summary>
    /// Bölüm doluluk raporu
    /// </summary>
    public class ZoneUtilization
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public decimal UtilizationPercentage { get; set; }
        public int TotalBins { get; set; }
        public int OccupiedBins { get; set; }
        public decimal TotalArea { get; set; }
        public decimal UsedArea { get; set; }
    }

    /// <summary>
    /// Raf doluluk raporu
    /// </summary>
    public class RackUtilization
    {
        public int RackId { get; set; }
        public string RackName { get; set; } = string.Empty;
        public decimal UtilizationPercentage { get; set; }
        public int TotalShelves { get; set; }
        public int OccupiedShelves { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal UsedWeight { get; set; }
    }

    /// <summary>
    /// Göz doluluk raporu
    /// </summary>
    public class BinUtilization
    {
        public int BinId { get; set; }
        public string BinName { get; set; } = string.Empty;
        public decimal UtilizationPercentage { get; set; }
        public int TotalProducts { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal UsedVolume { get; set; }
        public decimal TotalWeight { get; set; }
        public decimal UsedWeight { get; set; }
    }

    /// <summary>
    /// Optimizasyon önerisi
    /// </summary>
    public class OptimizationSuggestion
    {
        public string Type { get; set; } = string.Empty; // "REORGANIZE", "EXPAND", "CONSOLIDATE"
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PotentialSavings { get; set; }
        public int EstimatedTime { get; set; } // dakika
        public string Priority { get; set; } = string.Empty; // "LOW", "MEDIUM", "HIGH", "CRITICAL"
    }

    /// <summary>
    /// Göz kullanım raporu (placeholder)
    /// </summary>
    public class BinUtilizationReport
    {
        // TODO: Implement
    }

    /// <summary>
    /// Akıllı konum önerisi
    /// </summary>
    public class SmartLocationSuggestion
    {
        public int BinId { get; set; }
        public string BinCode { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
        public string ShelfName { get; set; } = string.Empty;
        public decimal MatchScore { get; set; } // 0-100 arası uyum skoru
        public string Reason { get; set; } = string.Empty; // Öneri nedeni
        public List<string> Advantages { get; set; } = new(); // Avantajlar
        public List<string> Disadvantages { get; set; } = new(); // Dezavantajlar
    }

    /// <summary>
    /// Konum verimlilik skoru
    /// </summary>
    public class LocationEfficiencyScore
    {
        public int BinId { get; set; }
        public string BinCode { get; set; } = string.Empty;
        public decimal OverallScore { get; set; } // 0-100 arası genel skor
        public decimal SpaceEfficiency { get; set; } // Alan kullanım verimliliği
        public decimal AccessibilityScore { get; set; } // Erişim kolaylığı
        public decimal CategoryProximityScore { get; set; } // Kategori yakınlığı
        public decimal MovementFrequencyScore { get; set; } // Hareket sıklığı
        public List<string> ImprovementSuggestions { get; set; } = new(); // İyileştirme önerileri
    }

    /// <summary>
    /// Toplu taşıma isteği
    /// </summary>
    public class BulkMoveRequest
    {
        public int ProductId { get; set; }
        public int FromBinId { get; set; }
        public int ToBinId { get; set; }
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime RequestedDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Toplu yerleştirme isteği
    /// </summary>
    public class BulkPlaceRequest
    {
        public int ProductId { get; set; }
        public int BinId { get; set; }
        public int Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime RequestedDate { get; set; } = DateTime.Now;
    }
}
