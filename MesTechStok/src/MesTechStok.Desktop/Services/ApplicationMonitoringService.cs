using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using MesTechStok.Desktop.Utils;

namespace MesTechStok.Desktop.Services
{
    /// <summary>
    /// Comprehensive application monitoring service
    /// Tracks startup time, uptime, performance metrics, and application lifecycle
    /// </summary>
    public interface IApplicationMonitoringService
    {
        DateTime ApplicationStartTime { get; }
        TimeSpan ApplicationUptime { get; }
        TimeSpan StartupDuration { get; }
        bool IsMonitoring { get; }

        void StartMonitoring();
        void StopMonitoring();
        void RecordApplicationReady();
        Task<ApplicationMetrics> GetMetricsAsync();

        event EventHandler<ApplicationMetrics>? MetricsUpdated;
    }

    public class ApplicationMonitoringService : IApplicationMonitoringService
    {
        private readonly ILogger<ApplicationMonitoringService> _logger;
        private readonly Timer _monitoringTimer;
        private readonly Stopwatch _startupStopwatch;

        private DateTime _applicationStartTime;
        private DateTime _applicationReadyTime;
        private TimeSpan _startupDuration;
        private bool _isMonitoring = false;
        private bool _startupCompleted = false;

        private readonly List<PerformanceSnapshot> _performanceHistory = new();

        public DateTime ApplicationStartTime => _applicationStartTime;
        public TimeSpan ApplicationUptime => DateTime.Now - _applicationStartTime;
        public TimeSpan StartupDuration => _startupDuration;
        public bool IsMonitoring => _isMonitoring;

        public event EventHandler<ApplicationMetrics>? MetricsUpdated;

        public ApplicationMonitoringService(ILogger<ApplicationMonitoringService> logger)
        {
            _logger = logger;
            _applicationStartTime = DateTime.Now;
            _startupStopwatch = Stopwatch.StartNew();

            // Her 5 saniyede monitoring yap
            _monitoringTimer = new Timer(MonitoringCallback, null, Timeout.Infinite, Timeout.Infinite);

            GlobalLogger.Instance.LogInfo($"🚀 Application monitoring başlatıldı - Start Time: {_applicationStartTime:HH:mm:ss}", "AppMonitoring");
        }

        public void StartMonitoring()
        {
            if (_isMonitoring) return;

            try
            {
                _isMonitoring = true;
                _monitoringTimer.Change(0, 5000); // 5 saniye interval

                GlobalLogger.Instance.LogInfo("📊 Application performance monitoring aktif", "AppMonitoring");
                _logger.LogInformation("Application monitoring started");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application monitoring start error");
                GlobalLogger.Instance.LogError($"Monitoring başlatma hatası: {ex.Message}", "AppMonitoring");
            }
        }

        public void StopMonitoring()
        {
            if (!_isMonitoring) return;

            try
            {
                _isMonitoring = false;
                _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);

                GlobalLogger.Instance.LogInfo("📊 Application monitoring durduruldu", "AppMonitoring");
                _logger.LogInformation("Application monitoring stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application monitoring stop error");
            }
        }

        public void RecordApplicationReady()
        {
            if (_startupCompleted) return;

            try
            {
                _startupCompleted = true;
                _applicationReadyTime = DateTime.Now;
                _startupStopwatch.Stop();
                _startupDuration = _startupStopwatch.Elapsed;

                var startupMessage = $"✅ Application hazır! Startup süresi: {_startupDuration.TotalMilliseconds:F0}ms";

                // Startup performance değerlendirmesi
                string performanceRating;
                if (_startupDuration.TotalSeconds < 2)
                    performanceRating = "🟢 ÇOK HIZLI";
                else if (_startupDuration.TotalSeconds < 5)
                    performanceRating = "🟡 NORMAL";
                else if (_startupDuration.TotalSeconds < 10)
                    performanceRating = "🟠 YAVAS";
                else
                    performanceRating = "🔴 ÇOK YAVAS";

                GlobalLogger.Instance.LogInfo($"{startupMessage} - {performanceRating}", "AppMonitoring");
                _logger.LogInformation($"Application ready - Startup duration: {_startupDuration.TotalMilliseconds}ms");

                // Otomatik monitoring başlat
                StartMonitoring();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Application ready recording error");
            }
        }

