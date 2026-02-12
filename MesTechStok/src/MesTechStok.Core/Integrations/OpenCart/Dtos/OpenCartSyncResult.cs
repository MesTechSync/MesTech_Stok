using System;
using System.Collections.Generic;

namespace MesTechStok.Core.Integrations.OpenCart.Dtos
{
    public class OpenCartSyncResult
    {
        // Canonical properties
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalProcessed { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string SyncType { get; set; } = string.Empty;

        // Backward compatibility (legacy in-code usage expects these names)
        public bool IsSuccess { get => Success; set => Success = value; }
        public int ErrorCount { get => FailureCount; set => FailureCount = value; }
        public DateTime SyncDate { get => StartTime; set => StartTime = value; }

        public static OpenCartSyncResult CreateSuccess(string syncType, int totalProcessed, int successCount)
        {
            return new OpenCartSyncResult
            {
                Success = true,
                Message = $"{syncType} senkronizasyonu başarılı",
                TotalProcessed = totalProcessed,
                SuccessCount = successCount,
                FailureCount = totalProcessed - successCount,
                SyncType = syncType,
                EndTime = DateTime.Now
            };
        }

        public static OpenCartSyncResult CreateFailure(string syncType, string message, List<string>? errors = null)
        {
            return new OpenCartSyncResult
            {
                Success = false,
                Message = message,
                SyncType = syncType,
                Errors = errors ?? new List<string>(),
                EndTime = DateTime.Now
            };
        }
    }
}
