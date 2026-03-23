using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract
{
    /// <summary>
    /// Mobil depo yönetimi servisi - QR kod tarama, offline çalışma ve mobil entegrasyon
    /// </summary>
    public interface IMobileWarehouseService
    {
        // QR Kod Tarama ve İşleme
        Task<QRCodeScanResult> ProcessQRCodeScanAsync(string qrCodeContent, string deviceId, string deviceType);
        Task<LocationInfo> GetLocationFromQRCodeAsync(string qrCodeContent);
        Task<ProductInfo> GetProductFromQRCodeAsync(string qrCodeContent);

        // Mobil Cihaz Yönetimi
        Task<bool> RegisterMobileDeviceAsync(MobileDeviceInfo deviceInfo);
        Task<bool> UpdateDeviceLocationAsync(string deviceId, DeviceLocation location);
        Task<List<MobileDeviceInfo>> GetActiveDevicesAsync();
        Task<MobileDeviceInfo> GetDeviceInfoAsync(string deviceId);

        // Offline Çalışma Desteği
        Task<bool> SyncOfflineDataAsync(string deviceId, List<OfflineOperation> operations);
        Task<List<OfflineOperation>> GetPendingOperationsAsync(string deviceId);
        Task<bool> MarkOperationAsSyncedAsync(string operationId);
        Task<OfflineSyncStatus> GetOfflineSyncStatusAsync(string deviceId);

        // Mobil Konum İşlemleri
        Task<bool> PlaceProductFromMobileAsync(MobilePlaceRequest request);
        Task<bool> MoveProductFromMobileAsync(MobileMoveRequest request);
        Task<bool> RemoveProductFromMobileAsync(MobileRemoveRequest request);
        Task<bool> ScanInventoryFromMobileAsync(MobileInventoryScanRequest request);

        // Mobil Raporlama
        Task<MobileDashboardData> GetMobileDashboardAsync(string deviceId);
        Task<List<MobileAlert>> GetMobileAlertsAsync(string deviceId);
        Task<List<MobileTask>> GetAssignedTasksAsync(string deviceId);
        Task<bool> UpdateTaskStatusAsync(string taskId, string status, string notes);

        // Sesli Komut Desteği
        Task<VoiceCommandResult> ProcessVoiceCommandAsync(string voiceCommand, string deviceId);
        Task<List<VoiceCommand>> GetAvailableVoiceCommandsAsync();
        Task<bool> TrainVoiceCommandAsync(string command, string response);

        // Mobil Performans İzleme
        Task<MobilePerformanceMetrics> GetDevicePerformanceAsync(string deviceId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<bool> LogMobileActivityAsync(MobileActivityLog activity);
        Task<List<MobileActivityLog>> GetDeviceActivityHistoryAsync(string deviceId, int limit = 100);
    }

    /// <summary>
    /// QR kod tarama sonucu
    /// </summary>
    public class QRCodeScanResult
    {
        public bool IsSuccess { get; set; }
        public string QRCodeContent { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty; // LOCATION, PRODUCT, MOVEMENT
        public DateTime ScanTime { get; set; } = DateTime.Now;
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public LocationInfo? LocationInfo { get; set; }
        public ProductInfo? ProductInfo { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    /// <summary>
    /// Mobil cihaz bilgisi
    /// </summary>
    public class MobileDeviceInfo
    {
        public string DeviceId { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty; // PHONE, TABLET, SCANNER
        public string OperatingSystem { get; set; } = string.Empty;
        public string AppVersion { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime LastSeen { get; set; } = DateTime.Now;
        public bool IsOnline { get; set; }
        public DeviceLocation? CurrentLocation { get; set; }
        public Dictionary<string, object> DeviceCapabilities { get; set; } = new();
    }

    /// <summary>
    /// Cihaz konumu
    /// </summary>
    public class DeviceLocation
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal? Altitude { get; set; }
        public decimal? Accuracy { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? ZoneCode { get; set; }
        public string? RackCode { get; set; }
        public string? BinCode { get; set; }
    }

    /// <summary>
    /// Offline işlem
    /// </summary>
    public class OfflineOperation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DeviceId { get; set; } = string.Empty;
        public string OperationType { get; set; } = string.Empty; // PLACE, MOVE, REMOVE, SCAN
        public string Status { get; set; } = "PENDING"; // PENDING, SYNCED, FAILED
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? SyncedDate { get; set; }
        public Dictionary<string, object> OperationData { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; } = 0;
    }

    /// <summary>
    /// Offline senkronizasyon durumu
    /// </summary>
    public class OfflineSyncStatus
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTime LastSyncTime { get; set; }
        public int PendingOperations { get; set; }
        public int SyncedOperations { get; set; }
        public int FailedOperations { get; set; }
        public string SyncStatus { get; set; } = "UNKNOWN"; // UNKNOWN, SYNCING, SYNCED, FAILED
        public string? LastErrorMessage { get; set; }
        public TimeSpan AverageSyncTime { get; set; }
        public bool IsOnline { get; set; }
    }

    /// <summary>
    /// Mobil yerleştirme isteği
    /// </summary>
    public class MobilePlaceRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string BinCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.Now;
        public DeviceLocation? DeviceLocation { get; set; }
        public Dictionary<string, object> AdditionalData { get; set; } = new();
    }

    /// <summary>
    /// Mobil taşıma isteği
    /// </summary>
    public class MobileMoveRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string FromBinCode { get; set; } = string.Empty;
        public string ToBinCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.Now;
        public DeviceLocation? DeviceLocation { get; set; }
    }

    /// <summary>
    /// Mobil çıkarma isteği
    /// </summary>
    public class MobileRemoveRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string BinCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string? Reason { get; set; }
        public string? Notes { get; set; }
        public DateTime RequestTime { get; set; } = DateTime.Now;
        public DeviceLocation? DeviceLocation { get; set; }
    }

    /// <summary>
    /// Mobil envanter tarama isteği
    /// </summary>
    public class MobileInventoryScanRequest
    {
        public string DeviceId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string BinCode { get; set; } = string.Empty;
        public List<ScannedProduct> ScannedProducts { get; set; } = new();
        public DateTime ScanTime { get; set; } = DateTime.Now;
        public DeviceLocation? DeviceLocation { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Taranan ürün
    /// </summary>
    public class ScannedProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public int ExpectedQuantity { get; set; }
        public int ActualQuantity { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Mobil dashboard verisi
    /// </summary>
    public class MobileDashboardData
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public MobileUserInfo UserInfo { get; set; } = new();
        public List<MobileTask> AssignedTasks { get; set; } = new();
        public List<MobileAlert> ActiveAlerts { get; set; } = new();
        public MobilePerformanceSummary PerformanceSummary { get; set; } = new();
        public List<QuickAction> QuickActions { get; set; } = new();
    }

    /// <summary>
    /// Mobil kullanıcı bilgisi
    /// </summary>
    public class MobileUserInfo
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public List<string> Permissions { get; set; } = new();
        public DateTime LastLogin { get; set; }
        public string CurrentZone { get; set; } = string.Empty;
        public string CurrentRack { get; set; } = string.Empty;
    }

    /// <summary>
    /// Mobil görev
    /// </summary>
    public class MobileTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // PLACE, MOVE, REMOVE, SCAN, COUNT
        public string Priority { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH, CRITICAL
        public string Status { get; set; } = "ASSIGNED"; // ASSIGNED, IN_PROGRESS, COMPLETED, CANCELLED
        public DateTime AssignedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string AssignedTo { get; set; } = string.Empty;
        public string? ZoneCode { get; set; }
        public string? RackCode { get; set; }
        public string? BinCode { get; set; }
        public Dictionary<string, object> TaskData { get; set; } = new();
        public List<string> Prerequisites { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
    }

    /// <summary>
    /// Mobil uyarı
    /// </summary>
    public class MobileAlert
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // INFO, WARNING, ERROR, CRITICAL
        public string Severity { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH, CRITICAL
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ExpiryDate { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime? ReadDate { get; set; }
        public string? ActionRequired { get; set; }
        public List<string> AffectedAreas { get; set; } = new();
        public Dictionary<string, object> AlertData { get; set; } = new();
    }

    /// <summary>
    /// Mobil performans özeti
    /// </summary>
    public class MobilePerformanceSummary
    {
        public DateTime Date { get; set; } = DateTime.Today;
        public int TasksCompleted { get; set; }
        public int TasksInProgress { get; set; }
        public int TasksOverdue { get; set; }
        public decimal CompletionRate { get; set; } // 0-100 arası
        public TimeSpan AverageTaskTime { get; set; }
        public int ProductsProcessed { get; set; }
        public int MovementsCompleted { get; set; }
        public int ScansCompleted { get; set; }
        public List<PerformanceMetric> KeyMetrics { get; set; } = new();
    }

    /// <summary>
    /// Hızlı aksiyon
    /// </summary>
    public class QuickAction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty; // NAVIGATE, SCAN, REPORT, SETTINGS
        public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public Dictionary<string, object> ActionData { get; set; } = new();
    }

    /// <summary>
    /// Sesli komut sonucu
    /// </summary>
    public class VoiceCommandResult
    {
        public bool IsSuccess { get; set; }
        public string OriginalCommand { get; set; } = string.Empty;
        public string RecognizedCommand { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Response { get; set; } = string.Empty;
        public decimal Confidence { get; set; } // 0-100 arası güven seviyesi
        public DateTime ProcessedTime { get; set; } = DateTime.Now;
        public string DeviceId { get; set; } = string.Empty;
        public Dictionary<string, object> ExtractedData { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Sesli komut
    /// </summary>
    public class VoiceCommand
    {
        public string Command { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public List<string> Aliases { get; set; } = new();
        public List<string> RequiredPermissions { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string? HelpText { get; set; }
        public string? Example { get; set; }
    }

    /// <summary>
    /// Mobil performans metrikleri
    /// </summary>
    public class MobilePerformanceMetrics
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public List<DailyPerformance> DailyPerformance { get; set; } = new();
        public List<HourlyPerformance> HourlyPerformance { get; set; } = new();
        public PerformanceSummary Summary { get; set; } = new();
        public List<PerformanceTrend> Trends { get; set; } = new();
    }

    /// <summary>
    /// Günlük performans
    /// </summary>
    public class DailyPerformance
    {
        public DateTime Date { get; set; }
        public int TasksCompleted { get; set; }
        public int TasksAssigned { get; set; }
        public decimal CompletionRate { get; set; }
        public TimeSpan AverageTaskTime { get; set; }
        public int ProductsProcessed { get; set; }
        public int MovementsCompleted { get; set; }
        public int ScansCompleted { get; set; }
        public TimeSpan TotalWorkingTime { get; set; }
        public TimeSpan TotalIdleTime { get; set; }
    }

    /// <summary>
    /// Saatlik performans
    /// </summary>
    public class HourlyPerformance
    {
        public int Hour { get; set; } // 0-23
        public int TasksCompleted { get; set; }
        public int ProductsProcessed { get; set; }
        public int MovementsCompleted { get; set; }
        public int ScansCompleted { get; set; }
        public decimal Efficiency { get; set; } // 0-100 arası
        public TimeSpan AverageTaskTime { get; set; }
    }

    /// <summary>
    /// Performans özeti
    /// </summary>
    public class PerformanceSummary
    {
        public int TotalTasksCompleted { get; set; }
        public int TotalTasksAssigned { get; set; }
        public decimal OverallCompletionRate { get; set; }
        public TimeSpan AverageTaskTime { get; set; }
        public int TotalProductsProcessed { get; set; }
        public int TotalMovementsCompleted { get; set; }
        public int TotalScansCompleted { get; set; }
        public TimeSpan TotalWorkingTime { get; set; }
        public TimeSpan TotalIdleTime { get; set; }
        public decimal OverallEfficiency { get; set; }
    }

    /// <summary>
    /// Performans trendi
    /// </summary>
    public class PerformanceTrend
    {
        public string Metric { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty; // INCREASING, DECREASING, STABLE
        public decimal ChangeRate { get; set; }
        public DateTime TrendStartDate { get; set; }
        public DateTime TrendEndDate { get; set; }
        public string Confidence { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH
        public List<string> ContributingFactors { get; set; } = new();
    }

    /// <summary>
    /// Mobil aktivite logu
    /// </summary>
    public class MobileActivityLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string DeviceId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty; // LOGIN, LOGOUT, TASK_START, TASK_COMPLETE, SCAN, MOVE
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public DeviceLocation? DeviceLocation { get; set; }
        public Dictionary<string, object> ActivityData { get; set; } = new();
        public string? Result { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}
