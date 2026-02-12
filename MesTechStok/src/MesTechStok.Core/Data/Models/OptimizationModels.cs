using System;
using System.Collections.Generic;

namespace MesTechStok.Core.Data.Models
{
    #region Optimizasyon Kriterleri

    /// <summary>
    /// Optimizasyon kriterleri
    /// </summary>
    public class OptimizationCriteria
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public OptimizationType Type { get; set; }
        public Priority Priority { get; set; }
        public decimal MinEfficiency { get; set; }
        public decimal MaxCost { get; set; }
        public int MaxImplementationTime { get; set; } // Gün cinsinden
        public List<string> PreferredZones { get; set; } = new List<string>();
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion

    #region Optimizasyon Önerileri

    /// <summary>
    /// Optimizasyon önerisi
    /// </summary>
    public class OptimizationRecommendation
    {
        public int Id { get; set; }
        public OptimizationType Type { get; set; }
        public Priority Priority { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EstimatedImpact { get; set; } = string.Empty;
        public EffortLevel ImplementationEffort { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal EstimatedSavings { get; set; }
        public decimal PaybackPeriod { get; set; } // Ay cinsinden
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Optimizasyon aksiyonu
    /// </summary>
    public class OptimizationAction
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public OptimizationType Type { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal EstimatedSavings { get; set; }
        public int EstimatedDuration { get; set; } // Gün cinsinden
        public RiskLevel RiskLevel { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Optimizasyon etkisi
    /// </summary>
    public class OptimizationImpact
    {
        public int ActionId { get; set; }
        public string ActionName { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
        public decimal EstimatedSavings { get; set; }
        public decimal PaybackPeriod { get; set; } // Ay cinsinden
        public RiskLevel RiskLevel { get; set; }
        public TimeSpan ImplementationTime { get; set; }
        public decimal SuccessProbability { get; set; }
        public DateTime CalculatedDate { get; set; }
    }

    #endregion

    #region Verimlilik Raporları

    /// <summary>
    /// Depo verimlilik raporu
    /// </summary>
    public class WarehouseEfficiencyReport
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string ReportPeriod { get; set; } = string.Empty;
        public decimal OverallEfficiency { get; set; }
        public decimal SpaceUtilization { get; set; }
        public decimal MovementEfficiency { get; set; }
        public decimal LaborEfficiency { get; set; }
        public decimal CostEfficiency { get; set; }
        public DateTime GeneratedDate { get; set; }
    }

    /// <summary>
    /// Bölge verimlilik raporu
    /// </summary>
    public class ZoneEfficiencyReport
    {
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
        public string ReportPeriod { get; set; } = string.Empty;
        public decimal Efficiency { get; set; }
        public decimal SpaceUtilization { get; set; }
        public decimal MovementEfficiency { get; set; }
        public decimal LaborEfficiency { get; set; }
        public decimal CostEfficiency { get; set; }
        public DateTime GeneratedDate { get; set; }
    }

    /// <summary>
    /// Raf verimlilik raporu
    /// </summary>
    public class RackEfficiencyReport
    {
        public int RackId { get; set; }
        public string RackName { get; set; } = string.Empty;
        public string ReportPeriod { get; set; } = string.Empty;
        public decimal Efficiency { get; set; }
        public decimal SpaceUtilization { get; set; }
        public decimal MovementEfficiency { get; set; }
        public decimal LaborEfficiency { get; set; }
        public decimal CostEfficiency { get; set; }
        public DateTime GeneratedDate { get; set; }
    }

    /// <summary>
    /// Raf verimlilik raporu
    /// </summary>
    public class ShelfEfficiencyReport
    {
        public int ShelfId { get; set; }
        public string ShelfName { get; set; } = string.Empty;
        public string ReportPeriod { get; set; } = string.Empty;
        public decimal Efficiency { get; set; }
        public decimal SpaceUtilization { get; set; }
        public decimal MovementEfficiency { get; set; }
        public decimal LaborEfficiency { get; set; }
        public decimal CostEfficiency { get; set; }
        public DateTime GeneratedDate { get; set; }
    }

    #endregion

    #region Kapasite Planlama

    /// <summary>
    /// Kapasite planlama raporu
    /// </summary>
    public class CapacityPlanningReport
    {
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public int CurrentCapacity { get; set; }
        public int UsedCapacity { get; set; }
        public int AvailableCapacity { get; set; }
        public decimal CapacityUtilization { get; set; }
        public decimal ProjectedGrowth { get; set; }
        public decimal CapacityAlertThreshold { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<CapacityForecast> MonthlyForecast { get; set; } = new List<CapacityForecast>();
    }

    /// <summary>
    /// Kapasite tahmini
    /// </summary>
    public class CapacityForecast
    {
        public int WarehouseId { get; set; }
        public string ForecastPeriod { get; set; } = string.Empty;
        public int CurrentUsage { get; set; }
        public int ProjectedUsage { get; set; }
        public int AvailableCapacity { get; set; }
        public decimal GrowthRate { get; set; }
        public decimal ConfidenceLevel { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string Month { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kapasite uyarısı
    /// </summary>
    public class CapacityAlert
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public AlertType AlertType { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public decimal CurrentUtilization { get; set; }
        public decimal Threshold { get; set; }
        public Severity Severity { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    #endregion

    #region Konum Analizi

    /// <summary>
    /// Konum heatmap verisi
    /// </summary>
    public class LocationHeatmapData
    {
        public int WarehouseId { get; set; }
        public LocationHeatmapType HeatmapType { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<HeatmapPoint> HeatmapPoints { get; set; } = new List<HeatmapPoint>();
    }

    /// <summary>
    /// Heatmap noktası
    /// </summary>
    public class HeatmapPoint
    {
        public int X { get; set; }
        public int Y { get; set; }
        public decimal Value { get; set; }
        public int ZoneId { get; set; }
        public string ZoneName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Hareket paterni analizi
    /// </summary>
    public class MovementPatternAnalysis
    {
        public int WarehouseId { get; set; }
        public string AnalysisPeriod { get; set; } = string.Empty;
        public int TotalMovements { get; set; }
        public TimeSpan AverageMovementTime { get; set; }
        public List<int> PeakMovementHours { get; set; } = new List<int>();
        public List<MovementTrend> MovementTrends { get; set; } = new List<MovementTrend>();
        public DateTime GeneratedDate { get; set; }
    }

    /// <summary>
    /// Hareket trendi
    /// </summary>
    public class MovementTrend
    {
        public DateTime Date { get; set; }
        public int MovementCount { get; set; }
        public TimeSpan AverageTime { get; set; }
    }

    /// <summary>
    /// Alan kullanım trendi
    /// </summary>
    public class SpaceUtilizationTrend
    {
        public int WarehouseId { get; set; }
        public string AnalysisPeriod { get; set; } = string.Empty;
        public decimal CurrentUtilization { get; set; }
        public TrendDirection TrendDirection { get; set; }
        public List<MonthlyUtilization> MonthlyTrends { get; set; } = new List<MonthlyUtilization>();
        public DateTime GeneratedDate { get; set; }
    }

    /// <summary>
    /// Aylık kullanım
    /// </summary>
    public class MonthlyUtilization
    {
        public string Month { get; set; } = string.Empty;
        public decimal Utilization { get; set; }
        public decimal Change { get; set; }
    }

    #endregion

    #region Enum'lar

    /// <summary>
    /// Optimizasyon türü
    /// </summary>
    public enum OptimizationType
    {
        SpaceUtilization,
        MovementEfficiency,
        LaborEfficiency,
        CostEfficiency,
        SafetyImprovement,
        TechnologyUpgrade
    }

    /// <summary>
    /// Öncelik seviyesi
    /// </summary>
    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Uygulama çabası seviyesi
    /// </summary>
    public enum EffortLevel
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    /// <summary>
    /// Risk seviyesi
    /// </summary>
    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Uyarı türü
    /// </summary>
    public enum AlertType
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>
    /// Ciddiyet seviyesi
    /// </summary>
    public enum Severity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Konum heatmap türü
    /// </summary>
    public enum LocationHeatmapType
    {
        Utilization,
        Movement,
        Temperature,
        Humidity,
        Safety
    }

    /// <summary>
    /// Trend yönü
    /// </summary>
    public enum TrendDirection
    {
        Increasing,
        Decreasing,
        Stable,
        Fluctuating
    }

    #endregion
}
