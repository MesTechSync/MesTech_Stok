using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using MesTechStok.Desktop.Models;

namespace MesTechStok.Desktop.Services.Analytics
{
    /// <summary>
    /// 🚀 YENİLİKÇİ GELİŞTİRME: Advanced Business Intelligence & Analytics Service
    /// Gelecekteki karar destek sistemleri için gelişmiş analitik yetenekleri
    /// </summary>
    public interface IAdvancedBusinessIntelligenceService
    {
        // 📊 ANALYTICS MODÜL 1: Real-time dashboard metrics with ML predictions
        Task<BusinessDashboard> GetRealTimeDashboardAsync();

        // 📊 ANALYTICS MODÜL 2: Customer behavior analysis and segmentation
        Task<CustomerAnalytics> AnalyzeCustomerBehaviorAsync(int customerId);
        Task<List<CustomerSegment>> GetCustomerSegmentsAsync();

        // 📊 ANALYTICS MODÜL 3: Product performance analytics with trend analysis
        Task<ProductPerformanceReport> GetProductPerformanceAsync(int productId, TimeRange range);
        Task<List<TrendAnalysis>> GetProductTrendsAsync();

        // 📊 ANALYTICS MODÜL 4: Financial analytics and profit optimization
        Task<FinancialAnalytics> GetFinancialAnalyticsAsync(DateTime fromDate, DateTime toDate);
        Task<ProfitOptimizationSuggestions> GetProfitOptimizationAsync();

        // 📊 ANALYTICS MODÜL 5: Operational efficiency metrics
        Task<OperationalMetrics> GetOperationalEfficiencyAsync();
        Task<ProcessOptimizationReport> AnalyzeProcessEfficiencyAsync();

        // 📊 ANALYTICS MODÜL 6: Predictive maintenance for equipment
        Task<List<MaintenanceAlert>> GetPredictiveMaintenanceAlertsAsync();
        Task<EquipmentHealthReport> AnalyzeEquipmentHealthAsync(string equipmentId);

        // 📊 ANALYTICS MODÜL 7: Supply chain optimization
        Task<SupplyChainOptimization> OptimizeSupplyChainAsync();
        Task<List<VendorPerformanceMetrics>> AnalyzeVendorPerformanceAsync();

        // 📊 ANALYTICS MODÜL 8: Risk assessment and compliance monitoring
        Task<RiskAssessmentReport> GetBusinessRiskAssessmentAsync();
        Task<ComplianceReport> GetComplianceStatusAsync();
    }

    // Supporting Models for Business Intelligence

    public class BusinessDashboard
    {
        public DashboardMetrics CurrentMetrics { get; set; } = new();
        public DashboardMetrics PredictedMetrics { get; set; } = new();
        public List<KPIAlert> Alerts { get; set; } = new();
        public List<ChartData> Charts { get; set; } = new();
        public DateTime LastUpdated { get; set; }
        public double DataQualityScore { get; set; }
    }

