using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MesTechStok.Desktop.Services;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// Log Monitoring View - ACİL LOG İYİLEŞTİRME RAPORU UI
    /// </summary>
    public partial class LogMonitoringView : UserControl
    {
        public LogMonitoringView()
        {
            InitializeComponent();
            LoadData();
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }

        private async Task LoadData()
        {
            try
            {
                BtnRefresh.IsEnabled = false;
                BtnRefresh.Content = "YENİLENİYOR...";

                // İstatistikleri yükle
                await Task.Run(() =>
                {
                    var stats = LogAnalyzer.GetErrorStats(1); // Son 24 saat
                    var recentErrors = LogAnalyzer.GetRecentCriticalErrors(1).Take(50).ToList();

                    Dispatcher.Invoke(() =>
                    {
                        // İstatistikleri güncelle
                        TxtTotalErrors.Text = stats.TotalCriticalErrors.ToString();
                        TxtOfflineErrors.Text = stats.OfflineQueueErrors.ToString();
                        TxtImageErrors.Text = stats.ImageStorageErrors.ToString();
                        TxtEncodingErrors.Text = stats.EncodingErrors.ToString();
                        TxtSecurityErrors.Text = stats.PathInjectionAttempts.ToString();

                        // Kritik hataları listele
                        DgCriticalErrors.ItemsSource = recentErrors;

                        // Son güncelleme zamanı
                        TxtLastUpdate.Text = $"Son güncelleme: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";

                        // Sistem sağlığı kontrolü
                        if (!stats.IsHealthy)
                        {
                            ShowCriticalAlert(stats);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Log verileri yüklenirken hata:\n{ex.Message}",
                              "Log Monitoring Hatası",
                              MessageBoxButton.OK,
                              MessageBoxImage.Error);
            }
            finally
            {
                BtnRefresh.IsEnabled = true;
                BtnRefresh.Content = "YENİLE";
            }
        }

        private void ShowCriticalAlert(LogStats stats)
        {
            var message = "🚨 KRİTİK SISTEM UYARISI!\n\n";

            if (stats.TotalCriticalErrors >= 5)
            {
                message += $"• Kritik hata sayısı limite ulaştı: {stats.TotalCriticalErrors}/5\n";
            }

            if (stats.PathInjectionAttempts > 0)
            {
                message += $"• GÜVENLİK TEHDİDİ: {stats.PathInjectionAttempts} path injection denemesi!\n";
            }

            if (stats.OfflineQueueErrors > 20)
            {
                message += $"• OpenCart entegrasyon sorunu: {stats.OfflineQueueErrors} hata\n";
            }

            message += "\nLütfen sistem yöneticisi ile iletişime geçin!";

            MessageBox.Show(message,
                          "KRİTİK SISTEM UYARISI",
                          MessageBoxButton.OK,
                          MessageBoxImage.Error);
        }
    }
}
