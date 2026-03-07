using System;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views
{
    public partial class PlatformSyncStatusView : UserControl
    {
        public PlatformSyncStatusView()
        {
            InitializeComponent();
        }

        private void PauseAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Tum senkronizasyon job'lari duraklatilacak. Devam edilsin mi?",
                "Tumu Duraklat", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                // TODO: Hangfire recurring job'lari duraklat
                MessageBox.Show("Tum job'lar duraklatildi. Hangfire implementasyonu bekleniyor (DEV4).",
                    "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ResumeAll_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Hangfire recurring job'lari baslat
            MessageBox.Show("Tum job'lar baslatildi. Hangfire implementasyonu bekleniyor (DEV4).",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RunOrderSync_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Hangfire.BackgroundJob.Enqueue<TrendyolOrderSyncJob>(...)
            MessageBox.Show("TrendyolOrderSync tetiklendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RunStockSync_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TrendyolStockSync tetiklendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RunClaimSync_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TrendyolClaimSync tetiklendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RunOpenCartSync_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("OpenCartStockSync tetiklendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RunInvoiceRetry_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("InvoiceRetryJob tetiklendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RunHealthCheck_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("HealthCheckJob tetiklendi.", "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            SyncHistoryGrid.ItemsSource = null;
        }
    }
}
