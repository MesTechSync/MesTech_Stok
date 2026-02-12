using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Desktop.Models
{
    // ========== ENHANCED BARCODE MODELS ==========
    public enum BarcodeFormat
    {
        EAN13, EAN8, UPC_A, UPC_E, CODE128, CODE39, QR_CODE, DATA_MATRIX, PDF417
    }

    public class BarcodeValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public BarcodeFormat Format { get; set; }
        public double ConfidenceLevel { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class ProductSuggestion
    {
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal SuggestedPrice { get; set; }
        public double ConfidenceScore { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> AlternativeNames { get; set; } = new();
    }

    // ========== BUSINESS INTELLIGENCE MODELS ==========
    public class TrendAnalysis
    {
        public string ProductId { get; set; } = string.Empty;
        public double GrowthRate { get; set; }
        public string TrendDirection { get; set; } = string.Empty;
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public Dictionary<string, double> SeasonalFactors { get; set; } = new();
    }

    public class ProfitOptimizationSuggestions
    {
        public string ProductId { get; set; } = string.Empty;
        public decimal CurrentMargin { get; set; }
        public decimal OptimalMargin { get; set; }
        public decimal PotentialIncrease { get; set; }
        public List<string> Actions { get; set; } = new();
        public string Priority { get; set; } = string.Empty;
    }

    public class ProcessOptimizationReport
    {
        public string ProcessName { get; set; } = string.Empty;
        public double CurrentEfficiency { get; set; }
        public double TargetEfficiency { get; set; }
        public List<string> Bottlenecks { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public TimeSpan EstimatedImprovementTime { get; set; }
    }

    public class MaintenanceAlert
    {
        public string EquipmentId { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
    }

    public class EquipmentHealthReport
    {
        public string EquipmentId { get; set; } = string.Empty;
        public double HealthScore { get; set; }
        public List<string> Issues { get; set; } = new();
        public DateTime LastMaintenance { get; set; }
        public DateTime NextMaintenance { get; set; }
        public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
    }

    public class VendorPerformanceMetrics
    {
        public string VendorId { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public double QualityScore { get; set; }
        public double DeliveryScore { get; set; }
        public double PriceCompetitiveness { get; set; }
        public double OverallRating { get; set; }
        public List<string> StrengthAreas { get; set; } = new();
        public List<string> ImprovementAreas { get; set; } = new();
    }

    public class ComplianceReport
    {
        public string RegulationType { get; set; } = string.Empty;
        public bool IsCompliant { get; set; }
        public List<string> ViolationAreas { get; set; } = new();
        public List<string> RequiredActions { get; set; } = new();
        public DateTime AssessmentDate { get; set; }
        public DateTime NextAssessmentDate { get; set; }
    }

    // ========== CUSTOMER & MARKETING MODELS ==========
    public class ProductRecommendation
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public double RecommendationScore { get; set; }
        public string ReasonCode { get; set; } = string.Empty;
        public List<string> TargetCustomerSegments { get; set; } = new();
    }

    public class SatisfactionMetrics
    {
        public double OverallScore { get; set; }
        public Dictionary<string, double> CategoryScores { get; set; } = new();
        public List<string> PositiveFeedback { get; set; } = new();
        public List<string> ImprovementAreas { get; set; } = new();
        public int ResponseCount { get; set; }
    }

    public class MarketingStrategy
    {
        public string CampaignName { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty;
        public List<string> Channels { get; set; } = new();
        public decimal Budget { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Dictionary<string, string> KPIs { get; set; } = new();
    }

    // ========== FINANCIAL MODELS ==========
    public class ProfitabilityMetrics
    {
        public decimal GrossProfit { get; set; }
        public decimal NetProfit { get; set; }
        public double GrossMargin { get; set; }
        public double NetMargin { get; set; }
        public double ROI { get; set; }
        public Dictionary<string, decimal> CategoryProfits { get; set; } = new();
    }

    public class InventoryMetrics
    {
        public decimal TotalValue { get; set; }
        public double TurnoverRate { get; set; }
        public int SlowMovingItems { get; set; }
        public decimal DeadStock { get; set; }
        public double ServiceLevel { get; set; }
        public Dictionary<string, int> CategoryCounts { get; set; } = new();
    }

    public class CustomerFeedbackMetrics
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();
        public List<string> CommonCompliments { get; set; } = new();
        public List<string> CommonComplaints { get; set; } = new();
    }

    public class CompetitivePosition
    {
        public string CompetitorName { get; set; } = string.Empty;
        public double MarketShare { get; set; }
        public double PricePosition { get; set; }
        public List<string> Advantages { get; set; } = new();
        public List<string> Disadvantages { get; set; } = new();
    }

    public class ImprovementSuggestion
    {
        public string Area { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal EstimatedCost { get; set; }
        public TimeSpan EstimatedImplementationTime { get; set; }
    }

    public class ProfitLossStatement
    {
        public decimal Revenue { get; set; }
        public decimal CostOfGoodsSold { get; set; }
        public decimal GrossProfit { get; set; }
        public decimal OperatingExpenses { get; set; }
        public decimal NetIncome { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }

    public class CashFlowAnalysis
    {
        public decimal OperatingCashFlow { get; set; }
        public decimal InvestingCashFlow { get; set; }
        public decimal FinancingCashFlow { get; set; }
        public decimal NetCashFlow { get; set; }
        public Dictionary<string, decimal> MonthlyFlows { get; set; } = new();
    }

    public class FinancialRatio
    {
        public string RatioName { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double IndustryBenchmark { get; set; }
        public string PerformanceIndicator { get; set; } = string.Empty;
        public string Interpretation { get; set; } = string.Empty;
    }

    public class BudgetVarianceAnalysis
    {
        public string Category { get; set; } = string.Empty;
        public decimal BudgetedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public decimal Variance { get; set; }
        public double VariancePercentage { get; set; }
        public string VarianceReason { get; set; } = string.Empty;
    }

    public class TaxOptimizationSuggestions
    {
        public string TaxCategory { get; set; } = string.Empty;
        public decimal CurrentTax { get; set; }
        public decimal OptimizedTax { get; set; }
        public decimal PotentialSavings { get; set; }
        public List<string> OptimizationStrategies { get; set; } = new();
    }

    public class FinancialForecasting
    {
        public DateTime ForecastDate { get; set; }
        public decimal PredictedRevenue { get; set; }
        public decimal PredictedExpenses { get; set; }
        public decimal PredictedProfit { get; set; }
        public double ConfidenceLevel { get; set; }
        public Dictionary<string, decimal> ScenarioAnalysis { get; set; } = new();
    }

    // ========== OPERATIONAL MODELS ==========
    public class WarehouseEfficiency
    {
        public double SpaceUtilization { get; set; }
        public double PickingAccuracy { get; set; }
        public TimeSpan AveragePickTime { get; set; }
        public double OrderFulfillmentRate { get; set; }
        public Dictionary<string, double> ZoneEfficiencies { get; set; } = new();
    }

    public class OrderFulfillmentMetrics
    {
        public double OnTimeDeliveryRate { get; set; }
        public TimeSpan AverageProcessingTime { get; set; }
        public double OrderAccuracy { get; set; }
        public int BackorderCount { get; set; }
        public Dictionary<string, TimeSpan> ProcessStepTimes { get; set; } = new();
    }

    public class QualityMetrics
    {
        public double DefectRate { get; set; }
        public double FirstPassYield { get; set; }
        public double CustomerReturnRate { get; set; }
        public List<string> QualityIssues { get; set; } = new();
        public Dictionary<string, double> InspectionResults { get; set; } = new();
    }

    public class ProductivityMetrics
    {
        public double UnitsPerHour { get; set; }
        public double EfficiencyRating { get; set; }
        public TimeSpan DowntimeHours { get; set; }
        public double UtilizationRate { get; set; }
        public Dictionary<string, double> DepartmentProductivity { get; set; } = new();
    }

    public class ResourceUtilization
    {
        public string ResourceType { get; set; } = string.Empty;
        public double UtilizationPercentage { get; set; }
        public TimeSpan ActiveTime { get; set; }
        public TimeSpan IdleTime { get; set; }
        public List<string> OptimizationOpportunities { get; set; } = new();
    }

    public class ProcessPerformance
    {
        public string ProcessName { get; set; } = string.Empty;
        public TimeSpan CycleTime { get; set; }
        public double Throughput { get; set; }
        public double ErrorRate { get; set; }
        public List<string> PerformanceIndicators { get; set; } = new();
    }

    // ========== SUPPLY CHAIN MODELS ==========
    public class SupplierOptimization
    {
        public string SupplierId { get; set; } = string.Empty;
        public double OptimizationScore { get; set; }
        public List<string> OptimizationAreas { get; set; } = new();
        public decimal PotentialSavings { get; set; }
        public string RecommendedAction { get; set; } = string.Empty;
    }

    public class InventoryOptimization
    {
        public string ProductId { get; set; } = string.Empty;
        public int CurrentLevel { get; set; }
        public int OptimalLevel { get; set; }
        public int ReorderPoint { get; set; }
        public int EconomicOrderQuantity { get; set; }
        public decimal CarryingCostSavings { get; set; }
    }

    public class LogisticsOptimization
    {
        public string RouteId { get; set; } = string.Empty;
        public TimeSpan CurrentTime { get; set; }
        public TimeSpan OptimizedTime { get; set; }
        public decimal CostSavings { get; set; }
        public List<string> OptimizationSuggestions { get; set; } = new();
    }

    public class RiskMitigation
    {
        public string RiskType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public double Probability { get; set; }
        public List<string> MitigationStrategies { get; set; } = new();
        public decimal Impact { get; set; }
    }

    public class CostOptimization
    {
        public string CostCategory { get; set; } = string.Empty;
        public decimal CurrentCost { get; set; }
        public decimal OptimizedCost { get; set; }
        public decimal Savings { get; set; }
        public List<string> OptimizationMethods { get; set; } = new();
    }

    // ========== RISK & ANALYTICS MODELS ==========
    public class RiskMatrix
    {
        public string RiskId { get; set; } = string.Empty;
        public string RiskName { get; set; } = string.Empty;
        public int Impact { get; set; }
        public int Probability { get; set; }
        public int RiskScore { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public class MitigationStrategy
    {
        public string StrategyId { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal ImplementationCost { get; set; }
        public double EffectivenessScore { get; set; }
        public TimeSpan ImplementationTime { get; set; }
    }

    public class RiskTolerance
    {
        public string RiskCategory { get; set; } = string.Empty;
        public int MaxAcceptableScore { get; set; }
        public string ToleranceLevel { get; set; } = string.Empty;
        public List<string> AcceptanceCriteria { get; set; } = new();
    }

    // ========== PATTERN & CHANNEL MODELS ==========
    public class SeasonalityPattern
    {
        public string ProductId { get; set; } = string.Empty;
        public Dictionary<string, double> MonthlyFactors { get; set; } = new();
        public double SeasonalityIndex { get; set; }
        public string PeakSeason { get; set; } = string.Empty;
        public string LowSeason { get; set; } = string.Empty;
    }

    public class SalesChannel
    {
        public string ChannelId { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public double ConversionRate { get; set; }
        public int CustomerCount { get; set; }
        public double Profitability { get; set; }
    }

    // ========== CLOUD & ARCHITECTURE MODELS ==========
    public class TenantProvisionRequest
    {
        public string TenantId { get; set; } = string.Empty;
        public string TenantName { get; set; } = string.Empty;
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public string PlanType { get; set; } = string.Empty;
    }

    public class EventSubscription
    {
        public string EventType { get; set; } = string.Empty;
        public string SubscriberId { get; set; } = string.Empty;
        public string CallbackUrl { get; set; } = string.Empty;
        public Dictionary<string, string> Filters { get; set; } = new();
        public bool IsActive { get; set; }
    }

    public class ServiceEndpoint
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string HealthStatus { get; set; } = string.Empty;
        public DateTime LastHealthCheck { get; set; }
    }

    public class ServiceHealthCheck
    {
        public string ServiceName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class FunctionExecutionResult
    {
        public string FunctionName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public object Result { get; set; } = new();
        public TimeSpan ExecutionTime { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ServerlessFunction
    {
        public string FunctionId { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public string Runtime { get; set; } = string.Empty;
        public Dictionary<string, string> Environment { get; set; } = new();
        public string TriggerType { get; set; } = string.Empty;
    }

    public class ContainerClusterStatus
    {
        public string ClusterId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int NodeCount { get; set; }
        public int RunningPods { get; set; }
        public Dictionary<string, object> Resources { get; set; } = new();
    }

    public class ContainerDeploymentRequest
    {
        public string ServiceName { get; set; } = string.Empty;
        public string ImageTag { get; set; } = string.Empty;
        public int Replicas { get; set; }
        public Dictionary<string, string> Environment { get; set; } = new();
        public Dictionary<string, object> Resources { get; set; } = new();
    }

    public class EdgeLocation
    {
        public string LocationId { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ResourceLimits
    {
        public int MaxCPU { get; set; }
        public int MaxMemoryMB { get; set; }
        public int MaxStorageGB { get; set; }
        public int MaxNetworkMbps { get; set; }
        public Dictionary<string, int> CustomLimits { get; set; } = new();
    }

    public class BillingConfiguration
    {
        public string PlanType { get; set; } = string.Empty;
        public decimal MonthlyLimit { get; set; }
        public Dictionary<string, decimal> ResourceRates { get; set; } = new();
        public bool AutoScalingEnabled { get; set; }
        public string BillingCycle { get; set; } = string.Empty;
    }

    public class ScalingEvent
    {
        public DateTime Timestamp { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ScalingDirection { get; set; } = string.Empty;
        public int OldInstanceCount { get; set; }
        public int NewInstanceCount { get; set; }
        public string Trigger { get; set; } = string.Empty;
    }

    // ========== SECURITY MODELS ==========
    public class AuthenticationRequest
    {
        public string Username { get; set; } = string.Empty;
        public string AuthMethod { get; set; } = string.Empty;
        public Dictionary<string, object> Credentials { get; set; } = new();
        public string ClientInfo { get; set; } = string.Empty;
    }

    public class AuthenticationResult
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public List<string> Permissions { get; set; } = new();
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class AuthorizationResult
    {
        public bool Authorized { get; set; }
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public List<string> RequiredPermissions { get; set; } = new();
        public string DenialReason { get; set; } = string.Empty;
    }

    public class ResponseAction
    {
        public string ActionType { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string Priority { get; set; } = string.Empty;
        public DateTime ExecuteAt { get; set; }
    }

    public class ThreatResponse
    {
        public string ThreatId { get; set; } = string.Empty;
        public string ThreatType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public List<ResponseAction> Actions { get; set; } = new();
        public DateTime DetectedAt { get; set; }
    }

    public enum EncryptionLevel
    {
        None = 0,
        Basic = 1,
        Standard = 2,
        Advanced = 3,
        Military = 4
    }

    public class AuditQuery
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
    }

    public class AuditLog
    {
        public string LogId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public enum ComplianceFramework
    {
        GDPR, HIPAA, SOX, PCI_DSS, ISO27001, SOC2
    }

    public class ComplianceStatus
    {
        public ComplianceFramework Framework { get; set; }
        public bool IsCompliant { get; set; }
        public double ComplianceScore { get; set; }
        public List<string> NonCompliantAreas { get; set; } = new();
        public DateTime LastAssessment { get; set; }
    }

    public class BiometricData
    {
        public string DataType { get; set; } = string.Empty;
        public byte[] TemplateData { get; set; } = Array.Empty<byte>();
        public string Algorithm { get; set; } = string.Empty;
        public double Quality { get; set; }
    }

    public class SecurityAnomaly
    {
        public string AnomalyId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
        public string Severity { get; set; } = string.Empty;
        public Dictionary<string, object> Details { get; set; } = new();
    }

    public class TimeRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public TimeSpan Duration => End - Start;
    }

    public class SecureComputationRequest
    {
        public string ComputationId { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public Dictionary<string, byte[]> EncryptedInputs { get; set; } = new();
        public string SecurityLevel { get; set; } = string.Empty;
    }

    public class ComputationResult
    {
        public string ComputationId { get; set; } = string.Empty;
        public bool Success { get; set; }
        public byte[] EncryptedResult { get; set; } = Array.Empty<byte>();
        public TimeSpan ComputationTime { get; set; }
    }

    public class QuantumResistantKey
    {
        public string KeyId { get; set; } = string.Empty;
        public string Algorithm { get; set; } = string.Empty;
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    // ========== SUSTAINABILITY & ESG MODELS ==========
    public class SustainabilityBehaviorPattern
    {
        public string PatternId { get; set; } = string.Empty;
        public string PatternType { get; set; } = string.Empty;
        public double Frequency { get; set; }
        public Dictionary<string, object> Characteristics { get; set; } = new();
        public DateTime FirstObserved { get; set; }
    }

    public class CarbonReductionSuggestion
    {
        public string SuggestionId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double EstimatedReduction { get; set; }
        public decimal ImplementationCost { get; set; }
        public string Priority { get; set; } = string.Empty;
    }

    public class WasteAnalysis
    {
        public string WasteType { get; set; } = string.Empty;
        public double Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public double RecycleRate { get; set; }
        public decimal DisposalCost { get; set; }
        public List<string> ReductionOpportunities { get; set; } = new();
    }

    public class CircularEconomyOpportunities
    {
        public string OpportunityType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal PotentialValue { get; set; }
        public string ImplementationDifficulty { get; set; } = string.Empty;
        public List<string> RequiredActions { get; set; } = new();
    }

    public class RenewableEnergyStatus
    {
        public double TotalConsumption { get; set; }
        public double RenewablePercentage { get; set; }
        public Dictionary<string, double> EnergySourcesMix { get; set; } = new();
        public decimal CostSavings { get; set; }
        public double CarbonOffset { get; set; }
    }

    public class SupplierSustainabilityReport
    {
        public string SupplierId { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public double SustainabilityScore { get; set; }
        public Dictionary<string, double> ESGScores { get; set; } = new();
        public List<string> CertificationsList { get; set; } = new();
        public List<string> ImprovementAreas { get; set; } = new();
    }

    public class SustainableAlternative
    {
        public string ProductId { get; set; } = string.Empty;
        public string AlternativeId { get; set; } = string.Empty;
        public string AlternativeName { get; set; } = string.Empty;
        public double SustainabilityScore { get; set; }
        public decimal PriceDifference { get; set; }
        public List<string> Benefits { get; set; } = new();
    }

    public class ESGScore
    {
        public double EnvironmentalScore { get; set; }
        public double SocialScore { get; set; }
        public double GovernanceScore { get; set; }
        public double OverallScore { get; set; }
        public Dictionary<string, double> SubCategoryScores { get; set; } = new();
    }

    public class BiodiversityImpactReport
    {
        public string AssessmentId { get; set; } = string.Empty;
        public double BiodiversityImpactScore { get; set; }
        public List<string> AffectedEcosystems { get; set; } = new();
        public Dictionary<string, string> MitigationMeasures { get; set; } = new();
        public DateTime AssessmentDate { get; set; }
    }

    public class WaterUsageReport
    {
        public double TotalUsage { get; set; }
        public string UsageUnit { get; set; } = string.Empty;
        public Dictionary<string, double> UsageByProcess { get; set; } = new();
        public double RecycledPercentage { get; set; }
        public List<string> ConservationOpportunities { get; set; } = new();
    }

    public class WaterOptimizationPlan
    {
        public string PlanId { get; set; } = string.Empty;
        public List<string> OptimizationStrategies { get; set; } = new();
        public double TargetReduction { get; set; }
        public decimal ImplementationCost { get; set; }
        public TimeSpan ImplementationTimeframe { get; set; }
    }

    public class PackagingSustainabilityAnalysis
    {
        public string PackagingType { get; set; } = string.Empty;
        public double RecyclabilityScore { get; set; }
        public double BiodegradabilityScore { get; set; }
        public decimal CarbonFootprint { get; set; }
        public List<string> SustainableAlternatives { get; set; } = new();
    }

    public class EcoFriendlyPackagingOption
    {
        public string OptionId { get; set; } = string.Empty;
        public string MaterialType { get; set; } = string.Empty;
        public double SustainabilityScore { get; set; }
        public decimal CostPerUnit { get; set; }
        public double CarbonReduction { get; set; }
        public List<string> Certifications { get; set; } = new();
    }

    public class ComparisonData
    {
        public string MetricName { get; set; } = string.Empty;
        public double CurrentValue { get; set; }
        public double IndustryAverage { get; set; }
        public double BestPractice { get; set; }
        public string PerformanceRating { get; set; } = string.Empty;
    }

    public class CarbonHotspot
    {
        public string ProcessId { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public double CarbonEmission { get; set; }
        public double PercentageOfTotal { get; set; }
        public List<string> ReductionOpportunities { get; set; } = new();
    }

    public class SustainabilityGoal
    {
        public string GoalId { get; set; } = string.Empty;
        public string GoalName { get; set; } = string.Empty;
        public double TargetValue { get; set; }
        public double CurrentValue { get; set; }
        public DateTime TargetDate { get; set; }
        public double ProgressPercentage { get; set; }
    }

    public class EnvironmentalMetrics
    {
        public double CarbonFootprint { get; set; }
        public double WaterUsage { get; set; }
        public double WasteGeneration { get; set; }
        public double EnergyConsumption { get; set; }
        public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
    }

    public class SocialMetrics
    {
        public double EmployeeSatisfaction { get; set; }
        public double CommunityImpact { get; set; }
        public double DiversityIndex { get; set; }
        public double SafetyScore { get; set; }
        public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
    }

    public class GovernanceMetrics
    {
        public double TransparencyScore { get; set; }
        public double EthicsScore { get; set; }
        public double ComplianceScore { get; set; }
        public double BoardDiversityScore { get; set; }
        public Dictionary<string, double> AdditionalMetrics { get; set; } = new();
    }

    public class ESGImprovement
    {
        public string Area { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public string TargetStatus { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
        public decimal InvestmentRequired { get; set; }
        public DateTime TargetDate { get; set; }
    }

    public class MitigationRecommendation
    {
        public string RecommendationId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal ImplementationCost { get; set; }
        public TimeSpan ImplementationTime { get; set; }
        public double ExpectedImpact { get; set; }
    }

    // ========== IOT & WAREHOUSE MODELS ==========
    public class AssetTrackingHistory
    {
        public string AssetId { get; set; } = string.Empty;
        public List<LocationPoint> LocationHistory { get; set; } = new();
        public Dictionary<DateTime, string> StatusHistory { get; set; } = new();
        public List<string> MaintenanceHistory { get; set; } = new();
    }

    public class LocationPoint
    {
        public DateTime Timestamp { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public string Zone { get; set; } = string.Empty;
    }

    public class EquipmentStatus
    {
        public string EquipmentId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double TemperatureC { get; set; }
        public double Humidity { get; set; }
        public double Vibration { get; set; }
        public Dictionary<string, double> SensorReadings { get; set; } = new();
    }

    public class MaintenanceSchedule
    {
        public string EquipmentId { get; set; } = string.Empty;
        public DateTime NextMaintenance { get; set; }
        public string MaintenanceType { get; set; } = string.Empty;
        public TimeSpan EstimatedDuration { get; set; }
        public List<string> RequiredParts { get; set; } = new();
    }

    public class SustainabilityMetrics
    {
        public double EnergyEfficiency { get; set; }
        public double CarbonFootprint { get; set; }
        public double WasteReduction { get; set; }
        public Dictionary<string, double> GreenMetrics { get; set; } = new();
    }

    public class SafetyAlert
    {
        public string AlertId { get; set; } = string.Empty;
        public string AlertType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class DefectAnalysisReport
    {
        public string ProductId { get; set; } = string.Empty;
        public double DefectRate { get; set; }
        public List<string> CommonDefects { get; set; } = new();
        public Dictionary<string, int> DefectCategories { get; set; } = new();
        public List<string> RootCauses { get; set; } = new();
    }

    public class DroneCapability
    {
        public string DroneId { get; set; } = string.Empty;
        public List<string> Sensors { get; set; } = new();
        public double FlightTime { get; set; }
        public double PayloadCapacity { get; set; }
        public List<string> SupportedTasks { get; set; } = new();
    }

    public class EnergyZoneUsage
    {
        public string ZoneId { get; set; } = string.Empty;
        public string ZoneName { get; set; } = string.Empty;
        public double EnergyConsumption { get; set; }
        public double Efficiency { get; set; }
        public DateTime MeasurementTime { get; set; }
    }

    public class EnergyOptimizationSuggestion
    {
        public string SuggestionId { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
        public double PotentialSaving { get; set; }
        public decimal ImplementationCost { get; set; }
    }

    public class RenewableEnergyUsage
    {
        public double SolarGeneration { get; set; }
        public double WindGeneration { get; set; }
        public double HydroGeneration { get; set; }
        public double TotalRenewable { get; set; }
        public double RenewablePercentage { get; set; }
    }

    public class SafetyZoneStatus
    {
        public string ZoneId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> ActiveAlerts { get; set; } = new();
        public int PersonnelCount { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class EmergencyProtocolStatus
    {
        public string ProtocolId { get; set; } = string.Empty;
        public string ProtocolName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime LastTested { get; set; }
        public List<string> RequiredActions { get; set; } = new();
    }

    public class PPEComplianceCheck
    {
        public string PersonnelId { get; set; } = string.Empty;
        public bool IsCompliant { get; set; }
        public List<string> MissingItems { get; set; } = new();
        public DateTime CheckTime { get; set; }
        public string Zone { get; set; } = string.Empty;
    }

    public class QualityStatus
    {
        public string ProductId { get; set; } = string.Empty;
        public string QualityGrade { get; set; } = string.Empty;
        public double QualityScore { get; set; }
        public DateTime InspectionDate { get; set; }
        public string InspectorId { get; set; } = string.Empty;
    }

    public class QualityDefect
    {
        public string DefectId { get; set; } = string.Empty;
        public string DefectType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public DateTime DetectedAt { get; set; }
    }

    public class VisualInspectionResult
    {
        public string InspectionId { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public List<string> DetectedIssues { get; set; } = new();
        public double ConfidenceScore { get; set; }
        public byte[] InspectionImage { get; set; } = Array.Empty<byte>();
    }

    public class DimensionalAnalysis
    {
        public string ProductId { get; set; } = string.Empty;
        public Dictionary<string, double> Measurements { get; set; } = new();
        public Dictionary<string, double> Tolerances { get; set; } = new();
        public bool WithinSpecification { get; set; }
        public List<string> OutOfSpecDimensions { get; set; } = new();
    }

    public class ScheduledLoading
    {
        public string LoadingId { get; set; } = string.Empty;
        public DateTime ScheduledTime { get; set; }
        public string DockId { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
    }

    public class DockCapability
    {
        public string DockId { get; set; } = string.Empty;
        public List<string> SupportedVehicleTypes { get; set; } = new();
        public double MaxWeight { get; set; }
        public bool IsAvailable { get; set; }
        public List<string> Features { get; set; } = new();
    }

    // ========== AR/VR MODELS ==========
    public class ARTrainingModule
    {
        public string ModuleId { get; set; } = string.Empty;
        public string ModuleName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public List<string> LearningObjectives { get; set; } = new();
    }

    public class ARTrainingSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string ModuleId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double ProgressPercentage { get; set; }
    }

    public class ARMaintenanceGuide
    {
        public string GuideId { get; set; } = string.Empty;
        public string EquipmentId { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public List<string> Steps { get; set; } = new();
        public List<string> SafetyWarnings { get; set; } = new();
    }

    public class ARInspectionGuide
    {
        public string GuideId { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public List<string> CheckPoints { get; set; } = new();
        public Dictionary<string, string> QualityCriteria { get; set; } = new();
    }

    public class ARRemoteSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string ExpertUserId { get; set; } = string.Empty;
        public string TechnicianUserId { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public bool IsActive { get; set; }
        public string ProblemDescription { get; set; } = string.Empty;
    }

    public class ARInfoOverlay
    {
        public string OverlayId { get; set; } = string.Empty;
        public string ObjectId { get; set; } = string.Empty;
        public Dictionary<string, string> InfoItems { get; set; } = new();
        public string DisplayType { get; set; } = string.Empty;
    }

    public class ARAnimation
    {
        public string AnimationId { get; set; } = string.Empty;
        public string AnimationName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string TriggerCondition { get; set; } = string.Empty;
        public List<string> AnimationSteps { get; set; } = new();
    }

    public class ARScale
    {
        public double ScaleX { get; set; } = 1.0;
        public double ScaleY { get; set; } = 1.0;
        public double ScaleZ { get; set; } = 1.0;
        public bool IsUniform { get; set; }
    }

    public class ARMaterial
    {
        public string MaterialId { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string TextureUrl { get; set; } = string.Empty;
        public Dictionary<string, object> Properties { get; set; } = new();
    }

    public class ARWaypoint
    {
        public string WaypointId { get; set; } = string.Empty;
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class ARNavigationInstruction
    {
        public string InstructionId { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
        public double Distance { get; set; }
        public string Landmark { get; set; } = string.Empty;
        public string NextAction { get; set; } = string.Empty;
    }
}
