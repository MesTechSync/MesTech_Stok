using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MesTechStok.Desktop.Utils; // For GlobalLogger and ToastManager

namespace MesTechStok.Desktop.Services
{
    public class NavigationTimingService
    {
        private static NavigationTimingService? _instance;
        public static NavigationTimingService Instance => _instance ??= new NavigationTimingService();

        private readonly Dictionary<string, Stopwatch> _activeTimers = new();
        private readonly Dictionary<string, TimeSpan> _lastLoadTimes = new();

        private NavigationTimingService() { }

        /// <summary>
        /// Menü yükleme süresini başlat
        /// </summary>
        public void StartTiming(string moduleName)
        {
            try
            {
                if (_activeTimers.ContainsKey(moduleName))
                {
                    _activeTimers[moduleName].Restart();
                }
                else
                {
                    _activeTimers[moduleName] = Stopwatch.StartNew();
                }

                GlobalLogger.Instance.LogInfo($"⏱️ {moduleName} yükleme başladı", "NavigationTiming");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Timing başlatma hatası: {ex.Message}", "NavigationTiming");
            }
        }

        /// <summary>
        /// Menü yükleme süresini bitir ve kaydet
        /// </summary>
        public TimeSpan StopTiming(string moduleName)
        {
            try
            {
                if (_activeTimers.TryGetValue(moduleName, out var stopwatch))
                {
                    stopwatch.Stop();
                    var elapsed = stopwatch.Elapsed;

                    _lastLoadTimes[moduleName] = elapsed;
                    _activeTimers.Remove(moduleName);

                    var loadTime = $"{elapsed.TotalMilliseconds:F0}ms";
                    var status = elapsed.TotalMilliseconds switch
                    {
                        < 100 => "🟢 ÇOK HIZLI",
                        < 500 => "🟡 NORMAL",
                        < 1000 => "🟠 YAVAS",
                        _ => "🔴 ÇOK YAVAS"
                    };

                    GlobalLogger.Instance.LogInfo($"⏱️ {moduleName} yüklendi: {loadTime} - {status}", "NavigationTiming");

                    // Yavaş yükleme uyarısı
                    if (elapsed.TotalMilliseconds > 800)
                    {
                        ToastManager.ShowWarning($"⚠️ {moduleName} yavaş yüklendi ({loadTime})", "Performans");
                    }
                    else if (elapsed.TotalMilliseconds < 100)
                    {
                        ToastManager.ShowSuccess($"⚡ {moduleName} hızlı yüklendi ({loadTime})", "Performans");
                    }

                    return elapsed;
                }
                else
                {
                    GlobalLogger.Instance.LogWarning($"Timing bulunamadı: {moduleName}", "NavigationTiming");
                    return TimeSpan.Zero;
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Timing durdurma hatası: {ex.Message}", "NavigationTiming");
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Son yükleme sürelerini al
        /// </summary>
        public Dictionary<string, TimeSpan> GetLastLoadTimes()
        {
            return new Dictionary<string, TimeSpan>(_lastLoadTimes);
        }

        /// <summary>
        /// Ortalama yükleme sürelerini raporla
        /// </summary>
        public string GetPerformanceReport()
        {
            try
            {
                if (_lastLoadTimes.Count == 0)
                    return "Henüz performans verisi yok";

                var report = "📊 MENÜ PERFORMANS RAPORU\n\n";

                foreach (var kvp in _lastLoadTimes)
                {
                    var time = kvp.Value.TotalMilliseconds;
                    var emoji = time switch
                    {
                        < 100 => "🟢",
                        < 500 => "🟡",
                        < 1000 => "🟠",
                        _ => "🔴"
                    };

                    report += $"{emoji} {kvp.Key}: {time:F0}ms\n";
                }

                var avgTime = _lastLoadTimes.Values.Select(t => t.TotalMilliseconds).Average();
                report += $"\n📈 Ortalama: {avgTime:F0}ms";

                return report;
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Performans raporu hatası: {ex.Message}", "NavigationTiming");
                return "Performans raporu oluşturulamadı";
            }
        }

        /// <summary>
        /// Tüm timing verilerini temizle
        /// </summary>
        public void ClearTimings()
        {
            _activeTimers.Clear();
            _lastLoadTimes.Clear();
            GlobalLogger.Instance.LogInfo("🧹 Tüm timing verileri temizlendi", "NavigationTiming");
        }
    }
}