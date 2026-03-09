using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace MesTechStok.Desktop.Utils
{
    public class GlobalLogger
    {
        private static GlobalLogger? _instance;
        private static readonly object _lock = new object();
        public event EventHandler<MesTechStok.Desktop.Views.LogEntry>? LogAdded;

        // In-memory halka tampon (ring buffer) – LogView açılmadığında bile geçmişin görülebilmesi için
        private readonly object _bufferLock = new object();
        private readonly LinkedList<MesTechStok.Desktop.Views.LogEntry> _ringBuffer = new LinkedList<MesTechStok.Desktop.Views.LogEntry>();
        private const int BufferCapacity = 2000;

        public static GlobalLogger Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new GlobalLogger();
                    }
                }
                return _instance;
            }
        }

        private GlobalLogger()
        {
            // Private constructor for singleton
        }

        public void LogInfo(string message, string source = "General")
        {
            Log("INFO", message, source);
        }

        public void LogWarning(string message, string source = "General")
        {
            Log("WARNING", message, source);
        }

        public void LogError(string message, string source = "General")
        {
            Log("ERROR", message, source);
        }

        private void Log(string level, string message, string source)
        {
            try
            {
                // Route to Serilog with structured properties
                try
                {
                    var logger = Serilog.Log.ForContext("Source", source);
                    switch (level)
                    {
                        case "ERROR":
                            logger.Error("{Message}", message);
                            break;
                        case "WARNING":
                            logger.Warning("{Message}", message);
                            break;
                        default:
                            logger.Information("{Message}", message);
                            break;
                    }
                }
                catch
                {
                    // Fallback: minimal file append to ensure we never lose message
                    var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{source}] {message}";
                    var logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                    if (!Directory.Exists(logDirectory))
                        Directory.CreateDirectory(logDirectory);
                    var logFile = Path.Combine(logDirectory, $"mestech-{DateTime.Now:yyyy-MM-dd}.log");
                    File.AppendAllText(logFile, logMessage + Environment.NewLine);
                }

                // Realtime UI bridge (LogView)
                try
                {
                    var color = level == "ERROR" ? System.Windows.Media.Colors.Red :
                                level == "WARNING" ? System.Windows.Media.Colors.Orange :
                                System.Windows.Media.Colors.Blue;
                    var entry = new MesTechStok.Desktop.Views.LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Level = level == "ERROR" ? "🔴 Hata" : level == "WARNING" ? "⚠️ Uyarı" : "ℹ️ Bilgi",
                        Message = message,
                        Source = source,
                        Color = color
                    };

                    // Halka tamponda sakla (geçmiş görüntüleme için)
                    lock (_bufferLock)
                    {
                        _ringBuffer.AddLast(entry);
                        if (_ringBuffer.Count > BufferCapacity)
                        {
                            _ringBuffer.RemoveFirst();
                        }
                    }

                    LogAdded?.Invoke(this, entry);
                }
                catch { /* UI köprüsü opsiyonel */ }
            }
            catch
            {
                // Logging should never throw exceptions
            }
        }

        // Convenience helpers with event type tagging per PRD 34
        public void LogEvent(string eventType, string message, string source = "General")
        {
            LogInfo($"[{eventType}] {message}", source);
        }

        public void LogAudit(string eventType, string message, string source = "Audit")
        {
            LogInfo($"[AUDIT:{eventType}] {message}", source);
        }

        /// <summary>
        /// UTF-8 karakter bozulması için güncel log dosyasını hızlı kontrol eder ve UI'ya bilgi mesajı yayınlar.
        /// </summary>
        public void LoadLogsWithCharacterValidation()
        {
            try
            {
                var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDir)) return;

                var todayLog = Path.Combine(logDir, $"mestech-{DateTime.Now:yyyy-MM-dd}.log");
                if (!File.Exists(todayLog)) return;

                var lines = File.ReadAllLines(todayLog, System.Text.Encoding.UTF8);
                var brokenCharCount = 0;

                foreach (var line in lines.Take(100)) // İlk 100 satırı kontrol et
                {
                    if (line.Contains("Ã") || line.Contains("ğ˘") || line.Contains("ı") ||
                        line.Contains("ü") || line.Contains("Ş") || line.Contains("ç"))
                    {
                        brokenCharCount++;
                    }
                }

                if (brokenCharCount > 0)
                {
                    LogAdded?.Invoke(this, new MesTechStok.Desktop.Views.LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Level = "⚠️",
                        Message = $"🇹🇷 {brokenCharCount} satırda Türkçe karakter bozukluğu tespit edildi! UTF-8 encoding sorunu.",
                        Source = "LogAnalyzer",
                        Color = System.Windows.Media.Colors.Orange
                    });
                }
                else
                {
                    LogAdded?.Invoke(this, new MesTechStok.Desktop.Views.LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Level = "ℹ️",
                        Message = "✅ Türkçe karakter kontrolü: Problem tespit edilmedi",
                        Source = "LogAnalyzer",
                        Color = System.Windows.Media.Colors.Green
                    });
                }
            }
            catch (Exception ex)
            {
                try
                {
                    LogAdded?.Invoke(this, new MesTechStok.Desktop.Views.LogEntry
                    {
                        Timestamp = DateTime.Now,
                        Level = "🔴",
                        Message = $"Log karakter analizi hatası: {ex.Message}",
                        Source = "LogAnalyzer",
                        Color = System.Windows.Media.Colors.Red
                    });
                }
                catch
                {
                    // Intentional: in-process logger event — the logger itself must never throw.
                }
            }
        }

        // Geçmişin anlık görüntüsünü ver (en yeni en sonda olacak şekilde)
        public IReadOnlyList<MesTechStok.Desktop.Views.LogEntry> GetSnapshot(int maxItems = BufferCapacity)
        {
            lock (_bufferLock)
            {
                if (_ringBuffer.Count == 0) return Array.Empty<MesTechStok.Desktop.Views.LogEntry>();
                var take = Math.Min(maxItems, _ringBuffer.Count);
                return _ringBuffer.Skip(Math.Max(0, _ringBuffer.Count - take)).ToList();
            }
        }
    }
}