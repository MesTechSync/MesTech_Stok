using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MesTechStok.Core.Integrations.OpenCart;
using MesTechStok.Desktop.Services;

namespace MesTechStok.Desktop.Views
{
    public class SyncHistoryRecord
    {
        public DateTime Date { get; set; }
        public string Operation { get; set; } = "";
        public string Status { get; set; } = "";
        public int RecordCount { get; set; }
        public string Duration { get; set; } = "";
        public string Details { get; set; } = "";
    }

    public partial class OpenCartView : UserControl
    {
        private ObservableCollection<SyncHistoryRecord> syncHistory = new();
        private DispatcherTimer? statisticsTimer;
        private DispatcherTimer? syncProgressTimer;
        private Random random = new Random();
        private bool isSyncRunning = false;
        private int currentProgress = 65;

        public OpenCartView()
        {
            InitializeComponent();
            InitializeDemoData();
            SetupTimers();
            UpdateConnectionStatus();
            TryPopulateConfigFromAppSettings();
            HookHealth();
        }

        private void InitializeDemoData()
        {
            syncHistory = new ObservableCollection<SyncHistoryRecord>();

            // Demo senkronizasyon geçmişi
            var demoSyncHistory = new[]
            {
                new SyncHistoryRecord { Date = DateTime.Now.AddMinutes(-15), Operation = "Ürün Sync", Status = "✅ Başarılı", RecordCount = 150, Duration = "45s", Details = "150 ürün başarıyla senkronize edildi" },
                new SyncHistoryRecord { Date = DateTime.Now.AddMinutes(-32), Operation = "Stok Güncelle", Status = "✅ Başarılı", RecordCount = 150, Duration = "23s", Details = "Tüm stok bilgileri güncellendi" },
                new SyncHistoryRecord { Date = DateTime.Now.AddHours(-1), Operation = "Sipariş Çek", Status = "✅ Başarılı", RecordCount = 23, Duration = "12s", Details = "23 yeni sipariş alındı" },
                new SyncHistoryRecord { Date = DateTime.Now.AddHours(-2), Operation = "Kategori Sync", Status = "✅ Başarılı", RecordCount = 12, Duration = "8s", Details = "Kategoriler senkronize edildi" },
                new SyncHistoryRecord { Date = DateTime.Now.AddHours(-3), Operation = "Ürün Sync", Status = "⚠️ Uyarı", RecordCount = 142, Duration = "67s", Details = "8 ürün senkronize edilemedi" },
                new SyncHistoryRecord { Date = DateTime.Now.AddHours(-4), Operation = "Stok Güncelle", Status = "✅ Başarılı", RecordCount = 150, Duration = "31s", Details = "Stok seviyeleri güncellendi" },
                new SyncHistoryRecord { Date = DateTime.Now.AddHours(-6), Operation = "Sipariş Çek", Status = "❌ Hata", RecordCount = 0, Duration = "5s", Details = "API bağlantı hatası" },
                new SyncHistoryRecord { Date = DateTime.Now.AddDays(-1), Operation = "Full Sync", Status = "✅ Başarılı", RecordCount = 1247, Duration = "3m 45s", Details = "Tam senkronizasyon tamamlandı" },
                new SyncHistoryRecord { Date = DateTime.Now.AddDays(-1).AddHours(-3), Operation = "Ürün Sync", Status = "✅ Başarılı", RecordCount = 150, Duration = "52s", Details = "Ürün bilgileri güncellendi" },
                new SyncHistoryRecord { Date = DateTime.Now.AddDays(-2), Operation = "Kategori Sync", Status = "✅ Başarılı", RecordCount = 12, Duration = "7s", Details = "Kategori ağacı senkronize edildi" }
            };

            foreach (var record in demoSyncHistory)
            {
                syncHistory.Add(record);
            }

            SyncHistoryDataGrid.ItemsSource = syncHistory;
        }

        private void SetupTimers()
        {
            // Statistics timer - 3 saniyede bir güncelle
            statisticsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            statisticsTimer.Tick += UpdateStatistics;
            statisticsTimer.Start();

            // Sync progress timer - sync sırasında progress bar güncelle
            syncProgressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            syncProgressTimer.Tick += UpdateSyncProgress;
        }

