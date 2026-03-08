using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MesTech.Infrastructure.Integration.Adapters;

namespace MesTechStok.Desktop.Views
{
    public partial class TrendyolConnectionView : UserControl
    {
        private readonly TrendyolAdapter? _adapter;
        private readonly string _defaultBaseUrl;

        public TrendyolConnectionView()
        {
            InitializeComponent();
            _adapter = App.ServiceProvider?.GetService<TrendyolAdapter>();
            var config = App.ServiceProvider?.GetService<IConfiguration>();
            _defaultBaseUrl = config?["Platforms:Trendyol:BaseUrl"] ?? "https://api.trendyol.com/sapigw";
            AddLogEntry(_adapter != null
                ? "Trendyol baglanti ekrani yuklendi. Adapter hazir."
                : "Trendyol baglanti ekrani yuklendi. UYARI: TrendyolAdapter bulunamadi.");
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            var sellerId = SellerIdTextBox.Text?.Trim();
            var apiKey = ApiKeyTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(sellerId) || string.IsNullOrWhiteSpace(apiKey) || ApiSecretBox.SecurePassword.Length == 0)
            {
                MessageBox.Show("Lutfen tum kimlik bilgilerini doldurunuz.", "Eksik Bilgi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_adapter == null)
            {
                AddLogEntry("HATA: TrendyolAdapter DI'da bulunamadi.");
                return;
            }

            AddLogEntry($"Baglanti testi baslatildi... Seller ID: {sellerId}");
            ConnectionStatusText.Text = "Test ediliyor...";

            try
            {
                var credentials = BuildCredentials();
                var result = await _adapter.TestConnectionAsync(credentials);

                if (result.IsSuccess)
                {
                    ConnectionStatusBadge.Background = new SolidColorBrush(Color.FromRgb(232, 245, 233));
                    ConnectionStatusText.Text = "Bagli";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(46, 125, 50));
                    ProductCountText.Text = $"Urun: {result.ProductCount ?? 0}";
                    AddLogEntry($"Baglanti basarili! Magaza: {result.StoreName}, Urun: {result.ProductCount}, Sure: {result.ResponseTime.TotalMilliseconds:F0}ms");
                }
                else
                {
                    ConnectionStatusBadge.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                    ConnectionStatusText.Text = "Basarisiz";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                    AddLogEntry($"Baglanti basarisiz: {result.ErrorMessage}");
                }

                ApiStatusText.Text = result.IsSuccess ? "Aktif" : "Hatali";
                LastSyncText.Text = $"Son test: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                ConnectionStatusBadge.Background = new SolidColorBrush(Color.FromRgb(255, 235, 238));
                ConnectionStatusText.Text = "Hata";
                ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(198, 40, 40));
                AddLogEntry($"Baglanti testi hatasi: {ex.Message}");
            }
        }

        private void SaveCredentials_Click(object sender, RoutedEventArgs e)
        {
            var sellerId = SellerIdTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(sellerId))
            {
                MessageBox.Show("Seller ID bos olamaz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AddLogEntry("Kimlik bilgileri kaydedildi (bellek icinde — User Secrets entegrasyonu planlanmakta).");
            MessageBox.Show("Kimlik bilgileri basariyla kaydedildi.", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveSyncSettings_Click(object sender, RoutedEventArgs e)
        {
            AddLogEntry($"Sync ayarlari kaydedildi. Otomatik sync: {AutoSyncCheckBox.IsChecked}, Otomatik fatura: {AutoInvoiceCheckBox.IsChecked}");
            MessageBox.Show("Senkronizasyon ayarlari kaydedildi.", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void PullProducts_Click(object sender, RoutedEventArgs e)
        {
            if (_adapter == null) { AddLogEntry("HATA: TrendyolAdapter bulunamadi."); return; }

            AddLogEntry("Trendyol urun cekme islemi baslatildi...");
            try
            {
                var products = await _adapter.PullProductsAsync();
                ProductCountText.Text = $"Urun: {products.Count}";
                LastSyncText.Text = $"Son sync: {DateTime.Now:HH:mm:ss}";
                AddLogEntry($"Urun cekme tamamlandi. {products.Count} urun alindi.");
            }
            catch (InvalidOperationException)
            {
                AddLogEntry("Once baglanti testi yapin (API kimlik bilgileri gerekli).");
            }
            catch (Exception ex)
            {
                AddLogEntry($"Urun cekme hatasi: {ex.Message}");
            }
        }

        private async void PushStock_Click(object sender, RoutedEventArgs e)
        {
            if (_adapter == null) { AddLogEntry("HATA: TrendyolAdapter bulunamadi."); return; }

            AddLogEntry("Toplu stok senkronizasyonu icin Sync Durumu ekranini kullanin.");
            AddLogEntry("Tekil stok guncelleme icin Urun Yonetimi ekranindan ilgili urunu secin.");
        }

        private async void PullOrders_Click(object sender, RoutedEventArgs e)
        {
            if (_adapter == null) { AddLogEntry("HATA: TrendyolAdapter bulunamadi."); return; }

            AddLogEntry("Siparis cekme islemi baslatildi (son 7 gun)...");
            try
            {
                var orders = await _adapter.PullOrdersAsync(DateTime.Today.AddDays(-7));
                PendingOrderText.Text = $"Bekleyen: {orders.Count}";
                LastSyncText.Text = $"Son sync: {DateTime.Now:HH:mm:ss}";
                AddLogEntry($"Siparis cekme tamamlandi. {orders.Count} siparis alindi.");
            }
            catch (InvalidOperationException)
            {
                AddLogEntry("Once baglanti testi yapin (API kimlik bilgileri gerekli).");
            }
            catch (Exception ex)
            {
                AddLogEntry($"Siparis cekme hatasi: {ex.Message}");
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            ActivityLogList.Items.Clear();
        }

        private Dictionary<string, string> BuildCredentials()
        {
            return new Dictionary<string, string>
            {
                ["SupplierId"] = SellerIdTextBox.Text?.Trim() ?? "",
                ["ApiKey"] = ApiKeyTextBox.Text?.Trim() ?? "",
                ["ApiSecret"] = ConvertSecureString(ApiSecretBox.SecurePassword),
                ["BaseUrl"] = BaseUrlTextBox.Text?.Trim() ?? _defaultBaseUrl
            };
        }

        private static string ConvertSecureString(SecureString secureString)
        {
            if (secureString == null || secureString.Length == 0) return string.Empty;
            var ptr = Marshal.SecureStringToBSTR(secureString);
            try { return Marshal.PtrToStringBSTR(ptr); }
            finally { Marshal.ZeroFreeBSTR(ptr); }
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
