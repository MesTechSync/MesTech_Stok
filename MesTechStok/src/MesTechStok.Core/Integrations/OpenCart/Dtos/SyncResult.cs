using System;
using System.Collections.Generic;

namespace MesTechStok.Core.Integrations.OpenCart.Dtos
{
    public class SyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public int SuccessCount { get; set; }
        public int FailureCount { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;

        public static SyncResult CreateSuccess(int processedCount, int successCount)
        {
            return new SyncResult
            {
                Success = true,
                Message = "Senkronizasyon başarılı",
                ProcessedCount = processedCount,
                SuccessCount = successCount,
                FailureCount = processedCount - successCount,
                EndTime = DateTime.Now
            };
        }

        public static SyncResult Failure(string message, List<string>? errors = null)
        {
            return new SyncResult
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<string>(),
                EndTime = DateTime.Now
            };
        }
    }
}
