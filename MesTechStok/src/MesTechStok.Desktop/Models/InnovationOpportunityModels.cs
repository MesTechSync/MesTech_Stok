using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MesTechStok.Desktop.Models
{
    /// <summary>
    /// Innovation Opportunity Analysis Models for Next-Generation MesTech Features
    /// Created: 2025-01-14
    /// Purpose: Support advanced AI, AR/VR, Blockchain, and IoT integrations
    /// </summary>

    #region AI-Powered Health Analytics Models

    public class SystemHealthAnalytics
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public double NetworkLatency { get; set; }
        public double DatabaseResponseTime { get; set; }
        public HealthStatus Status { get; set; }
        public List<HealthPrediction> Predictions { get; set; } = new List<HealthPrediction>();
        public List<MaintenanceRecommendation> Recommendations { get; set; } = new List<MaintenanceRecommendation>();
    }

    public class HealthPrediction
    {
        public int Id { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public double PredictedValue { get; set; }
        public double Confidence { get; set; }
        public DateTime PredictionTime { get; set; }
        public PredictionSeverity Severity { get; set; }
        public string AIModel { get; set; } = string.Empty; // "LSTM", "Prophet", "RandomForest"
    }

    public class MaintenanceRecommendation
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public MaintenancePriority Priority { get; set; }
        public DateTime RecommendedDate { get; set; }
        public TimeSpan EstimatedDuration { get; set; }
        public decimal EstimatedCost { get; set; }
        public double ImpactScore { get; set; }
        public bool IsAutomatable { get; set; }
    }

    public enum HealthStatus
    {
        Excellent = 1,
        Good = 2,
        Warning = 3,
        Critical = 4,
        Emergency = 5
    }

    public enum PredictionSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    public enum MaintenancePriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Urgent = 4,
        Emergency = 5
    }

    #endregion

    #region Intelligent Customer Relationship Models

    public class CustomerIntelligence
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public CustomerSegment Segment { get; set; }
        public double LifetimeValue { get; set; }
        public double ChurnProbability { get; set; }
        public double SatisfactionScore { get; set; }
        public List<BehaviorPattern> BehaviorPatterns { get; set; } = new List<BehaviorPattern>();
        public List<SalesOpportunity> PredictedOpportunities { get; set; } = new List<SalesOpportunity>();
        public List<CommunicationStrategy> RecommendedStrategies { get; set; } = new List<CommunicationStrategy>();
        public EmotionalProfile EmotionalProfile { get; set; }
    }

    public class BehaviorPattern
    {
        public int Id { get; set; }
        public string PatternType { get; set; } = string.Empty; // "Purchase", "Browse", "Communication", "Return"
        public string Description { get; set; } = string.Empty;
        public double Frequency { get; set; }
        public DateTime LastOccurrence { get; set; }
        public double Confidence { get; set; }
        public string AIModelUsed { get; set; } = string.Empty;
    }

    public class SalesOpportunity
    {
        public int Id { get; set; }
        public string ProductCategory { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public double ProbabilityScore { get; set; }
        public decimal EstimatedValue { get; set; }
        public DateTime OptimalContactTime { get; set; }
        public string RecommendedChannel { get; set; } = string.Empty; // "Email", "Phone", "SMS", "InApp"
        public string PersonalizedMessage { get; set; } = string.Empty;
    }

    public class CommunicationStrategy
    {
        public int Id { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string Tone { get; set; } = string.Empty; // "Professional", "Friendly", "Urgent", "Supportive"
        public string Language { get; set; } = string.Empty;
        public TimeSpan OptimalFrequency { get; set; }
        public double EffectivenessScore { get; set; }
    }

    public class EmotionalProfile
    {
        public int Id { get; set; }
        public double Satisfaction { get; set; }
        public double Frustration { get; set; }
        public double Excitement { get; set; }
        public double Trust { get; set; }
        public double Loyalty { get; set; }
        public DateTime LastAnalysis { get; set; }
        public string AnalysisMethod { get; set; } = string.Empty; // "TextSentiment", "VoiceAnalysis", "BehaviorAnalysis"
    }

    public enum CustomerSegment
    {
        VIP = 1,
        Loyal = 2,
        Regular = 3,
        New = 4,
        AtRisk = 5,
        Inactive = 6
    }

    #endregion

    #region AI-Driven Smart Category Intelligence Models

    public class CategoryIntelligence
    {
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string ParentCategory { get; set; } = string.Empty;
        public double PerformanceScore { get; set; }
        public List<AutoClassification> AutoClassifications { get; set; } = new List<AutoClassification>();
        public List<MarketTrend> MarketTrends { get; set; } = new List<MarketTrend>();
        public List<CrossSellingOpportunity> CrossSellingOpportunities { get; set; } = new List<CrossSellingOpportunity>();
        public CategoryOptimization Optimization { get; set; }
    }

    public class AutoClassification
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public List<string> ImageUrls { get; set; } = new List<string>();
        public string SuggestedCategory { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public List<string> ExtractedTags { get; set; } = new List<string>();
        public string ClassificationMethod { get; set; } = string.Empty; // "ComputerVision", "NLP", "Hybrid"
        public DateTime ClassificationDate { get; set; }
        public bool RequiresHumanReview { get; set; }
    }

    public class MarketTrend
    {
        public int Id { get; set; }
        public string TrendName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double TrendStrength { get; set; }
        public TrendDirection Direction { get; set; }
        public DateTime DetectedDate { get; set; }
        public DateTime PredictedEndDate { get; set; }
        public List<string> AffectedCategories { get; set; } = new List<string>();
        public double ImpactScore { get; set; }
        public List<string> DataSources { get; set; } = new List<string>();
    }

    public class CrossSellingOpportunity
    {
        public int Id { get; set; }
        public string PrimaryCategory { get; set; } = string.Empty;
        public string SuggestedCategory { get; set; } = string.Empty;
        public double AffinityScore { get; set; }
        public decimal PotentialRevenue { get; set; }
        public double ConversionProbability { get; set; }
        public string OptimalTiming { get; set; } = string.Empty;
        public List<string> SuccessExamples { get; set; } = new List<string>();
    }

    public class CategoryOptimization
    {
        public int Id { get; set; }
        public string CategoryId { get; set; } = string.Empty;
        public List<string> OptimizationSuggestions { get; set; } = new List<string>();
        public double CurrentPerformance { get; set; }
        public double ProjectedPerformance { get; set; }
        public DateTime OptimizationDate { get; set; }
        public List<string> KeyMetrics { get; set; } = new List<string>();
        public bool ImplementationRequired { get; set; }
    }

    public enum TrendDirection
    {
        Increasing = 1,
        Decreasing = 2,
        Stable = 3,
        Volatile = 4,
        Seasonal = 5
    }

    #endregion

    #region Voice Command & Natural Language Processing Models

    public class VoiceCommand
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string RawAudio { get; set; } = string.Empty;
        public string TranscribedText { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public VoiceIntent Intent { get; set; }
        public List<VoiceEntity> ExtractedEntities { get; set; } = new List<VoiceEntity>();
        public string ExecutedAction { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime ProcessedAt { get; set; }
        public TimeSpan ProcessingDuration { get; set; }
    }

    public class VoiceIntent
    {
        public int Id { get; set; }
        public string IntentName { get; set; } = string.Empty; // "UpdateStock", "FindProduct", "CreateOrder", "GetReport"
        public double Confidence { get; set; }
        public string RequiredAction { get; set; } = string.Empty;
        public List<string> RequiredParameters { get; set; } = new List<string>();
        public bool IsExecutable { get; set; }
        public string FeedbackMessage { get; set; } = string.Empty;
    }

    public class VoiceEntity
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = string.Empty; // "Product", "Quantity", "Location", "Date", "Customer"
        public string Value { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
    }

    public class VoiceAnalytics
    {
        public int Id { get; set; }
        public DateTime AnalysisDate { get; set; }
        public int TotalCommands { get; set; }
        public int SuccessfulCommands { get; set; }
        public double SuccessRate { get; set; }
        public List<string> MostUsedIntents { get; set; } = new List<string>();
        public List<string> FailedIntents { get; set; } = new List<string>();
        public double AverageProcessingTime { get; set; }
        public Dictionary<string, int> LanguageUsage { get; set; } = new Dictionary<string, int>();
        public List<string> ImprovementSuggestions { get; set; } = new List<string>();
    }

    #endregion

    #region AR-Powered 3D Warehouse Navigation Models

    public class ARWarehouseModel
    {
        public int Id { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public Coordinate3D Dimensions { get; set; }
        public List<ARLocation> Locations { get; set; } = new List<ARLocation>();
        public List<ARAsset> Assets { get; set; } = new List<ARAsset>();
        public ARConfiguration Configuration { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsARReady { get; set; }
    }

    public class ARLocation
    {
        public int Id { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Coordinate3D Position { get; set; }
        public Coordinate3D Rotation { get; set; }
        public ARLocationCategory Category { get; set; }
        public List<AROverlay> Overlays { get; set; } = new List<AROverlay>();
        public bool HasInventory { get; set; }
        public int CurrentCapacity { get; set; }
        public int MaxCapacity { get; set; }
    }

    public class ARAsset
    {
        public int Id { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string AssetType { get; set; } = string.Empty;
        public string ModelPath { get; set; } = string.Empty; // Path to 3D model file
        public Coordinate3D Scale { get; set; }
        public List<string> TextureUrls { get; set; } = new List<string>();
        public bool IsInteractive { get; set; }
        public List<ARInteraction> SupportedInteractions { get; set; } = new List<ARInteraction>();
    }

    public class AROverlay
    {
        public int Id { get; set; }
        public string OverlayType { get; set; } = string.Empty; // "Text", "Icon", "Animation", "Hologram"
        public string Content { get; set; } = string.Empty;
        public Coordinate3D Position { get; set; }
        public ARDisplayMode DisplayMode { get; set; }
        public bool IsVisible { get; set; }
        public DateTime ExpirationTime { get; set; }
        public string TriggerCondition { get; set; } = string.Empty;
    }

    public class ARConfiguration
    {
        public int Id { get; set; }
        public string DeviceType { get; set; } = string.Empty; // "HoloLens", "MagicLeap", "ARKit", "ARCore"
        public double TrackingAccuracy { get; set; }
        public bool GestureRecognitionEnabled { get; set; }
        public bool VoiceControlEnabled { get; set; }
        public List<string> SupportedGestures { get; set; } = new List<string>();
        public Dictionary<string, object> CustomSettings { get; set; } = new Dictionary<string, object>();
    }

    public class ARInteraction
    {
        public int Id { get; set; }
        public string InteractionType { get; set; } = string.Empty; // "Tap", "Pinch", "Swipe", "Voice", "Gesture"
        public string TriggerEvent { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ActionParameters { get; set; } = string.Empty;
        public bool RequiresConfirmation { get; set; }
    }

    public class Coordinate3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    public enum ARLocationCategory
    {
        Storage = 1,
        Workstation = 2,
        ReceivingDock = 3,
        ShippingDock = 4,
        QualityControl = 5,
        Office = 6,
        Utility = 7
    }

    public enum ARDisplayMode
    {
        AlwaysVisible = 1,
        ProximityTriggered = 2,
        OnDemand = 3,
        Contextual = 4
    }

    #endregion

    #region Blockchain Supply Chain Transparency Models

    public class BlockchainProduct
    {
        public int Id { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public string BlockchainAddress { get; set; } = string.Empty;
        public string SmartContractAddress { get; set; } = string.Empty;
        public List<SupplyChainEvent> SupplyChainHistory { get; set; } = new List<SupplyChainEvent>();
        public OriginVerification OriginVerification { get; set; }
        public List<QualityAssurance> QualityRecords { get; set; } = new List<QualityAssurance>();
        public CarbonFootprint CarbonFootprint { get; set; }
        public bool IsVerified { get; set; }
        public DateTime BlockchainRegistrationDate { get; set; }
    }

    public class SupplyChainEvent
    {
        public int Id { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty; // "Production", "Quality", "Transfer", "Sale"
        public string ParticipantAddress { get; set; } = string.Empty;
        public string ParticipantName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Location { get; set; } = string.Empty;
        public Dictionary<string, object> EventData { get; set; } = new Dictionary<string, object>();
        public List<string> DocumentHashes { get; set; } = new List<string>();
        public bool IsVerified { get; set; }
    }

    public class OriginVerification
    {
        public int Id { get; set; }
        public string ProducerName { get; set; } = string.Empty;
        public string ProducerAddress { get; set; } = string.Empty;
        public string ProductionLocation { get; set; } = string.Empty;
        public DateTime ProductionDate { get; set; }
        public List<string> RawMaterials { get; set; } = new List<string>();
        public List<string> Certifications { get; set; } = new List<string>();
        public string VerificationMethod { get; set; } = string.Empty;
        public double VerificationScore { get; set; }
        public bool IsAuthentic { get; set; }
    }

    public class QualityAssurance
    {
        public int Id { get; set; }
        public string InspectorName { get; set; } = string.Empty;
        public string InspectorCertification { get; set; } = string.Empty;
        public DateTime InspectionDate { get; set; }
        public QualityGrade Grade { get; set; }
        public List<QualityTest> Tests { get; set; } = new List<QualityTest>();
        public List<string> Defects { get; set; } = new List<string>();
        public bool PassedInspection { get; set; }
        public string BlockchainRecordHash { get; set; } = string.Empty;
    }

    public class QualityTest
    {
        public int Id { get; set; }
        public string TestName { get; set; } = string.Empty;
        public string TestMethod { get; set; } = string.Empty;
        public string ExpectedValue { get; set; } = string.Empty;
        public string ActualValue { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class CarbonFootprint
    {
        public int Id { get; set; }
        public double TotalEmissions { get; set; } // kg CO2
        public double ProductionEmissions { get; set; }
        public double TransportEmissions { get; set; }
        public double PackagingEmissions { get; set; }
        public List<CarbonOffset> Offsets { get; set; } = new List<CarbonOffset>();
        public double NetEmissions { get; set; }
        public string CalculationMethod { get; set; } = string.Empty;
        public DateTime LastCalculated { get; set; }
    }

    public class CarbonOffset
    {
        public int Id { get; set; }
        public string OffsetType { get; set; } = string.Empty; // "Reforestation", "Renewable Energy", "Carbon Capture"
        public double OffsetAmount { get; set; }
        public string VerificationStandard { get; set; } = string.Empty;
        public string CertificateNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
    }

    public class CryptocurrencyPayment
    {
        public int Id { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty; // "BTC", "ETH", "USDT", etc.
        public decimal Amount { get; set; }
        public decimal USDValue { get; set; }
        public string FromAddress { get; set; } = string.Empty;
        public string ToAddress { get; set; } = string.Empty;
        public DateTime TransactionTime { get; set; }
        public int Confirmations { get; set; }
        public PaymentStatus Status { get; set; }
        public string SmartContractAddress { get; set; } = string.Empty;
    }

    public enum QualityGrade
    {
        A = 1,  // Premium
        B = 2,  // Standard
        C = 3,  // Basic
        D = 4,  // Below Standard
        F = 5   // Failed
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Confirmed = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }

    #endregion

    #region Innovation Implementation Support Models

    public class InnovationMetrics
    {
        public int Id { get; set; }
        public DateTime MeasurementDate { get; set; }
        public double AIAccuracy { get; set; }
        public double SystemResponseTime { get; set; }
        public double UserAdoptionRate { get; set; }
        public double SystemUptime { get; set; }
        public double OperationalEfficiency { get; set; }
        public double CostReduction { get; set; }
        public double CustomerSatisfaction { get; set; }
        public double MarketCompetitiveAdvantage { get; set; }
    }

    public class ImplementationPhase
    {
        public int Id { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> Deliverables { get; set; } = new List<string>();
        public List<string> Technologies { get; set; } = new List<string>();
        public PhaseStatus Status { get; set; }
        public double CompletionPercentage { get; set; }
        public List<string> Challenges { get; set; } = new List<string>();
        public List<string> Successes { get; set; } = new List<string>();
    }

    public enum PhaseStatus
    {
        Planning = 1,
        InProgress = 2,
        Testing = 3,
        Completed = 4,
        OnHold = 5,
        Cancelled = 6
    }

    #endregion
}
