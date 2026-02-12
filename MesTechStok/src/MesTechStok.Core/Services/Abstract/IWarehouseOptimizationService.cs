using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract
{
    /// <summary>
    /// Depo optimizasyon servisi - Akıllı konum önerileri ve verimlilik analizi
    /// </summary>
    public interface IWarehouseOptimizationService
    {
        // Akıllı Konum Önerileri
        Task<List<SmartLocationSuggestion>> GetOptimalLocationSuggestionsAsync(int productId, int quantity);
        Task<List<SmartLocationSuggestion>> GetBulkLocationSuggestionsAsync(List<BulkLocationRequest> requests);
        Task<LocationOptimizationScore> CalculateLocationOptimizationScoreAsync(int binId);

        // Depo Verimlilik Analizi
        Task<WarehouseEfficiencyReport> GetWarehouseEfficiencyReportAsync(int warehouseId);
        Task<ZoneEfficiencyReport> GetZoneEfficiencyReportAsync(int zoneId);
        Task<RackEfficiencyReport> GetRackEfficiencyReportAsync(int rackId);

        // Optimizasyon Önerileri
        Task<List<OptimizationRecommendation>> GetOptimizationRecommendationsAsync(int warehouseId);
        Task<OptimizationImpact> CalculateOptimizationImpactAsync(OptimizationAction action);
        Task<bool> ApplyOptimizationActionAsync(OptimizationAction action);

        // Depo Kapasite Planlaması
        Task<CapacityPlanningReport> GetCapacityPlanningReportAsync(int warehouseId);
        Task<List<CapacityAlert>> GetCapacityAlertsAsync(int warehouseId);
        Task<CapacityForecast> GetCapacityForecastAsync(int warehouseId, int monthsAhead);

        // Konum Analizi
        Task<LocationHeatmap> GetLocationHeatmapAsync(int warehouseId);
        Task<MovementPatternAnalysis> GetMovementPatternAnalysisAsync(int warehouseId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<SpaceUtilizationTrend> GetSpaceUtilizationTrendAsync(int warehouseId, int monthsBack = 12);
    }

    /// <summary>
    /// Toplu konum isteği
    /// </summary>
    public class BulkLocationRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Priority { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH, CRITICAL
        public List<string> PreferredZones { get; set; } = new();
        public List<string> ExcludedZones { get; set; } = new();
        public bool RequireClimateControl { get; set; } = false;
        public decimal? MaxWeight { get; set; }
        public decimal? MaxVolume { get; set; }
    }

    /// <summary>
    /// Konum optimizasyon skoru
    /// </summary>
    public class LocationOptimizationScore
    {
        public int BinId { get; set; }
        public string BinCode { get; set; } = string.Empty;
        public decimal OverallScore { get; set; } // 0-100 arası genel skor
        public decimal SpaceEfficiency { get; set; } // Alan kullanım verimliliği
        public decimal AccessibilityScore { get; set; } // Erişim kolaylığı
        public decimal CategoryProximityScore { get; set; } // Kategori yakınlığı
        public decimal MovementFrequencyScore { get; set; } // Hareket sıklığı
        public decimal WeightDistributionScore { get; set; } // Ağırlık dağılımı
        public decimal ClimateCompatibilityScore { get; set; } // İklim uyumluluğu
        public List<string> ImprovementSuggestions { get; set; } = new();
        public Dictionary<string, decimal> DetailedScores { get; set; } = new();
    }

    /// <summary>
    /// Depo verimlilik raporu
    /// </summary>
    public class WarehouseEfficiencyReport
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; } = DateTime.Now;
        public decimal OverallEfficiency { get; set; } // 0-100 arası genel verimlilik
        public decimal SpaceUtilization { get; set; } // Alan kullanım oranı
        public decimal LaborEfficiency { get; set; } // İş gücü verimliliği
        public decimal EquipmentEfficiency { get; set; } // Ekipman verimliliği
        public decimal OrderFulfillmentRate { get; set; } // Sipariş karşılama oranı
        public decimal InventoryTurnoverRate { get; set; } // Stok devir hızı
        public List<ZoneEfficiencyReport> ZoneReports { get; set; } = new();
        public List<EfficiencyTrend> EfficiencyTrends { get; set; } = new();
        public List<PerformanceMetric> KeyMetrics { get; set; } = new();
    }

    /// <summary>
    /// Verimlilik trendi
    /// </summary>
    public class EfficiencyTrend
    {
        public string Metric { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty; // INCREASING, DECREASING, STABLE
        public decimal ChangeRate { get; set; }
        public DateTime TrendStartDate { get; set; }
        public DateTime TrendEndDate { get; set; }
        public string Confidence { get; set; } = "MEDIUM";
    }

    /// <summary>
    /// Performans metriği
    /// </summary>
    public class PerformanceMetric
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public decimal Target { get; set; }
        public decimal Threshold { get; set; }
        public string Status { get; set; } = string.Empty; // GOOD, WARNING, CRITICAL
        public string Trend { get; set; } = string.Empty; // UP, DOWN, SAME
    }

    /// <summary>
    /// Bölüm verimlilik raporu
    /// </summary>
    public class ZoneEfficiencyReport
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public decimal Efficiency { get; set; }
        public decimal SpaceUtilization { get; set; }
        public int TotalRacks { get; set; }
        public int ActiveRacks { get; set; }
        public decimal AverageRackEfficiency { get; set; }
        public List<RackEfficiencyReport> RackReports { get; set; } = new();
    }

    /// <summary>
    /// Raf verimlilik raporu
    /// </summary>
    public class RackEfficiencyReport
    {
        public int RackId { get; set; }
        public string RackName { get; set; } = string.Empty;
        public decimal Efficiency { get; set; }
        public decimal SpaceUtilization { get; set; }
        public decimal WeightUtilization { get; set; }
        public int TotalShelves { get; set; }
        public int ActiveShelves { get; set; }
        public decimal AverageShelfEfficiency { get; set; }
    }

    /// <summary>
    /// Optimizasyon önerisi
    /// </summary>
    public class OptimizationRecommendation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty; // REORGANIZE, EXPAND, CONSOLIDATE, RELOCATE
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH, CRITICAL
        public decimal PotentialSavings { get; set; } // Potansiyel tasarruf (TL)
        public int EstimatedTime { get; set; } // Tahmini süre (dakika)
        public decimal EstimatedCost { get; set; } // Tahmini maliyet (TL)
        public decimal ROI { get; set; } // Yatırım getirisi
        public List<string> AffectedAreas { get; set; } = new();
        public List<string> Prerequisites { get; set; } = new();
        public List<string> Risks { get; set; } = new();
        public Dictionary<string, object> ImplementationDetails { get; set; } = new();
    }

    /// <summary>
    /// Optimizasyon aksiyonu
    /// </summary>
    public class OptimizationAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<OptimizationStep> Steps { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Optimizasyon adımı
    /// </summary>
    public class OptimizationStep
    {
        public int Order { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool IsRequired { get; set; } = true;
        public int EstimatedDuration { get; set; } // dakika
    }

    /// <summary>
    /// Optimizasyon etkisi
    /// </summary>
    public class OptimizationImpact
    {
        public string ActionId { get; set; } = string.Empty;
        public decimal SpaceEfficiencyImprovement { get; set; }
        public decimal LaborEfficiencyImprovement { get; set; }
        public decimal CostSavings { get; set; }
        public decimal TimeSavings { get; set; } // saat
        public int RiskLevel { get; set; } // 1-5 arası risk seviyesi
        public List<string> Benefits { get; set; } = new();
        public List<string> Drawbacks { get; set; } = new();
        public Dictionary<string, decimal> DetailedImpact { get; set; } = new();
    }

    /// <summary>
    /// Kapasite planlama raporu
    /// </summary>
    public class CapacityPlanningReport
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; } = DateTime.Now;
        public decimal CurrentUtilization { get; set; }
        public decimal ProjectedUtilization { get; set; } // 6 ay sonra
        public decimal PeakUtilization { get; set; } // En yüksek kullanım
        public DateTime? PeakDate { get; set; }
        public List<CapacityAlert> Alerts { get; set; } = new();
        public List<CapacityForecast> Forecasts { get; set; } = new();
        public List<CapacityRecommendation> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Kapasite uyarısı
    /// </summary>
    public class CapacityAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = string.Empty; // CAPACITY_WARNING, CAPACITY_CRITICAL, OVERFLOW
        public string Message { get; set; } = string.Empty;
        public string Severity { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH, CRITICAL
        public DateTime AlertDate { get; set; } = DateTime.Now;
        public DateTime? ExpectedDate { get; set; }
        public List<string> AffectedAreas { get; set; } = new();
        public List<string> SuggestedActions { get; set; } = new();
    }

    /// <summary>
    /// Kapasite tahmini
    /// </summary>
    public class CapacityForecast
    {
        public DateTime ForecastDate { get; set; }
        public decimal PredictedUtilization { get; set; }
        public decimal ConfidenceLevel { get; set; } // 0-100 arası güven seviyesi
        public List<CapacityFactor> ContributingFactors { get; set; } = new();
        public string Trend { get; set; } = string.Empty; // INCREASING, DECREASING, STABLE
    }

    /// <summary>
    /// Kapasite faktörü
    /// </summary>
    public class CapacityFactor
    {
        public string Name { get; set; } = string.Empty;
        public decimal Impact { get; set; } // -100 ile +100 arası etki
        public string Description { get; set; } = string.Empty;
        public bool IsControllable { get; set; }
    }

    /// <summary>
    /// Kapasite önerisi
    /// </summary>
    public class CapacityRecommendation
    {
        public string Type { get; set; } = string.Empty; // EXPAND, OPTIMIZE, REDISTRIBUTE
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ExpectedImprovement { get; set; }
        public int ImplementationTime { get; set; } // gün
        public decimal EstimatedCost { get; set; }
        public string Priority { get; set; } = "MEDIUM";
    }

    /// <summary>
    /// Konum ısı haritası
    /// </summary>
    public class LocationHeatmap
    {
        public int WarehouseId { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public List<HeatmapCell> Cells { get; set; } = new();
        public HeatmapLegend Legend { get; set; } = new();
    }

    /// <summary>
    /// Isı haritası hücresi
    /// </summary>
    public class HeatmapCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public decimal Value { get; set; } // 0-100 arası değer
        public string Color { get; set; } = string.Empty; // Hex renk kodu
        public string BinCode { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public string RackName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Isı haritası açıklaması
    /// </summary>
    public class HeatmapLegend
    {
        public decimal MinValue { get; set; }
        public decimal MaxValue { get; set; }
        public List<LegendItem> Items { get; set; } = new();
    }

    /// <summary>
    /// Açıklama öğesi
    /// </summary>
    public class LegendItem
    {
        public decimal Value { get; set; }
        public string Color { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
    /// Hareket deseni analizi
    /// </summary>
    public class MovementPatternAnalysis
    {
        public int WarehouseId { get; set; }
        public DateTime AnalysisDate { get; set; } = DateTime.Now;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public List<MovementPattern> Patterns { get; set; } = new();
        public List<MovementTrend> Trends { get; set; } = new();
        public MovementStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// Hareket deseni
    /// </summary>
    public class MovementPattern
    {
        public string PatternType { get; set; } = string.Empty; // SEASONAL, WEEKLY, DAILY
        public string Description { get; set; } = string.Empty;
        public decimal Frequency { get; set; } // 0-100 arası sıklık
        public List<string> AffectedProducts { get; set; } = new();
        public List<string> AffectedAreas { get; set; } = new();
        public Dictionary<string, object> PatternData { get; set; } = new();
    }

    /// <summary>
    /// Hareket trendi
    /// </summary>
    public class MovementTrend
    {
        public string Metric { get; set; } = string.Empty; // VOLUME, FREQUENCY, DISTANCE
        public string Direction { get; set; } = string.Empty; // INCREASING, DECREASING, STABLE
        public decimal ChangeRate { get; set; } // Yüzde değişim
        public DateTime TrendStartDate { get; set; }
        public DateTime TrendEndDate { get; set; }
        public string Confidence { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH
    }

    /// <summary>
    /// Hareket istatistikleri
    /// </summary>
    public class MovementStatistics
    {
        public int TotalMovements { get; set; }
        public decimal AverageMovementDistance { get; set; } // metre
        public decimal AverageMovementTime { get; set; } // dakika
        public decimal TotalMovementVolume { get; set; } // m³
        public decimal TotalMovementWeight { get; set; } // kg
        public List<string> TopMovedProducts { get; set; } = new();
        public List<string> TopSourceAreas { get; set; } = new();
        public List<string> TopDestinationAreas { get; set; } = new();
    }

    /// <summary>
    /// Alan kullanım trendi
    /// </summary>
    public class SpaceUtilizationTrend
    {
        public int WarehouseId { get; set; }
        public DateTime AnalysisDate { get; set; } = DateTime.Now;
        public List<TrendDataPoint> DataPoints { get; set; } = new();
        public string OverallTrend { get; set; } = string.Empty; // INCREASING, DECREASING, STABLE
        public decimal TrendSlope { get; set; } // Trend eğimi
        public List<string> KeyInsights { get; set; } = new();
    }

    /// <summary>
    /// Trend veri noktası
    /// </summary>
    public class TrendDataPoint
    {
        public DateTime Date { get; set; }
        public decimal Utilization { get; set; }
        public decimal ChangeFromPrevious { get; set; }
        public string ChangeDirection { get; set; } = string.Empty; // UP, DOWN, SAME
        public List<string> ContributingFactors { get; set; } = new();
    }
}
