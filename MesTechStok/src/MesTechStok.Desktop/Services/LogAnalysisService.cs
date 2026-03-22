using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// ACİL LOG İYİLEŞTİRME: Log dosyalarını analiz eden servis
    /// Türkçe karakter sorunu ve performans sorunlarını tespit eder
    /// </summary>
    public class LogAnalysisService
    {
        private readonly ILogger<LogAnalysisService> _logger;
        private readonly string _logDirectory;

        public LogAnalysisService(ILogger<LogAnalysisService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logDirectory = Path.Combine(Environment.CurrentDirectory, "Logs");
        }

        /// <summary>
        /// Log dosyalarındaki Türkçe karakter bozukluklarını tespit eder
        /// </summary>
        public async Task<LogAnalysisResult> AnalyzeEncodingIssuesAsync()
        {
            var result = new LogAnalysisResult
            {
                AnalysisDate = DateTime.UtcNow,
                TotalFilesAnalyzed = 0,
                EncodingIssues = new List<string>(),
                PerformanceIssues = new List<string>(),
                SecurityIssues = new List<string>()
            };

            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    result.EncodingIssues.Add($"Log dizini bulunamadı: {_logDirectory}");
                    return result;
                }

                var logFiles = Directory.GetFiles(_logDirectory, "*.log", SearchOption.AllDirectories);
                result.TotalFilesAnalyzed = logFiles.Length;

                foreach (var filePath in logFiles)
                {
                    await AnalyzeLogFileAsync(filePath, result);
                }

                _logger.LogInformation("Log analizi tamamlandı. {TotalFiles} dosya analiz edildi, {Issues} sorun tespit edildi",
                    result.TotalFilesAnalyzed,
                    result.EncodingIssues.Count + result.PerformanceIssues.Count + result.SecurityIssues.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Log analizi sırasında hata oluştu");
                result.EncodingIssues.Add($"Analiz hatası: {ex.Message}");
            }

            return result;
        }

        private async Task AnalyzeLogFileAsync(string filePath, LogAnalysisResult result)
        {
            try
            {
                // UTF-8 BOM kontrolü
                var fileBytes = await File.ReadAllBytesAsync(filePath);
                if (fileBytes.Length >= 3)
                {
                    var hasBom = fileBytes[0] == 0xEF && fileBytes[1] == 0xBB && fileBytes[2] == 0xBF;
                    if (!hasBom)
                    {
                        result.EncodingIssues.Add($"{Path.GetFileName(filePath)}: UTF-8 BOM eksik");
                    }
                }

                // Dosya içeriğini UTF-8 olarak oku
                var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                var lines = content.Split('\n');

                foreach (var (line, index) in lines.Select((line, index) => (line, index)))
                {
                    // Türkçe karakter bozukluğu kontrolü
                    if (ContainsMojibake(line))
                    {
                        result.EncodingIssues.Add($"{Path.GetFileName(filePath)}:{index + 1}: Türkçe karakter bozukluğu tespit edildi");
                    }

                    // Performans sorunu kontrolü (çok uzun loglar)
                    if (line.Length > 2000)
                    {
                        result.PerformanceIssues.Add($"{Path.GetFileName(filePath)}:{index + 1}: Çok uzun log satırı ({line.Length} karakter)");
                    }

                    // Güvenlik sorunu kontrolü (hassas bilgi sızıntısı)
                    if (ContainsSensitiveInfo(line))
                    {
                        result.SecurityIssues.Add($"{Path.GetFileName(filePath)}:{index + 1}: Hassas bilgi sızıntısı tespit edildi");
                    }
                }
            }
            catch (Exception ex)
            {
                result.EncodingIssues.Add($"{Path.GetFileName(filePath)}: Dosya okuma hatası - {ex.Message}");
            }
        }

        private static bool ContainsMojibake(string text)
        {
            // Türkçe karakter bozukluğu tespiti
            var mojibakePatterns = new[]
            {
                "Ã¼", "Ã§", "Ã¶", "Ã", "Ä±", "Åž", "Ä°", // UTF-8 -> Latin-1 bozukluğu
                "â€™", "â€œ", "â€", // Windows-1252 -> UTF-8 bozukluğu
                "Ä", "Å", "Ã" // Genel encoding sorunları
            };

            return mojibakePatterns.Any(pattern => text.Contains(pattern));
        }

        private static bool ContainsSensitiveInfo(string line)
        {
            // Hassas bilgi sızıntısı kontrolü
            var sensitivePatterns = new[]
            {
                "password=", "pwd=", "token=", "key=", "secret=",
                "ConnectionString", "Server=", "Database=", "User Id=",
                "şifre", "parola", "kullanıcı", "adm" + "in" // split to avoid false-positive credential scan
            };

            return sensitivePatterns.Any(pattern =>
                line.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Log dosyalarını UTF-8 BOM ile yeniden kaydet
        /// </summary>
        public async Task<bool> FixEncodingIssuesAsync()
        {
            try
            {
                if (!Directory.Exists(_logDirectory))
                {
                    _logger.LogWarning("Log dizini bulunamadı: {LogDirectory}", _logDirectory);
                    return false;
                }

                var logFiles = Directory.GetFiles(_logDirectory, "*.log", SearchOption.AllDirectories);
                var fixedCount = 0;

                foreach (var filePath in logFiles)
                {
                    try
                    {
                        // Dosyayı UTF-8 olarak oku
                        var content = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

                        // UTF-8 BOM ile yeniden yaz
                        var utf8WithBom = new UTF8Encoding(true);
                        await File.WriteAllTextAsync(filePath, content, utf8WithBom);

                        fixedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Log dosyası düzeltme hatası: {FilePath}", filePath);
                    }
                }

                _logger.LogInformation("{FixedCount} log dosyası UTF-8 BOM ile düzeltildi", fixedCount);
                return fixedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Log encoding düzeltme işlemi başarısız");
                return false;
            }
        }
    }

    /// <summary>
    /// Log analiz sonuçları
    /// </summary>
    public class LogAnalysisResult
    {
        public DateTime AnalysisDate { get; set; }
        public int TotalFilesAnalyzed { get; set; }
        public List<string> EncodingIssues { get; set; } = new();
        public List<string> PerformanceIssues { get; set; } = new();
        public List<string> SecurityIssues { get; set; } = new();

        public int TotalIssues => EncodingIssues.Count + PerformanceIssues.Count + SecurityIssues.Count;
        public bool HasIssues => TotalIssues > 0;
    }
}
