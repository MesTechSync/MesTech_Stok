using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MesTechStok.Desktop.Utils
{
    /// <summary>
    /// 🚨 ACİL LOG ANALİZÖRÜ - Türkçe karakter ve hata filtreleme sistemi
    /// AI Command Template uygulaması: "Ezbere değil, bağlama uygun ve bilinçli yaz"
    /// </summary>
    public static class LogAnalyzer
    {
        private static readonly Regex ErrorPattern = new Regex(
            @"\[(?<timestamp>\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2})\]\s\[(?<level>ERROR|FATAL|CRITICAL)\]\s\[(?<source>.*?)\]\s(?<message>.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TurkishCharPattern = new Regex(
            @"[ğĞıİöÖüÜşŞçÇ]",
            RegexOptions.Compiled);

        public class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Level { get; set; } = "";
            public string Source { get; set; } = "";
            public string Message { get; set; } = "";
            public bool HasTurkishCharacterIssue { get; set; }
            public string RawLine { get; set; } = "";
        }

        /// <summary>
        /// Kritik hataları filtreler ve UTF-8 encoding sorunlarını tespit eder
        /// </summary>
        public static IEnumerable<LogEntry> FilterCriticalErrors(string logPath)
        {
            if (!File.Exists(logPath))
                throw new FileNotFoundException($"Log dosyası bulunamadı: {logPath}");

            var lines = File.ReadAllLines(logPath, Encoding.UTF8);
            var entries = new List<LogEntry>();

            foreach (var line in lines)
            {
                // Sadece ERROR, FATAL, CRITICAL seviyelerini al
                if (!line.Contains("[ERROR]") && !line.Contains("[FATAL]") && !line.Contains("[CRITICAL]"))
                    continue;

                var entry = ParseLogEntry(line);
                entries.Add(entry);
            }

            return entries.OrderByDescending(e => e.Timestamp);
        }

        /// <summary>
        /// Türkçe karakter bozukluğunu tespit eder
        /// </summary>
        public static IEnumerable<LogEntry> FindTurkishCharacterIssues(string logPath)
        {
            if (!File.Exists(logPath))
                throw new FileNotFoundException($"Log dosyası bulunamadı: {logPath}");

            var lines = File.ReadAllLines(logPath, Encoding.UTF8);
            var problematicEntries = new List<LogEntry>();

            foreach (var line in lines)
            {
                // Bozuk Türkçe karakter kalıplarını ara
                if (line.Contains("Ã") || line.Contains("ğŸ") || line.Contains("Ä±") ||
                    line.Contains("Ã¼") || line.Contains("ÅŸ") || line.Contains("Ã§"))
                {
                    var entry = new LogEntry
                    {
                        RawLine = line,
                        HasTurkishCharacterIssue = true,
                        Message = "Türkçe karakter bozukluğu tespit edildi",
                        Level = "WARNING",
                        Source = "LogAnalyzer"
                    };
                    problematicEntries.Add(entry);
                }
            }

            return problematicEntries;
        }

        /// <summary>
        /// En sık görülen hataları gruplar
        /// </summary>
        public static Dictionary<string, int> GetErrorFrequency(string logPath, int topN = 10)
        {
            var errors = FilterCriticalErrors(logPath);
            return errors
                .GroupBy(e => ExtractErrorType(e.Message))
                .OrderByDescending(g => g.Count())
                .Take(topN)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Günlük rapor oluşturur
        /// </summary>
        public static string GenerateDailyReport(string logPath)
        {
            var report = new StringBuilder();
            report.AppendLine($"📊 GÜNLÜK LOG ANALİZ RAPORU - {DateTime.Now:dd.MM.yyyy HH:mm}");
            report.AppendLine("=" + new string('=', 50));

            try
            {
                var errors = FilterCriticalErrors(logPath).ToList();
                var turkishIssues = FindTurkishCharacterIssues(logPath).ToList();
                var errorFreq = GetErrorFrequency(logPath);

                report.AppendLine($"🔴 Toplam Kritik Hata: {errors.Count}");
                report.AppendLine($"🇹🇷 Türkçe Karakter Sorunu: {turkishIssues.Count}");
                report.AppendLine();

                report.AppendLine("📈 EN SIK GÖRÜLEN HATALAR:");
                foreach (var error in errorFreq.Take(5))
                {
                    report.AppendLine($"   • {error.Key}: {error.Value} kez");
                }
                report.AppendLine();

                if (turkishIssues.Any())
                {
                    report.AppendLine("⚠️ TÜRKÇE KARAKTER SORUNLARI:");
                    foreach (var issue in turkishIssues.Take(3))
                    {
                        report.AppendLine($"   • {issue.RawLine.Substring(0, Math.Min(100, issue.RawLine.Length))}...");
                    }
                    report.AppendLine();
                }

                report.AppendLine("✅ ÖNERİLER:");
                if (turkishIssues.Any())
                    report.AppendLine("   • UTF-8 encoding zorla uygulanmalı");
                if (errors.Any(e => e.Message.Contains("OfflineQueue")))
                    report.AppendLine("   • OfflineQueue tablosu kontrol edilmeli");
                if (errors.Any(e => e.Message.Contains("Users")))
                    report.AppendLine("   • Users tablosu migration problemi var");
            }
            catch (Exception ex)
            {
                report.AppendLine($"❌ Rapor oluşturma hatası: {ex.Message}");
            }

            return report.ToString();
        }

        private static LogEntry ParseLogEntry(string line)
        {
            var match = ErrorPattern.Match(line);
            if (match.Success)
            {
                return new LogEntry
                {
                    Timestamp = DateTime.TryParse(match.Groups["timestamp"].Value, out var ts) ? ts : DateTime.MinValue,
                    Level = match.Groups["level"].Value,
                    Source = match.Groups["source"].Value,
                    Message = match.Groups["message"].Value,
                    HasTurkishCharacterIssue = HasTurkishCharacterIssue(line),
                    RawLine = line
                };
            }

            return new LogEntry
            {
                RawLine = line,
                Message = line,
                HasTurkishCharacterIssue = HasTurkishCharacterIssue(line),
                Level = "UNKNOWN",
                Source = "Unknown"
            };
        }

        private static bool HasTurkishCharacterIssue(string text)
        {
            return text.Contains("Ã") || text.Contains("ğŸ") || text.Contains("Ä±") ||
                   text.Contains("Ã¼") || text.Contains("ÅŸ") || text.Contains("Ã§");
        }

        private static string ExtractErrorType(string errorMessage)
        {
            if (errorMessage.Contains("Invalid object name"))
                return "Veritabanı Tablo Eksik";
            if (errorMessage.Contains("Could not find file"))
                return "Dosya Bulunamadı";
            if (errorMessage.Contains("Access to the path") && errorMessage.Contains("denied"))
                return "Dosya Erişim İzni";
            if (errorMessage.Contains("Login failed"))
                return "Veritabanı Bağlantısı";

            return errorMessage.Length > 50 ? errorMessage.Substring(0, 50) + "..." : errorMessage;
        }
    }
}