        private async void MonitoringCallback(object? state)
        {
            if (!_isMonitoring) return;

            try
            {
                var metrics = await CollectMetricsAsync();

                lock (_performanceHistory)
                {
                    _performanceHistory.Add(new PerformanceSnapshot
                    {
                        Timestamp = DateTime.Now,
                        MemoryUsage = metrics.MemoryUsageMB,
                        CpuUsage = 0, // Placeholder
                        UptimeSeconds = metrics.UptimeSeconds
                    });

                    // Son 100 snapshot'ı tut
                    if (_performanceHistory.Count > 100)
                    {
                        _performanceHistory.RemoveRange(0, _performanceHistory.Count - 100);
                    }
                }

                // Her 30 saniyede log
                if (DateTime.Now.Second % 30 == 0)
                {
                    var uptimeFormatted = FormatUptime(metrics.Uptime);
                    GlobalLogger.Instance.LogInfo($"📊 Uptime: {uptimeFormatted}, Memory: {metrics.MemoryUsageMB:F1}MB", "AppMonitoring");
                }

                MetricsUpdated?.Invoke(this, metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monitoring callback error");
            }
        }

        private async Task<ApplicationMetrics> CollectMetricsAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var currentProcess = Process.GetCurrentProcess();

                    return new ApplicationMetrics
                    {
                        ApplicationStartTime = _applicationStartTime,
                        ApplicationReadyTime = _applicationReadyTime,
                        StartupDuration = _startupDuration,
                        Uptime = ApplicationUptime,
                        UptimeSeconds = (int)ApplicationUptime.TotalSeconds,
                        MemoryUsageMB = currentProcess.WorkingSet64 / (1024.0 * 1024.0),
                        PrivateMemoryMB = currentProcess.PrivateMemorySize64 / (1024.0 * 1024.0),
                        ThreadCount = currentProcess.Threads.Count,
                        HandleCount = currentProcess.HandleCount,
                        IsStartupCompleted = _startupCompleted,
                        LastUpdated = DateTime.Now
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Metrics collection error");
                    return new ApplicationMetrics
                    {
                        ApplicationStartTime = _applicationStartTime,
                        Uptime = ApplicationUptime,
                        StartupDuration = _startupDuration,
                        IsStartupCompleted = _startupCompleted,
                        LastUpdated = DateTime.Now
                    };
                }
            });
        }

        public async Task<ApplicationMetrics> GetMetricsAsync()
        {
            return await CollectMetricsAsync();
        }

        private string FormatUptime(TimeSpan uptime)
        {
            if (uptime.TotalHours >= 1)
                return $"{(int)uptime.TotalHours}h {uptime.Minutes}m {uptime.Seconds}s";
            else if (uptime.TotalMinutes >= 1)
                return $"{uptime.Minutes}m {uptime.Seconds}s";
            else
                return $"{uptime.Seconds}s";
        }

        public void Dispose()
        {
            StopMonitoring();
            _monitoringTimer?.Dispose();
            _startupStopwatch?.Stop();
        }
    }

    /// <summary>
    /// Application metrics data
    /// </summary>
    public class ApplicationMetrics
    {
        public DateTime ApplicationStartTime { get; set; }
        public DateTime ApplicationReadyTime { get; set; }
        public TimeSpan StartupDuration { get; set; }
        public TimeSpan Uptime { get; set; }
        public int UptimeSeconds { get; set; }
        public double MemoryUsageMB { get; set; }
        public double PrivateMemoryMB { get; set; }
        public int ThreadCount { get; set; }
        public int HandleCount { get; set; }
        public bool IsStartupCompleted { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Performance snapshot for history tracking
    /// </summary>
    public class PerformanceSnapshot
    {
        public DateTime Timestamp { get; set; }
        public double MemoryUsage { get; set; }
        public double CpuUsage { get; set; }
        public int UptimeSeconds { get; set; }
    }
}