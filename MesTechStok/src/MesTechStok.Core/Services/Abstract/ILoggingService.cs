using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MesTechStok.Core.Data.Models;

namespace MesTechStok.Core.Services.Abstract
{
    public interface ILoggingService
    {
        // Temel logging metodları
        Task LogInfoAsync(string message, string category = "General", object? data = null, string? userId = null);
        Task LogWarningAsync(string message, string category = "General", object? data = null, string? userId = null);
        Task LogErrorAsync(string message, Exception? exception = null, string category = "General", object? data = null, string? userId = null);
        Task LogDebugAsync(string message, string category = "General", object? data = null, string? userId = null);

        // Özel logging kategorileri
        Task LogProductOperationAsync(string operation, string productName, string? barcode = null, object? data = null, string? userId = null);
        Task LogUserActionAsync(string action, string? userId = null, object? data = null);
        Task LogSystemEventAsync(string eventName, string message, object? data = null);
        Task LogSecurityEventAsync(string securityEvent, string message, string? userId = null, object? data = null);
        Task LogPerformanceAsync(string operation, TimeSpan duration, object? data = null);

        // Log okuma
        Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime? startDate = null, DateTime? endDate = null, string? category = null, int page = 1, int pageSize = 50);
        Task<IEnumerable<LogEntry>> GetUserLogsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50);
        Task<IEnumerable<LogEntry>> GetProductLogsAsync(string? productName = null, string? barcode = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50);

        // Log temizleme ve yönetim
        Task CleanOldLogsAsync(int daysToKeep = 90);
        Task<long> GetLogCountAsync(string? category = null);
    }
}