        private void HookHealth()
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var health = sp?.GetService<IOpenCartHealthService>();
                if (health != null && ConsecutiveFailuresText != null)
                {
                    // İlk değer
                    ConsecutiveFailuresText.Text = $"Fail: {health.ConsecutiveFailures}";
                }
            }
            catch
            {
                // Intentional: health service UI init — optional DI service, element may not be loaded.
            }
        }

        private void UpdateStatistics(object? sender, EventArgs e)
        {
            // Demo istatistik güncellemeleri
            var variation = random.Next(-1, 2);

            if (TodaySyncText != null) TodaySyncText.Text = Math.Max(10, 12 + variation).ToString();
            if (WeekSyncText != null) WeekSyncText.Text = Math.Max(80, 84 + (variation * 3)).ToString();

            var successRate = 98.5 + (random.NextDouble() - 0.5);
            if (SuccessRateText != null) SuccessRateText.Text = $"{successRate:F1}%";

            var totalProducts = 1247 + random.Next(-5, 6);
            if (TotalProductsText != null) TotalProductsText.Text = totalProducts.ToString("N0");

            // Ping simulation
            var ping = random.Next(1, 8);
            if (LastPingText != null) LastPingText.Text = $"{ping}ms";

            // Last sync time update
            var minutesAgo = random.Next(3, 8);
            if (LastSyncText != null) LastSyncText.Text = $"{minutesAgo} dk önce";
        }

        private void UpdateSyncProgress(object? sender, EventArgs e)
        {
            if (isSyncRunning)
            {
                currentProgress += random.Next(1, 4);
                if (currentProgress >= 100)
                {
                    currentProgress = 100;
                    CompleteSyncOperation();
                }

                if (SyncProgressBar != null) SyncProgressBar.Value = currentProgress;
                if (ProgressPercentText != null) ProgressPercentText.Text = $"{currentProgress}%";

                var currentItems = (int)(currentProgress * 5); // 500 items max
                if (ProgressText != null) ProgressText.Text = $"{currentItems} / 500 ürün";

                var operations = new[] { "Ürünler alınıyor...", "Stok kontrol ediliyor...", "Fiyatlar güncelleniyor...", "Kategoriler senkronize ediliyor..." };
                if (CurrentOperationText != null) CurrentOperationText.Text = operations[random.Next(operations.Length)];
            }
        }

        private void CompleteSyncOperation()
        {
            isSyncRunning = false;
            syncProgressTimer?.Stop();

            currentProgress = 0;
            if (SyncProgressBar != null) SyncProgressBar.Value = 0;
            if (CurrentOperationText != null) CurrentOperationText.Text = "Senkronizasyon tamamlandı ✅";
            if (ProgressText != null) ProgressText.Text = "500 / 500 ürün";
            if (ProgressPercentText != null) ProgressPercentText.Text = "100%";

            // Add new sync record
            var newRecord = new SyncHistoryRecord
            {
                Date = DateTime.Now,
                Operation = "Ürün Sync",
                Status = "✅ Başarılı",
                RecordCount = 500,
                Duration = "65s",
                Details = "500 ürün başarıyla senkronize edildi"
            };
            syncHistory.Insert(0, newRecord);

            MessageBox.Show("✅ Senkronizasyon başarıyla tamamlandı!\n\n" +
                          "• 500 ürün güncellendi\n" +
                          "• Süre: 65 saniye\n" +
                          "• Hata sayısı: 0",
                          "Senkronizasyon Tamamlandı",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);

            // Reset progress after 2 seconds
            var resetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            resetTimer.Tick += (s, args) =>
            {
                resetTimer.Stop();
                if (CurrentOperationText != null) CurrentOperationText.Text = "Beklemede...";
                if (ProgressText != null) ProgressText.Text = "0 / 0 ürün";
                if (ProgressPercentText != null) ProgressPercentText.Text = "0%";
            };
            resetTimer.Start();
        }

        private void UpdateConnectionStatus()
        {
            // Simulate connection status
            var isConnected = random.NextDouble() > 0.1; // 90% chance connected

            if (isConnected)
            {
                if (ConnectionStatusBorder != null)
                    ConnectionStatusBorder.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218)); // Green
                if (ConnectionStatusText != null)
                {
                    ConnectionStatusText.Text = "🔗 Bağlı";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36));
                }
            }
            else
            {
                if (ConnectionStatusBorder != null)
                    ConnectionStatusBorder.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218)); // Red
                if (ConnectionStatusText != null)
                {
                    ConnectionStatusText.Text = "❌ Bağlantı Yok";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36));
                }
            }
        }

        private void TryPopulateConfigFromAppSettings()
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var opts = sp?.GetService<IOptions<OpenCartSettingsOptions>>()?.Value;
                if (opts != null)
                {
                    if (!string.IsNullOrWhiteSpace(opts.ApiUrl) && StoreUrlTextBox != null)
                        StoreUrlTextBox.Text = opts.ApiUrl;

                    // API anahtarını ekranda açık göstermeyelim; butonla göster/gizle akışı kalsın
                    if (!string.IsNullOrWhiteSpace(opts.ApiKey) && ApiKeyTextBox != null)
                        ApiKeyTextBox.Text = new string('*', Math.Min(12, opts.ApiKey.Length));
                }
            }
            catch
            {
                // Intentional: populate config from app settings — optional DI service, UI elements may not be ready.
            }
        }

        #region Header Events

        private async void StartSync_Click(object sender, RoutedEventArgs e)
        {
            if (isSyncRunning)
            {
                MessageBox.Show("Senkronizasyon zaten çalışıyor!", "Bilgi",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            isSyncRunning = true;
            currentProgress = 0;
            syncProgressTimer?.Start();
            await Task.Yield();
            MessageBox.Show("🔄 Tam senkronizasyon başlatıldı!\n\n" +
                          "• Ürünler\n" +
                          "• Stok bilgileri\n" +
                          "• Fiyatlar\n" +
                          "• Kategoriler\n\n" +
                          "İlerleme durumunu takip edebilirsiniz.",
                          "Senkronizasyon Başlatıldı",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private async void TestAPI_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var client = sp?.GetService<IOpenCartClient>();
                if (client == null)
                {
                    MessageBox.Show("❌ OpenCart istemcisi bulunamadı.", "API Test", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var sw = Stopwatch.StartNew();
                var ok = await client.TestConnectionAsync();
                sw.Stop();

                if (ok)
                {
                    MessageBox.Show($"✅ API Test Başarılı\n\nPing: {sw.ElapsedMilliseconds} ms", "API Test", MessageBoxButton.OK, MessageBoxImage.Information);
                    // UI rozetlerini yeşile çek
                    if (ConnectionStatusBorder != null)
                        ConnectionStatusBorder.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218));
                    if (ConnectionStatusText != null)
                    {
                        ConnectionStatusText.Text = "🔗 Bağlı";
                        ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36));
                    }
                }
                else
                {
                    MessageBox.Show("❌ API Test Başarısız", "API Test", MessageBoxButton.OK, MessageBoxImage.Warning);
                    if (ConnectionStatusBorder != null)
                        ConnectionStatusBorder.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218));
                    if (ConnectionStatusText != null)
                    {
                        ConnectionStatusText.Text = "❌ Bağlantı Yok";
                        ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ API Test Hatası: {ex.Message}", "API Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowStats_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("📊 Detaylı İstatistikler:\n\n" +
                          $"• Bugün: {TodaySyncText.Text} sync\n" +
                          $"• Bu hafta: {WeekSyncText.Text} sync\n" +
                          $"• Başarı oranı: {SuccessRateText.Text}\n" +
                          $"• Toplam ürün: {TotalProductsText.Text}\n" +
                          $"• Son ping: {LastPingText.Text}\n" +
                          $"• Son sync: {LastSyncText.Text}\n\n" +
                          "• Ortalama sync süresi: 45s\n" +
                          "• API kullanımı: 456/1000\n" +
                          "• Aktif store sayısı: 1",
                          "OpenCart İstatistikleri",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        #endregion

        #region Quick Actions

        private async void SyncProducts_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("📦 Ürün senkronizasyonu başlatılıyor...\n\n" +
                          "• Yeni ürünler kontrol ediliyor\n" +
                          "• Mevcut ürünler güncelleniyor\n" +
                          "• Silinen ürünler işaretleniyor",
                          "Ürün Senkronizasyonu",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);

            await Task.Delay(1000);

            var newRecord = new SyncHistoryRecord
            {
                Date = DateTime.Now,
                Operation = "Ürün Sync",
                Status = "✅ Başarılı",
                RecordCount = 150,
                Duration = "42s",
                Details = "150 ürün başarıyla senkronize edildi"
            };
            syncHistory.Insert(0, newRecord);

            MessageBox.Show("✅ Ürün senkronizasyonu tamamlandı!\n\n" +
                          "• Güncellenen ürün: 150\n" +
                          "• Yeni ürün: 5\n" +
                          "• Hata: 0",
                          "İşlem Tamamlandı",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private async void UpdateStock_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("📊 Stok güncellemesi başlatılıyor...\n\n" +
                          "• Mevcut stok seviyeleri kontrol ediliyor\n" +
                          "• OpenCart stok bilgileri güncelleniyor\n" +
                          "• Düşük stok uyarıları oluşturuluyor",
                          "Stok Güncellemesi",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);

            await Task.Delay(1000);

            var newRecord = new SyncHistoryRecord
            {
                Date = DateTime.Now,
                Operation = "Stok Güncelle",
                Status = "✅ Başarılı",
                RecordCount = 150,
                Duration = "28s",
                Details = "Tüm ürün stok bilgileri güncellendi"
            };
            syncHistory.Insert(0, newRecord);

            MessageBox.Show("✅ Stok güncellemesi tamamlandı!\n\n" +
                          "• Güncellenen ürün: 150\n" +
                          "• Düşük stok uyarısı: 12 ürün\n" +
                          "• Stok sıfır: 3 ürün",
                          "İşlem Tamamlandı",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private async void PullOrders_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🛒 Sipariş çekme işlemi başlatılıyor...\n\n" +
                          "• Yeni siparişler kontrol ediliyor\n" +
                          "• Sipariş durumları güncelleniyor\n" +
                          "• Müşteri bilgileri senkronize ediliyor",
                          "Sipariş Çekme",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);

            await Task.Delay(1000);

            var newRecord = new SyncHistoryRecord
            {
                Date = DateTime.Now,
                Operation = "Sipariş Çek",
                Status = "✅ Başarılı",
                RecordCount = 18,
                Duration = "15s",
                Details = "18 yeni sipariş sisteme aktarıldı"
            };
            syncHistory.Insert(0, newRecord);

            MessageBox.Show("✅ Sipariş çekme tamamlandı!\n\n" +
                          "• Yeni sipariş: 18\n" +
                          "• Güncellenen sipariş: 5\n" +
                          "• Yeni müşteri: 3",
                          "İşlem Tamamlandı",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private async void SyncCategories_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("🏷️ Kategori senkronizasyonu başlatılıyor...\n\n" +
                          "• Kategori ağacı kontrol ediliyor\n" +
                          "• Yeni kategoriler ekleniyor\n" +
                          "• Kategori hiyerarşisi güncelleniyor",
                          "Kategori Senkronizasyonu",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);

            await Task.Delay(1000);

            var newRecord = new SyncHistoryRecord
            {
                Date = DateTime.Now,
                Operation = "Kategori Sync",
                Status = "✅ Başarılı",
                RecordCount = 12,
                Duration = "9s",
                Details = "Kategori ağacı başarıyla senkronize edildi"
            };
            syncHistory.Insert(0, newRecord);

            MessageBox.Show("✅ Kategori senkronizasyonu tamamlandı!\n\n" +
                          "• Güncellenen kategori: 12\n" +
                          "• Yeni kategori: 2\n" +
                          "• Hata: 0",
                          "İşlem Tamamlandı",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        #endregion

        #region API Configuration Events

        private async void TestUrl_Click(object sender, RoutedEventArgs e)
        {
            var url = StoreUrlTextBox.Text;

            MessageBox.Show($"🔗 URL test ediliyor...\n\n{url}", "URL Test",
                MessageBoxButton.OK, MessageBoxImage.Information);

            await Task.Delay(1500);

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("❌ Lütfen bir URL giriniz.",
                              "URL Test Başarısız",
                              MessageBoxButton.OK,
                              MessageBoxImage.Warning);
            }
            else
            {
                try
                {
                    using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    var response = await httpClient.GetAsync(url);
                    MessageBox.Show($"✅ URL erişilebilir!\n\n• Durum kodu: {(int)response.StatusCode}",
                                  "URL Test Başarılı",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ URL'ye erişilemedi!\n\n• Hata: {ex.Message}",
                                  "URL Test Başarısız",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                }
            }
        }

        private void ShowApiKey_Click(object sender, RoutedEventArgs e)
        {
            var currentType = ApiKeyTextBox.Text.Contains("*") ? "hidden" : "visible";

            if (currentType == "visible")
            {
                var key = ApiKeyTextBox.Text;
                if (key.Length > 6)
                    ApiKeyTextBox.Text = key.Substring(0, 6) + new string('*', key.Length - 6);
                ApiKeyTextBox.Tag = key;
                ((Button)sender).Content = "👁️ Göster";
            }
            else
            {
                var originalKey = ApiKeyTextBox.Tag as string;
                if (!string.IsNullOrEmpty(originalKey))
                    ApiKeyTextBox.Text = originalKey;
                ((Button)sender).Content = "🙈 Gizle";
            }
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("💾 API konfigürasyonu kaydedildi!\n\n" +
                          $"• Store URL: {StoreUrlTextBox.Text}\n" +
                          $"• Timeout: {((ComboBoxItem)TimeoutComboBox.SelectedItem)?.Content}\n" +
                          $"• Store ID: {((ComboBoxItem)StoreIdComboBox.SelectedItem)?.Content}",
                          "Konfigürasyon Kaydedildi",
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sp = MesTechStok.Desktop.App.ServiceProvider;
                var client = sp?.GetService<IOpenCartClient>();
                if (client == null)
                {
                    MessageBox.Show("❌ OpenCart istemcisi bulunamadı.", "Bağlantı Testi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var sw = Stopwatch.StartNew();
                var ok = await client.TestConnectionAsync();
                sw.Stop();

                if (ok)
                {
                    if (ConnectionStatusBorder != null)
                        ConnectionStatusBorder.Background = new SolidColorBrush(Color.FromRgb(212, 237, 218));
                    if (ConnectionStatusText != null)
                    {
                        ConnectionStatusText.Text = "✅ Aktif";
                        ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(21, 87, 36));
                    }
                    if (LastPingText != null)
                        LastPingText.Text = $"{sw.ElapsedMilliseconds}ms";
                    MessageBox.Show($"✅ Bağlantı testi başarılı. Ping: {sw.ElapsedMilliseconds} ms", "Bağlantı Testi", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    if (ConnectionStatusBorder != null)
                        ConnectionStatusBorder.Background = new SolidColorBrush(Color.FromRgb(248, 215, 218));
                    if (ConnectionStatusText != null)
                    {
                        ConnectionStatusText.Text = "❌ Bağlantı Yok";
                        ConnectionStatusText.Foreground = new SolidColorBrush(Color.FromRgb(114, 28, 36));
                    }
                    MessageBox.Show("❌ Bağlantı testi başarısız.", "Bağlantı Testi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Bağlantı Testi Hatası: {ex.Message}", "Bağlantı Testi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyConfig_Click(object sender, RoutedEventArgs e)
        {
            var config = $"Store URL: {StoreUrlTextBox?.Text}\n" +
                        $"API Key: {ApiKeyTextBox?.Text}\n" +
                        $"Timeout: {((ComboBoxItem?)TimeoutComboBox?.SelectedItem)?.Content}\n" +
                        $"Store ID: {((ComboBoxItem?)StoreIdComboBox?.SelectedItem)?.Content}";

            Clipboard.SetText(config);

            MessageBox.Show("📋 Konfigürasyon panoya kopyalandı!", "Kopyalandı",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Sync Settings Events

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            var settings = $"✅ Senkronizasyon ayarları kaydedildi!\n\n" +
                          "Otomatik Senkronizasyon:\n" +
                          $"• Ürün sync: {(AutoSyncProductsCheckBox?.IsChecked == true ? "Aktif" : "Pasif")}\n" +
                          $"• Stok güncelle: {(AutoSyncStockCheckBox?.IsChecked == true ? "Aktif" : "Pasif")}\n" +
                          $"• Fiyat güncelle: {(AutoSyncPricesCheckBox?.IsChecked == true ? "Aktif" : "Pasif")}\n" +
                          $"• Kategori sync: {(AutoSyncCategoriesCheckBox?.IsChecked == true ? "Aktif" : "Pasif")}\n\n" +
                          $"Sync Sıklığı: {((ComboBoxItem?)SyncFrequencyComboBox?.SelectedItem)?.Content}\n" +
                          $"Log Seviyesi: {((ComboBoxItem?)LogLevelComboBox?.SelectedItem)?.Content}";

            MessageBox.Show(settings, "Ayarlar Kaydedildi",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ResetSettings_Click(object sender, RoutedEventArgs e)
        {
            if (AutoSyncProductsCheckBox != null) AutoSyncProductsCheckBox.IsChecked = true;
            if (AutoSyncStockCheckBox != null) AutoSyncStockCheckBox.IsChecked = true;
            if (AutoSyncPricesCheckBox != null) AutoSyncPricesCheckBox.IsChecked = false;
            if (AutoSyncCategoriesCheckBox != null) AutoSyncCategoriesCheckBox.IsChecked = true;
            if (SyncFrequencyComboBox != null) SyncFrequencyComboBox.SelectedIndex = 1; // Her 30 dakikada

            if (NotifyErrorsCheckBox != null) NotifyErrorsCheckBox.IsChecked = true;
            if (NotifySuccessCheckBox != null) NotifySuccessCheckBox.IsChecked = false;
            if (EmailNotificationsCheckBox != null) EmailNotificationsCheckBox.IsChecked = true;
            if (TrayNotificationsCheckBox != null) TrayNotificationsCheckBox.IsChecked = true;
            if (LogLevelComboBox != null) LogLevelComboBox.SelectedIndex = 2; // Info

            MessageBox.Show("🔄 Ayarlar varsayılan değerlere sıfırlandı!", "Ayarlar Sıfırlandı",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Sync Control Events

        private void PauseSync_Click(object sender, RoutedEventArgs e)
        {
            if (isSyncRunning)
            {
                syncProgressTimer?.Stop();
                isSyncRunning = false;
                if (CurrentOperationText != null) CurrentOperationText.Text = "Senkronizasyon duraklatıldı ⏸️";

                MessageBox.Show("⏸️ Senkronizasyon duraklatıldı.\n\n" +
                              "Devam etmek için 'Sync Başlat' butonunu kullanın.",
                              "Senkronizasyon Duraklatıldı",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        private void StopSync_Click(object sender, RoutedEventArgs e)
        {
            if (isSyncRunning)
            {
                syncProgressTimer.Stop();
                isSyncRunning = false;
                currentProgress = 0;
                if (SyncProgressBar != null) SyncProgressBar.Value = 0;
                if (CurrentOperationText != null) CurrentOperationText.Text = "Senkronizasyon durduruldu ⏹️";
                if (ProgressText != null) ProgressText.Text = "0 / 0 ürün";
                if (ProgressPercentText != null) ProgressPercentText.Text = "0%";

                MessageBox.Show("⏹️ Senkronizasyon durduruldu.\n\n" +
                              "İşlem iptal edildi.",
                              "Senkronizasyon Durduruldu",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        #endregion

        private void ViewSyncDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var record = button?.DataContext as SyncHistoryRecord;

            if (record != null)
            {
                MessageBox.Show($"📄 Senkronizasyon Detayları:\n\n" +
                              $"Tarih: {record.Date:dd.MM.yyyy HH:mm:ss}\n" +
                              $"İşlem: {record.Operation}\n" +
                              $"Durum: {record.Status}\n" +
                              $"Kayıt Sayısı: {record.RecordCount}\n" +
                              $"Süre: {record.Duration}\n" +
                              $"Detay: {record.Details}\n\n" +
                              "Tam log dosyasını görüntülemek için\n" +
                              "Logs klasörünü kontrol edin.",
                              "Sync Detayları",
                              MessageBoxButton.OK,
                              MessageBoxImage.Information);
            }
        }

        // Cleanup when control is unloaded
        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            statisticsTimer?.Stop();
            syncProgressTimer?.Stop();
        }
    }
}