    public class DashboardMetrics
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalProfit { get; set; }
        public int TotalOrders { get; set; }
        public int ActiveProducts { get; set; }
        public int ActiveCustomers { get; set; }
        public double AverageOrderValue { get; set; }
        public double CustomerSatisfactionScore { get; set; }
        public int InventoryTurnover { get; set; }
    }

    public class CustomerAnalytics
    {
        public int CustomerId { get; set; }
        public string CustomerSegment { get; set; } = string.Empty;
        public decimal LifetimeValue { get; set; }
        public double ChurnProbability { get; set; }
        public List<string> PreferredCategories { get; set; } = new();
        public PurchaseBehavior BehaviorProfile { get; set; } = new();
        public List<ProductRecommendation> Recommendations { get; set; } = new();
        public SatisfactionMetrics Satisfaction { get; set; } = new();
    }

    public class CustomerSegment
    {
        public string SegmentName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CustomerCount { get; set; }
        public decimal AverageSpending { get; set; }
        public double RetentionRate { get; set; }
        public string[] Characteristics { get; set; } = Array.Empty<string>();
        public MarketingStrategy SuggestedStrategy { get; set; } = new();
    }

    public class ProductPerformanceReport
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public SalesMetrics Sales { get; set; } = new();
        public ProfitabilityMetrics Profitability { get; set; } = new();
        public InventoryMetrics Inventory { get; set; } = new();
        public CustomerFeedbackMetrics Feedback { get; set; } = new();
        public CompetitivePosition MarketPosition { get; set; } = new();
        public List<ImprovementSuggestion> Suggestions { get; set; } = new();
    }

    public class FinancialAnalytics
    {
        public ProfitLossStatement ProfitLoss { get; set; } = new();
        public CashFlowAnalysis CashFlow { get; set; } = new();
        public List<FinancialRatio> Ratios { get; set; } = new();
        public BudgetVarianceAnalysis BudgetVariance { get; set; } = new();
        public TaxOptimizationSuggestions TaxOptimization { get; set; } = new();
        public FinancialForecasting Forecast { get; set; } = new();
    }

    public class OperationalMetrics
    {
        public WarehouseEfficiency Warehouse { get; set; } = new();
        public OrderFulfillmentMetrics OrderFulfillment { get; set; } = new();
        public QualityMetrics Quality { get; set; } = new();
        public ProductivityMetrics Productivity { get; set; } = new();
        public ResourceUtilization Resources { get; set; } = new();
        public ProcessPerformance Processes { get; set; } = new();
    }

    public class SupplyChainOptimization
    {
        public List<SupplierOptimization> SupplierRecommendations { get; set; } = new();
        public InventoryOptimization Inventory { get; set; } = new();
        public LogisticsOptimization Logistics { get; set; } = new();
        public RiskMitigation Risks { get; set; } = new();
        public CostOptimization Costs { get; set; } = new();
        public double OptimizationScore { get; set; }
    }

    public class RiskAssessmentReport
    {
        public List<BusinessRisk> IdentifiedRisks { get; set; } = new();
        public RiskMatrix RiskMatrix { get; set; } = new();
        public List<MitigationStrategy> MitigationStrategies { get; set; } = new();
        public double OverallRiskScore { get; set; }
        public RiskTolerance CurrentRiskTolerance { get; set; } = new();
        public DateTime NextAssessmentDue { get; set; }
    }

    // Detailed Supporting Classes (sampling key ones)

    public class PurchaseBehavior
    {
        public double AverageOrderFrequency { get; set; }
        public TimeSpan AverageTimeBetweenOrders { get; set; }
        public string PreferredPaymentMethod { get; set; } = string.Empty;
        public DayOfWeek PreferredShoppingDay { get; set; }
        public int AverageItemsPerOrder { get; set; }
        public decimal PriceSenativity { get; set; }
        public bool IsSeasonalBuyer { get; set; }
        public List<string> BrandLoyalty { get; set; } = new();
    }

    public class SalesMetrics
    {
        public int TotalUnitsSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public double GrowthRate { get; set; }
        public double MarketShare { get; set; }
        public SeasonalityPattern Seasonality { get; set; } = new();
        public List<SalesChannel> ChannelPerformance { get; set; } = new();
    }

    public class BusinessRisk
    {
        public string RiskType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public RiskLevel Level { get; set; }
        public double Probability { get; set; }
        public decimal PotentialImpact { get; set; }
        public string[] AffectedAreas { get; set; } = Array.Empty<string>();
        public DateTime LastAssessed { get; set; }
        public string ResponsiblePerson { get; set; } = string.Empty;
    }

    // Enums for Business Intelligence
    public enum RiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum TimeRange
    {
        LastWeek,
        LastMonth,
        LastQuarter,
        LastYear,
        Custom
    }

    public class KPIAlert
    {
        public string KPIName { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal ThresholdValue { get; set; }
        public DateTime AlertTime { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
    }

    public enum AlertSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public class ChartData
    {
        public string ChartTitle { get; set; } = string.Empty;
        public ChartType Type { get; set; }
        public List<DataPoint> DataPoints { get; set; } = new();
        public string XAxisLabel { get; set; } = string.Empty;
        public string YAxisLabel { get; set; } = string.Empty;
    }

    public enum ChartType
    {
        Line,
        Bar,
        Pie,
        Area,
        Scatter,
        Heatmap
    }

    public class DataPoint
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
