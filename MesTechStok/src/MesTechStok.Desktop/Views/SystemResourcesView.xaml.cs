using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MesTechStok.Desktop.Services;
using MesTechStok.Desktop.Models;
using System.Windows.Media;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// SystemResourcesView - Desktop içine entegre edilmiş sistem izleme
    /// </summary>
    public partial class SystemResourcesView : UserControl
    {
        private readonly SystemResourceService? _systemResourceService;
        private readonly DispatcherTimer _uiUpdateTimer;
        private bool _isMonitoring = false;

        public SystemResourcesView()
        {
            InitializeComponent();

            // SystemResourceService'i DI container'dan al
            var serviceProvider = App.ServiceProvider;
            if (serviceProvider != null)
            {
                _systemResourceService = serviceProvider.GetRequiredService<ISystemResourceService>() as SystemResourceService;
                if (_systemResourceService != null)
                {
                    _systemResourceService.PerformanceUpdated += OnPerformanceUpdated;
                }
            }
            else
            {
                // Fallback - create with logger
                var logger = new Logger<SystemResourceService>(new LoggerFactory());
                _systemResourceService = new SystemResourceService(logger);
                _systemResourceService.PerformanceUpdated += OnPerformanceUpdated;
            }

            // UI güncelleme timer'ı
            _uiUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _uiUpdateTimer.Tick += UiUpdateTimer_Tick;

            // Başlangıç durumu
            UpdateButtonStates();
        }

        private void OnPerformanceUpdated(object? sender, SystemPerformance performance)
        {
            // UI thread'de güncelleme yap
            Dispatcher.Invoke(() =>
            {
                UpdateUI(performance);
            });
        }

        private void UiUpdateTimer_Tick(object? sender, EventArgs e)
        {
            // UI elementlerini düzenli güncelle
            if (_isMonitoring && _systemResourceService != null)
            {
                var performance = _systemResourceService.SystemPerformance;
                UpdateUI(performance);
            }
        }

        private void UpdateUI(SystemPerformance performance)
        {
            try
            {
                // CPU
                CpuProgressBar.Value = performance.TotalCpuUsage;
                CpuUsageText.Text = $"{performance.TotalCpuUsage:F1}%";

                // RAM
                RamProgressBar.Value = performance.TotalMemoryUsage;
                RamUsageText.Text = $"{performance.TotalMemoryUsage:F1}%";
                RamDetailsText.Text = $"Kullanılan: {performance.UsedMemoryGB:F1} GB / {performance.TotalMemoryGB:F1} GB";

                // System Info
                ProcessCountText.Text = performance.ActiveProcessCount.ToString();
                UptimeText.Text = $"{(int)performance.SystemUptime.TotalHours}h {performance.SystemUptime.Minutes}m";
                LastUpdateText.Text = performance.LastUpdated.ToString("HH:mm:ss");

                // MesTech
                MesTechMemoryText.Text = $"{performance.MesTechMemoryUsageMB:F0} MB";
                MesTechCpuText.Text = $"{performance.MesTechCpuUsage:F1}%";

                StatusText.Text = _isMonitoring ? "Gerçek zamanlı izleme aktif" : "İzleme durduruldu";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UI update error: {ex.Message}");
            }
        }

        private void StartMonitoringBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_systemResourceService != null)
            {
                _systemResourceService.Start();
                _uiUpdateTimer?.Start();
                _isMonitoring = true;

                StartMonitoringBtn.IsEnabled = false;
                StopMonitoringBtn.IsEnabled = true;
                StatusText.Text = "✅ İzleme Aktif";
                StatusText.Foreground = new SolidColorBrush(Colors.Green);
            }
        }

        private void StopMonitoringBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_systemResourceService != null)
            {
                _systemResourceService.Stop();
                _uiUpdateTimer?.Stop();
                _isMonitoring = false;

                StartMonitoringBtn.IsEnabled = true;
                StopMonitoringBtn.IsEnabled = false;
                StatusText.Text = "⏸ İzleme Durduruldu";
                StatusText.Foreground = new SolidColorBrush(Colors.Orange);
            }
        }

        private void UpdateButtonStates()
        {
            StartMonitoringBtn.IsEnabled = !_isMonitoring;
            StopMonitoringBtn.IsEnabled = _isMonitoring;
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Cleanup
            _systemResourceService?.Stop();
            _systemResourceService?.Dispose();
            _uiUpdateTimer?.Stop();
        }
    }
}