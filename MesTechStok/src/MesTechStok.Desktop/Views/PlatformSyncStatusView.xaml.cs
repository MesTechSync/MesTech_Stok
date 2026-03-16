using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Infrastructure.Jobs;

namespace MesTechStok.Desktop.Views
{
    public partial class PlatformSyncStatusView : UserControl
    {
        private readonly ObservableCollection<SyncHistoryItem> _history = new();
        private bool _isPaused;

        public PlatformSyncStatusView()
        {
            InitializeComponent();
            SyncHistoryGrid.ItemsSource = _history;

            Loaded += (s, e) => RefreshJobStatuses();
        }

        private void RefreshJobStatuses()
        {
            var activeCount = _isPaused ? 0 : 9;
            ActiveJobsText.Text = $"{activeCount} / 9";
            TodaySyncCountText.Text = _history.Count(h =>
                DateTime.TryParse(h.Timestamp, out var dt) && dt.Date == DateTime.Today).ToString();
            FailedSyncText.Text = _history.Count(h => h.Result == "Basarisiz").ToString();
            QueuedItemsText.Text = "0";
        }

        private void PauseAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Tum senkronizasyon job'lari duraklatilacak. Devam edilsin mi?",
                "Tumu Duraklat", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _isPaused = true;
                AddHistory("System", "-", "Bilgi", 0, 0, "Tum job'lar duraklatildi");
                RefreshJobStatuses();
                MessageBox.Show("Tum job'lar duraklatildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ResumeAll_Click(object sender, RoutedEventArgs e)
        {
            _isPaused = false;
            AddHistory("System", "-", "Bilgi", 0, 0, "Tum job'lar baslatildi");
            RefreshJobStatuses();
            MessageBox.Show("Tum job'lar baslatildi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void RunOrderSync_Click(object sender, RoutedEventArgs e)
        {
            await RunJobAsync<TrendyolOrderSyncJob>("TrendyolOrderSync", "Trendyol");
        }

        private async void RunStockSync_Click(object sender, RoutedEventArgs e)
        {
            await RunJobAsync<TrendyolStockSyncJob>("TrendyolStockSync", "Trendyol");
        }

        private async void RunClaimSync_Click(object sender, RoutedEventArgs e)
        {
            await RunJobAsync<TrendyolClaimSyncJob>("TrendyolClaimSync", "Trendyol");
        }

        private async void RunOpenCartSync_Click(object sender, RoutedEventArgs e)
        {
            await RunJobAsync<OpenCartStockSyncJob>("OpenCartStockSync", "OpenCart");
        }

        private async void RunInvoiceRetry_Click(object sender, RoutedEventArgs e)
        {
            await RunJobAsync<InvoiceRetryJob>("InvoiceRetry", "Genel");
        }

        private async void RunHealthCheck_Click(object sender, RoutedEventArgs e)
        {
            await RunJobAsync<HealthCheckJob>("HealthCheck", "Genel");
        }

        private async Task RunJobAsync<TJob>(string jobName, string platform) where TJob : class, ISyncJob
        {
            AddHistory(jobName, platform, "Calisiyor", 0, 0, "Manuel tetiklendi");
            var sw = Stopwatch.StartNew();

            try
            {
                var job = App.Services?.GetService<TJob>();
                if (job == null)
                {
                    sw.Stop();
                    AddHistory(jobName, platform, "Basarisiz", (int)sw.ElapsedMilliseconds, 0,
                        "Job DI'da bulunamadi — Hangfire altyapisi henuz aktif degil");
                    RefreshJobStatuses();
                    return;
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                await job.ExecuteAsync(cts.Token);
                sw.Stop();

                AddHistory(jobName, platform, "Basarili", (int)sw.ElapsedMilliseconds, 0, "Tamamlandi");
                OrderSyncLastRun.Text = $"Son: {DateTime.Now:HH:mm:ss}";
            }
            catch (OperationCanceledException)
            {
                sw.Stop();
                AddHistory(jobName, platform, "Zaman Asimi", (int)sw.ElapsedMilliseconds, 0, "30s timeout asildi");
            }
            catch (Exception ex)
            {
                sw.Stop();
                AddHistory(jobName, platform, "Basarisiz", (int)sw.ElapsedMilliseconds, 0, ex.Message);
            }

            RefreshJobStatuses();
        }

        private void AddHistory(string jobName, string platform, string result, int durationMs, int itemsProcessed, string message)
        {
            _history.Insert(0, new SyncHistoryItem
            {
                Timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
                JobName = jobName,
                Platform = platform,
                Result = result,
                DurationMs = durationMs,
                ItemsProcessed = itemsProcessed,
                ErrorCount = result == "Basarisiz" ? 1 : 0,
                Message = message
            });

            while (_history.Count > 200)
                _history.RemoveAt(_history.Count - 1);
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            _history.Clear();
            RefreshJobStatuses();
        }

        #region L/E/E State Helpers

        private void ShowLoading()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            EmptyState.Visibility = Visibility.Collapsed;
            ErrorState.Visibility = Visibility.Collapsed;
        }

        private void ShowEmpty()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Visible;
            ErrorState.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;
            ErrorState.Visibility = Visibility.Visible;
            ErrorMessage.Text = message;
        }

        private void HideAllStates()
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;
            ErrorState.Visibility = Visibility.Collapsed;
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            HideAllStates();
            RefreshJobStatuses();
        }

        #endregion
    }

    internal class SyncHistoryItem
    {
        public string Timestamp { get; set; } = "";
        public string JobName { get; set; } = "";
        public string Platform { get; set; } = "";
        public string Result { get; set; } = "";
        public int DurationMs { get; set; }
        public int ItemsProcessed { get; set; }
        public int ErrorCount { get; set; }
        public string Message { get; set; } = "";
    }
}
