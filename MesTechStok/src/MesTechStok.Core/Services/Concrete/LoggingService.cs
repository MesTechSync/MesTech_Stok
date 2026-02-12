using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MesTechStok.Core.Data;
using MesTechStok.Core.Data.Models;
using MesTechStok.Core.Services.Abstract;

namespace MesTechStok.Core.Services.Concrete;

/// <summary>
/// Kapsamlı logging servisi implementasyonu
/// Tüm sistem işlemlerini, ürün operasyonlarını, kullanıcı eylemlerini ve hataları kaydeder
/// </summary>
public class LoggingService : ILoggingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<LoggingService> _logger;

    public LoggingService(AppDbContext context, ILogger<LoggingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task LogInfoAsync(string message, string category = "General", object? data = null, string? userId = null)
    {
        await CreateLogEntryAsync("Info", category, message, data, userId);
    }

    public async Task LogWarningAsync(string message, string category = "General", object? data = null, string? userId = null)
    {
        await CreateLogEntryAsync("Warning", category, message, data, userId);
    }

    public async Task LogErrorAsync(string message, Exception? exception = null, string category = "General", object? data = null, string? userId = null)
    {
        await CreateLogEntryAsync("Error", category, message, data, userId, exception);
    }

    public async Task LogDebugAsync(string message, string category = "General", object? data = null, string? userId = null)
    {
        await CreateLogEntryAsync("Debug", category, message, data, userId);
    }

    public async Task LogProductOperationAsync(string operation, string productName, string? barcode = null, object? data = null, string? userId = null)
    {
        var logData = new
        {
            Operation = operation,
            ProductName = productName,
            Barcode = barcode,
            AdditionalData = data,
            Timestamp = DateTime.UtcNow
        };

        var message = $"Ürün İşlemi: {operation} - {productName}" + (barcode != null ? $" (Barkod: {barcode})" : "");
        await CreateLogEntryAsync("Info", "Product", message, logData, userId);
    }

    public async Task LogUserActionAsync(string action, string? userId = null, object? data = null)
    {
        var logData = new
        {
            Action = action,
            UserId = userId,
            AdditionalData = data,
            Timestamp = DateTime.UtcNow
        };

        var message = $"Kullanıcı Eylemi: {action}" + (userId != null ? $" (Kullanıcı: {userId})" : "");
        await CreateLogEntryAsync("Info", "User", message, logData, userId);
    }

    public async Task LogSystemEventAsync(string eventName, string message, object? data = null)
    {
        var logData = new
        {
            EventName = eventName,
            SystemInfo = new
            {
                MachineName = Environment.MachineName,
                OSVersion = Environment.OSVersion.ToString(),
                ProcessId = Environment.ProcessId
            },
            AdditionalData = data,
            Timestamp = DateTime.UtcNow
        };

        var fullMessage = $"Sistem Olayı: {eventName} - {message}";
        await CreateLogEntryAsync("Info", "System", fullMessage, logData);
    }

    public async Task LogSecurityEventAsync(string securityEvent, string message, string? userId = null, object? data = null)
    {
        var logData = new
        {
            SecurityEvent = securityEvent,
            UserId = userId,
            SecurityContext = new
            {
                Timestamp = DateTime.UtcNow,
                MachineName = Environment.MachineName,
                ProcessName = Environment.ProcessPath
            },
            AdditionalData = data
        };

        var fullMessage = $"Güvenlik Olayı: {securityEvent} - {message}";
        await CreateLogEntryAsync("Warning", "Security", fullMessage, logData, userId);
    }

    public async Task LogPerformanceAsync(string operation, TimeSpan duration, object? data = null)
    {
        var logData = new
        {
            Operation = operation,
            DurationMs = duration.TotalMilliseconds,
            DurationFormatted = duration.ToString(@"mm\:ss\.fff"),
            PerformanceMetrics = new
            {
                WorkingSet = GC.GetTotalMemory(false),
                Gen0Collections = GC.CollectionCount(0),
                Gen1Collections = GC.CollectionCount(1),
                Gen2Collections = GC.CollectionCount(2)
            },
            AdditionalData = data,
            Timestamp = DateTime.UtcNow
        };

        var message = $"Performans: {operation} - {duration.TotalMilliseconds:F2}ms";
        await CreateLogEntryAsync("Info", "Performance", message, logData);
    }

    public async Task<IEnumerable<LogEntry>> GetLogsAsync(DateTime? startDate = null, DateTime? endDate = null, string? category = null, int page = 1, int pageSize = 50)
    {
        var query = _context.LogEntries.AsQueryable();

        if (startDate.HasValue)
            query = query.Where(l => l.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.Timestamp <= endDate.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(l => l.Category == category);

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<LogEntry>> GetUserLogsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50)
    {
        var query = _context.LogEntries
            .Where(l => l.UserId == userId);

        if (startDate.HasValue)
            query = query.Where(l => l.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.Timestamp <= endDate.Value);

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<LogEntry>> GetProductLogsAsync(string? productName = null, string? barcode = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 50)
    {
        var query = _context.LogEntries
            .Where(l => l.Category == "Product");

        if (!string.IsNullOrWhiteSpace(productName))
            query = query.Where(l => l.Message.Contains(productName));

        if (!string.IsNullOrWhiteSpace(barcode))
            query = query.Where(l => l.Message.Contains(barcode));

        if (startDate.HasValue)
            query = query.Where(l => l.Timestamp >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.Timestamp <= endDate.Value);

        return await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task CleanOldLogsAsync(int daysToKeep = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

        var oldLogs = await _context.LogEntries
            .Where(l => l.Timestamp < cutoffDate)
            .ToListAsync();

        if (oldLogs.Any())
        {
            _context.LogEntries.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            await LogSystemEventAsync("LogCleanup",
                $"Eski loglar temizlendi. {oldLogs.Count} kayıt silindi.",
                new { DaysToKeep = daysToKeep, DeletedCount = oldLogs.Count });
        }
    }

    public async Task<long> GetLogCountAsync(string? category = null)
    {
        var query = _context.LogEntries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(l => l.Category == category);

        return await query.CountAsync();
    }

    private async Task CreateLogEntryAsync(string level, string category, string message, object? data = null, string? userId = null, Exception? exception = null)
    {
        try
        {
            var logEntry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = level,
                Category = category,
                Message = message,
                Data = data != null ? JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }) : null,
                UserId = userId,
                Exception = exception?.ToString(),
                MachineName = Environment.MachineName,
                CreatedDate = DateTime.UtcNow
            };

            _context.LogEntries.Add(logEntry);
            await _context.SaveChangesAsync();

            // Aynı zamanda standart .NET logging'e de yaz
            LogLevel logLevel = level switch
            {
                "Error" => LogLevel.Error,
                "Warning" => LogLevel.Warning,
                "Debug" => LogLevel.Debug,
                _ => LogLevel.Information
            };

            _logger.Log(logLevel, "[{Category}] {Message}", category, message);

            if (exception != null)
            {
                _logger.LogError(exception, "[{Category}] {Message}", category, message);
            }
        }
        catch (Exception ex)
        {
            // Fallback logging - eğer database'e yazamıyorsak en azından console'a yaz
            _logger.LogError(ex, "Logging servisi hatası - Log kaydedilemedi: {Message}", message);
#if DEBUG
            Console.WriteLine($"[LOGGING ERROR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - {level} - {category} - {message}");
            if (exception != null)
            {
                Console.WriteLine($"[ORIGINAL EXCEPTION] {exception}");
            }
            Console.WriteLine($"[LOGGING EXCEPTION] {ex}");
#endif
        }
    }
}
