using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using MesTechStok.Desktop.Services;
using MesTechStok.Core.Data.Models;
using System.Threading.Tasks;

namespace MesTechStok.Desktop.ViewModels
{
    /// <summary>
    /// Sistem sağlığı ve telemetry verilerini gösteren ViewModel.
    /// API çağrıları, circuit breaker durumları, sync metrikleri.
    /// </summary>
    public class HealthMetricsViewModel : ViewModelBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly DispatcherTimer _refreshTimer;
        private readonly TelemetryQueryService _telemetryQuery;
        private readonly MesTechStok.Core.Integrations.OpenCart.OpenCartSyncService _syncService;

        private string _syncStatus = "Unknown";
        private DateTime? _lastSyncTime;
        private string _circuitBreakerState = "Closed";
        private int _totalApiCalls = 0;
        private int _failedApiCalls = 0;
        private double _successRate = 100.0;
        private TimeSpan _averageResponseTime = TimeSpan.Zero;
        private string _lastError = "None";

        public string SyncStatus
        {
            get => _syncStatus;
            set => SetProperty(ref _syncStatus, value);
        }

        public DateTime? LastSyncTime
        {
            get => _lastSyncTime;
            set => SetProperty(ref _lastSyncTime, value);
        }

        public string CircuitBreakerState
        {
            get => _circuitBreakerState;
            set => SetProperty(ref _circuitBreakerState, value);
        }

        public int TotalApiCalls
        {
            get => _totalApiCalls;
            set => SetProperty(ref _totalApiCalls, value);
        }

        public int FailedApiCalls
        {
            get => _failedApiCalls;
            set => SetProperty(ref _failedApiCalls, value);
        }

        public double SuccessRate
        {
            get => _successRate;
            set => SetProperty(ref _successRate, value);
        }

        public TimeSpan AverageResponseTime
        {
            get => _averageResponseTime;
            set => SetProperty(ref _averageResponseTime, value);
        }

        public string LastError
        {
            get => _lastError;
            set => SetProperty(ref _lastError, value);
        }

        public ObservableCollection<ApiCallLog> RecentApiCalls { get; } = new();
        public ObservableCollection<CircuitStateLog> RecentCircuitChanges { get; } = new();

        public new ICommand RefreshCommand { get; }
        public ICommand ClearMetricsCommand { get; }
        public ICommand ExportMetricsCommand { get; }

        public HealthMetricsViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _telemetryQuery = serviceProvider.GetRequiredService<TelemetryQueryService>();
            _syncService = serviceProvider.GetRequiredService<MesTechStok.Core.Integrations.OpenCart.OpenCartSyncService>();

            RefreshCommand = new RelayCommand(async () => await RefreshMetricsAsync());
            ClearMetricsCommand = new RelayCommand(async () => await ClearMetricsAsync());
            ExportMetricsCommand = new RelayCommand(async () => await ExportMetricsAsync());

            // Auto-refresh her 30 saniyede
            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _refreshTimer.Tick += async (s, e) => await RefreshMetricsAsync();
            _refreshTimer.Start();

            // İlk yükleme
            _ = Task.Run(async () => await RefreshMetricsAsync());
        }

        /// <summary>
        /// Tüm metrikleri yenile
        /// </summary>
        public async Task RefreshMetricsAsync()
        {
            try
            {
                await RefreshSyncMetricsAsync();
                await RefreshApiMetricsAsync();
                await RefreshCircuitMetricsAsync();
                await RefreshRecentLogsAsync();
            }
            catch (Exception ex)
            {
                LastError = $"Refresh Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Sync durum bilgilerini yenile
        /// </summary>
        private Task RefreshSyncMetricsAsync()
        {
            SyncStatus = _syncService.IsSyncRunning ? "Running" : "Idle";
            LastSyncTime = _syncService.LastSyncDate;
            return Task.CompletedTask;
        }

        /// <summary>
        /// API çağrı metriklerini yenile
        /// </summary>
        private async Task RefreshApiMetricsAsync()
        {
            var last24Hours = await _telemetryQuery.GetRecentAsync(
                take: 1000
            );

            var apiCalls = last24Hours.ToList();

            TotalApiCalls = apiCalls.Count;
            FailedApiCalls = apiCalls.Count(x => !x.Success);

            SuccessRate = TotalApiCalls > 0
                ? Math.Round((double)(TotalApiCalls - FailedApiCalls) / TotalApiCalls * 100, 1)
                : 100.0;

            if (apiCalls.Any())
            {
                AverageResponseTime = TimeSpan.FromMilliseconds(
                    apiCalls.Average(x => x.DurationMs)
                );
            }

            var lastFailed = apiCalls.Where(x => !x.Success).OrderByDescending(x => x.TimestampUtc).FirstOrDefault();
            if (lastFailed != null)
            {
                LastError = $"{lastFailed.Method} {lastFailed.Endpoint} - Status: {lastFailed.StatusCode}";
            }
            else
            {
                LastError = "None";
            }
        }

        /// <summary>
        /// Circuit breaker durumunu yenile
        /// </summary>
        private async Task RefreshCircuitMetricsAsync()
        {
            var recentCircuitLogs = await _telemetryQuery.GetCircuitStateHistoryAsync(take: 1);
            var lastState = recentCircuitLogs.FirstOrDefault();

            CircuitBreakerState = lastState?.NewState ?? "Closed";
        }

        /// <summary>
        /// Son logları yenile
        /// </summary>
        private async Task RefreshRecentLogsAsync()
        {
            var recentApi = await _telemetryQuery.GetRecentAsync(take: 50);
            var recentCircuit = await _telemetryQuery.GetCircuitStateHistoryAsync(take: 20);

            RecentApiCalls.Clear();
            foreach (var call in recentApi)
            {
                RecentApiCalls.Add(call);
            }

            RecentCircuitChanges.Clear();
            foreach (var change in recentCircuit)
            {
                RecentCircuitChanges.Add(change);
            }
        }

        /// <summary>
        /// Telemetry verilerini temizle
        /// </summary>
        private async Task ClearMetricsAsync()
        {
            try
            {
                // Telemetry retention service kullanarak eski verileri temizle
                var retention = _serviceProvider.GetRequiredService<ITelemetryRetentionService>();
                await retention.ExecuteCleanupAsync();

                await RefreshMetricsAsync();
            }
            catch (Exception ex)
            {
                LastError = $"Clear Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Metrikleri CSV olarak dışa aktar
        /// </summary>
        private async Task ExportMetricsAsync()
        {
            try
            {
                var allData = await _telemetryQuery.GetRecentAsync(take: 1000);

                var csv = "Timestamp,Endpoint,Method,Success,StatusCode,DurationMs,Category\n";
                foreach (var call in allData)
                {
                    csv += $"{call.TimestampUtc:yyyy-MM-dd HH:mm:ss},{call.Endpoint},{call.Method},{call.Success},{call.StatusCode},{call.DurationMs},{call.Category}\n";
                }

                var fileName = $"health_metrics_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                await System.IO.File.WriteAllTextAsync(fileName, csv);

                LastError = $"Exported to {fileName}";
            }
            catch (Exception ex)
            {
                LastError = $"Export Error: {ex.Message}";
            }
        }

        public void Dispose()
        {
            _refreshTimer?.Stop();
        }
    }
}
