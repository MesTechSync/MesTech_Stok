using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Core.Services.Abstract;
using Microsoft.Extensions.Logging;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// Safe fallback implementation to allow the app to start when Core's concrete service is excluded.
    /// Provides no-op behavior returning defaults.
    /// </summary>
    public class MockMobileWarehouseService : IMobileWarehouseService
    {
        private readonly ILogger<MockMobileWarehouseService>? _logger;

        public MockMobileWarehouseService(ILogger<MockMobileWarehouseService>? logger = null)
        {
            _logger = logger;
        }

        // QR Kod Tarama ve İşleme
        public Task<QRCodeScanResult> ProcessQRCodeScanAsync(string qrCodeContent, string deviceId, string deviceType)
        {
            _logger?.LogInformation("[Mock] ProcessQRCodeScanAsync invoked");
            return Task.FromResult(new QRCodeScanResult
            {
                IsSuccess = true,
                QRCodeContent = qrCodeContent,
                DeviceId = deviceId,
                DeviceType = deviceType,
                ContentType = string.Empty,
                ScanTime = DateTime.Now,
                LocationInfo = null,
                ProductInfo = null
            });
        }

        public Task<LocationInfo> GetLocationFromQRCodeAsync(string qrCodeContent)
            => Task.FromResult<LocationInfo>(null);

        public Task<ProductInfo> GetProductFromQRCodeAsync(string qrCodeContent)
            => Task.FromResult<ProductInfo>(null);

        // Mobil Cihaz Yönetimi
        public Task<bool> RegisterMobileDeviceAsync(MobileDeviceInfo deviceInfo)
            => Task.FromResult(true);

        public Task<bool> UpdateDeviceLocationAsync(string deviceId, DeviceLocation location)
            => Task.FromResult(true);

        public Task<List<MobileDeviceInfo>> GetActiveDevicesAsync()
            => Task.FromResult(new List<MobileDeviceInfo>());

        public Task<MobileDeviceInfo> GetDeviceInfoAsync(string deviceId)
            => Task.FromResult<MobileDeviceInfo>(null);

        // Offline Çalışma Desteği
        public Task<bool> SyncOfflineDataAsync(string deviceId, List<OfflineOperation> operations)
            => Task.FromResult(true);

        public Task<List<OfflineOperation>> GetPendingOperationsAsync(string deviceId)
            => Task.FromResult(new List<OfflineOperation>());

        public Task<bool> MarkOperationAsSyncedAsync(string operationId)
            => Task.FromResult(true);

        public Task<OfflineSyncStatus> GetOfflineSyncStatusAsync(string deviceId)
            => Task.FromResult(new OfflineSyncStatus
            {
                DeviceId = deviceId,
                LastSyncTime = DateTime.Now,
                PendingOperations = 0,
                SyncedOperations = 0,
                FailedOperations = 0,
                SyncStatus = "IDLE",
                AverageSyncTime = TimeSpan.Zero,
                IsOnline = true
            });

        // Mobil Konum İşlemleri
        public Task<bool> PlaceProductFromMobileAsync(MobilePlaceRequest request)
            => Task.FromResult(true);

        public Task<bool> MoveProductFromMobileAsync(MobileMoveRequest request)
            => Task.FromResult(true);

        public Task<bool> RemoveProductFromMobileAsync(MobileRemoveRequest request)
            => Task.FromResult(true);

        public Task<bool> ScanInventoryFromMobileAsync(MobileInventoryScanRequest request)
            => Task.FromResult(true);

        // Mobil Raporlama
        public Task<MobileDashboardData> GetMobileDashboardAsync(string deviceId)
            => Task.FromResult(new MobileDashboardData());

        public Task<List<MobileAlert>> GetMobileAlertsAsync(string deviceId)
            => Task.FromResult(new List<MobileAlert>());

        public Task<List<MobileTask>> GetAssignedTasksAsync(string deviceId)
            => Task.FromResult(new List<MobileTask>());

        public Task<bool> UpdateTaskStatusAsync(string taskId, string status, string notes)
            => Task.FromResult(true);

        // Sesli Komut Desteği
        public Task<VoiceCommandResult> ProcessVoiceCommandAsync(string voiceCommand, string deviceId)
            => Task.FromResult(new VoiceCommandResult
            {
                IsSuccess = false,
                OriginalCommand = voiceCommand,
                RecognizedCommand = voiceCommand,
                Action = string.Empty,
                Response = "Mock response",
                Confidence = 0,
                ProcessedTime = DateTime.Now,
                DeviceId = deviceId
            });

        public Task<List<VoiceCommand>> GetAvailableVoiceCommandsAsync()
            => Task.FromResult(new List<VoiceCommand>());

        public Task<bool> TrainVoiceCommandAsync(string command, string response)
            => Task.FromResult(true);

        // Mobil Performans İzleme
        public Task<MobilePerformanceMetrics> GetDevicePerformanceAsync(string deviceId, DateTime? fromDate = null, DateTime? toDate = null)
            => Task.FromResult(new MobilePerformanceMetrics());

        public Task<bool> LogMobileActivityAsync(MobileActivityLog activity)
            => Task.FromResult(true);

        public Task<List<MobileActivityLog>> GetDeviceActivityHistoryAsync(string deviceId, int limit = 100)
            => Task.FromResult(new List<MobileActivityLog>());
    }
}
