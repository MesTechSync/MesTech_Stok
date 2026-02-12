using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// Log analiz ve filtreleme sistemi
    /// ACİL LOG İYİLEŞTİRME RAPORU - Kritik hata filtreleme
    /// </summary>
    public static class LogAnalyzer
    {
        private static readonly Regex LogLineRegex = new Regex(
            @"^(?<timestamp>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3} [+-]\d{2}:\d{2}) \[(?<level>\w{3})\] (?<message>.*)$",
            RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>
        /// Kritik hataları filtrele ve analiz et
        /// </summary>
        public static IEnumerable<LogEntry> FilterCriticalErrors(string logPath)
        {
            if (!File.Exists(logPath))
                return Enumerable.Empty<LogEntry>();

            try
            {
                var lines = File.ReadAllLines(logPath, Encoding.UTF8);
                return lines
                    .Where(line => IsCriticalError(line))
                    .Select(ParseLogEntry)
                    .Where(entry => entry != null)
                    .Cast<LogEntry>();
            }
            catch (Exception ex)
            {
                // Fallback: en azından exception'ı logla
                return new[] { new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = "ERR",
                    Message = $"Log analiz hatası: {ex.Message}",
                    Exception = ex.ToString()
                }};
            }
        }

        /// <summary>
        /// Log satırının kritik hata içerip içermediğini kontrol et
        /// </summary>
        private static bool IsCriticalError(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return false;

            return line.Contains("[ERR]") ||
                   line.Contains("[FATAL]") ||
                   line.Contains("Invalid object name") ||
                   line.Contains("Could not find file") ||
                   line.Contains("OfflineQueue") ||
                   line.Contains("Path injection") ||
                   line.Contains("UnauthorizedAccess");
        }

        /// <summary>
        /// Log satırını LogEntry nesnesine çevir
        /// </summary>
        private static LogEntry? ParseLogEntry(string line)
        {
            try
            {
                var match = LogLineRegex.Match(line);
                if (!match.Success)
                {
                    // Basit format fallback
                    return new LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Level = "UNK",
                        Message = line,
                        Exception = null
                    };
                }

                return new LogEntry
                {
                    Timestamp = DateTime.Parse(match.Groups["timestamp"].Value.Split(' ')[0] + " " +
                                             match.Groups["timestamp"].Value.Split(' ')[1].Split('.')[0]),
                    Level = match.Groups["level"].Value,
                    Message = match.Groups["message"].Value,
                    Exception = null
                };
            }
            catch
            {
                // Parse hatası durumunda basit entry döndür
                return new LogEntry
                {
                    Timestamp = DateTime.Now,
                    Level = "ERR",
                    Message = "Log parse hatası: " + line,
                    Exception = null
                };
            }
        }

        /// <summary>
        /// Son N gündeki kritik hataları getir
        /// </summary>
        public static IEnumerable<LogEntry> GetRecentCriticalErrors(int days = 7)
        {
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
                return Enumerable.Empty<LogEntry>();

            var cutoffDate = DateTime.Now.AddDays(-days);
            var logFiles = Directory.GetFiles(logDir, "mestech-*.log")
                .Where(f => File.GetLastWriteTime(f) >= cutoffDate);

            var allEntries = new List<LogEntry>();
            foreach (var logFile in logFiles)
            {
                allEntries.AddRange(FilterCriticalErrors(logFile));
            }

            return allEntries
                .Where(e => e.Timestamp >= cutoffDate)
                .OrderByDescending(e => e.Timestamp);
        }

        /// <summary>
        /// Kritik hata istatistikleri
        /// </summary>
        public static LogStats GetErrorStats(int days = 1)
        {
            var errors = GetRecentCriticalErrors(days).ToList();

            return new LogStats
            {
                TotalCriticalErrors = errors.Count,
                OfflineQueueErrors = errors.Count(e => e.Message.Contains("OfflineQueue")),
                ImageStorageErrors = errors.Count(e => e.Message.Contains("Could not find file") ||
                                                       e.Message.Contains("ImageStorage")),
                EncodingErrors = errors.Count(e => e.Message.Contains("encoding") ||
                                                   e.Message.Contains("Ã") ||
                                                   e.Message.Contains("ğŸ")),
                PathInjectionAttempts = errors.Count(e => e.Message.Contains("Path injection") ||
                                                          e.Message.Contains("UnauthorizedAccess"))
            };
        }
    }

    /// <summary>
    /// Log giriş modeli
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
    }

    /// <summary>
    /// Log istatistik modeli
    /// </summary>
    public class LogStats
    {
        public int TotalCriticalErrors { get; set; }
        public int OfflineQueueErrors { get; set; }
        public int ImageStorageErrors { get; set; }
        public int EncodingErrors { get; set; }
        public int PathInjectionAttempts { get; set; }

        /// <summary>
        /// Kritik seviye kontrolü (rapordaki hedeflere göre)
        /// </summary>
        public bool IsHealthy => TotalCriticalErrors < 5 && PathInjectionAttempts == 0;
    }
}
