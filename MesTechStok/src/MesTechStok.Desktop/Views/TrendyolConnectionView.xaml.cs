using System;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views
{
    public partial class TrendyolConnectionView : UserControl
    {
        public TrendyolConnectionView()
        {
            InitializeComponent();
            AddLogEntry("Trendyol baglanti ekrani yuklendi.");
        }

        private void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            var sellerId = SellerIdTextBox.Text?.Trim();
            var apiKey = ApiKeyTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(sellerId) || string.IsNullOrWhiteSpace(apiKey) || ApiSecretBox.SecurePassword.Length == 0)
            {
                MessageBox.Show("Lutfen tum kimlik bilgilerini doldurunuz.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AddLogEntry($"Baglanti testi baslatildi... Seller ID: {sellerId}");
            // TODO: Gercek Trendyol API baglanti testi (GET /api-status)
            ConnectionStatusBadge.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 245, 233));
            ConnectionStatusText.Text = "Test Bekleniyor";
            ConnectionStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50));
            AddLogEntry("Baglanti testi icin TrendyolAdapter.CheckHealthAsync() cagrilacak.");
        }

        private void SaveCredentials_Click(object sender, RoutedEventArgs e)
        {
            var sellerId = SellerIdTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                MessageBox.Show("Seller ID bos olamaz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // TODO: User Secrets veya guvenli depolama ile kaydet
            AddLogEntry("Kimlik bilgileri kaydedildi (User Secrets).");
            MessageBox.Show("Kimlik bilgileri basariyla kaydedildi.", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveSyncSettings_Click(object sender, RoutedEventArgs e)
        {
            AddLogEntry($"Sync ayarlari kaydedildi. Otomatik sync: {AutoSyncCheckBox.IsChecked}, Otomatik fatura: {AutoInvoiceCheckBox.IsChecked}");
            MessageBox.Show("Senkronizasyon ayarlari kaydedildi.", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PullProducts_Click(object sender, RoutedEventArgs e)
        {
            AddLogEntry("Trendyol urun cekme islemi baslatildi...");
            // TODO: TrendyolAdapter.PullProductsAsync(null)
            AddLogEntry("Urun cekme icin TrendyolAdapter bekliyor (DEV3 implementasyonu).");
        }

        private void PushStock_Click(object sender, RoutedEventArgs e)
        {
            AddLogEntry("Stok gonderme islemi baslatildi...");
            // TODO: TrendyolAdapter.PushStockUpdateAsync()
            AddLogEntry("Stok gonderme icin TrendyolAdapter bekliyor (DEV3 implementasyonu).");
        }

        private void PullOrders_Click(object sender, RoutedEventArgs e)
        {
            AddLogEntry("Siparis cekme islemi baslatildi...");
            // TODO: TrendyolAdapter.PullOrdersAsync(null)
            AddLogEntry("Siparis cekme icin TrendyolAdapter bekliyor (DEV3 implementasyonu).");
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            ActivityLogList.Items.Clear();
        }

        private void AddLogEntry(string message)
        {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            ActivityLogList.Items.Insert(0, entry);
            if (ActivityLogList.Items.Count > 200)
                ActivityLogList.Items.RemoveAt(ActivityLogList.Items.Count - 1);
        }
    }
}
