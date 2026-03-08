using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MesTechStok.Desktop.Utils;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// MesaAIToolsView - MESA AI Tools Dashboard
    /// Yapay zeka destekli stok optimizasyonu ve tahminleme araclari
    /// NOTE: Mock data used — DEV 6 owns real IMesaAIService/IMesaBotService integration
    /// </summary>
    public partial class MesaAIToolsView : UserControl, INotifyPropertyChanged
    {
        #region Nested Types

        public class AIActivityItem
        {
            public DateTime Date { get; set; }
            public string Operation { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string Result { get; set; } = string.Empty;
            public string Duration { get; set; } = string.Empty;

            public string FormattedDate => Date.ToString("dd.MM.yyyy HH:mm:ss");
        }

        #endregion

        #region Private Fields

        private readonly ObservableCollection<AIActivityItem> _activityLog;
        private bool _isLoading;
        private bool _isMesaConnected;
        private string _mesaVersion = "MESA v2.1 — GPT-4 Tabanli";
        private string _lastSyncTime = "--";
        private string _predictionAccuracy = "%94.2";
        private string _processedProducts = "1,423";
        private string _autoOrders = "234";

        #endregion

        #region Properties

        public ObservableCollection<AIActivityItem> ActivityLog => _activityLog;

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (LoadingOverlay != null)
                    {
                        LoadingOverlay.Visibility = _isLoading ? Visibility.Visible : Visibility.Collapsed;
                    }
                });
            }
        }

        public bool IsMesaConnected
        {
            get => _isMesaConnected;
            set
            {
                _isMesaConnected = value;
                OnPropertyChanged();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (ConnectionIndicator != null)
                    {
                        ConnectionIndicator.Fill = _isMesaConnected
                            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81))
                            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44));
                    }

                    if (ConnectionStatusText != null)
                    {
                        ConnectionStatusText.Text = _isMesaConnected ? "MESA Baglantisi Aktif" : "Baglanti Yok";
                        ConnectionStatusText.Foreground = _isMesaConnected
                            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81))
                            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44));
                    }
                });
            }
        }

        public string MesaVersion
        {
            get => _mesaVersion;
            set
            {
                _mesaVersion = value;
                OnPropertyChanged();
                if (ModelVersionText != null) ModelVersionText.Text = value;
            }
        }

        public string LastSyncTime
        {
            get => _lastSyncTime;
            set
            {
                _lastSyncTime = value;
                OnPropertyChanged();
                if (LastSyncText != null) LastSyncText.Text = value;
            }
        }

        public string PredictionAccuracy
        {
            get => _predictionAccuracy;
            set
            {
                _predictionAccuracy = value;
                OnPropertyChanged();
                if (PredictionAccuracyText != null) PredictionAccuracyText.Text = value;
            }
        }

        public string ProcessedProducts
        {
            get => _processedProducts;
            set
            {
                _processedProducts = value;
                OnPropertyChanged();
                if (ProcessedProductsText != null) ProcessedProductsText.Text = value;
            }
        }

        public string AutoOrders
        {
            get => _autoOrders;
            set
            {
                _autoOrders = value;
                OnPropertyChanged();
                if (AutoOrdersText != null) AutoOrdersText.Text = value;
            }
        }

        #endregion

        #region Constructor

        public MesaAIToolsView()
        {
            _activityLog = new ObservableCollection<AIActivityItem>();

            InitializeComponent();
            DataContext = this;

            ActivityDataGrid.ItemsSource = _activityLog;

            _ = InitializeAsync();
        }

        #endregion

        #region Initialization

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;

                // Simulate MESA connection check (mock: always connected)
                await Task.Delay(500);
                IsMesaConnected = true;
                MesaVersion = "MESA v2.1 — GPT-4 Tabanli";
                LastSyncTime = DateTime.Now.AddMinutes(-12).ToString("dd.MM.yyyy HH:mm");

                // Load mock stats
                PredictionAccuracy = "%94.2";
                ProcessedProducts = "1,423";
                AutoOrders = "234";

                // Load activity log
                LoadActivityLog();

                GlobalLogger.Instance.LogInfo("MesaAIToolsView basariyla baslatildi", "MesaAIToolsView");
                ToastManager.ShowSuccess("MESA AI Araclari yuklendi!", "MESA AI");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"MesaAIToolsView baslatma hatasi: {ex.Message}", "MesaAIToolsView");
                ToastManager.ShowError("MESA AI Araclari yuklenirken hata olustu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void LoadActivityLog()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _activityLog.Clear();

                var mockActivities = new[]
                {
                    new AIActivityItem
                    {
                        Date = DateTime.Now.AddMinutes(-15),
                        Operation = "Stok Tahminleme",
                        Status = "Basarili",
                        Result = "142 urun icin 30 gunluk tahmin olusturuldu",
                        Duration = "2.3s"
                    },
                    new AIActivityItem
                    {
                        Date = DateTime.Now.AddMinutes(-45),
                        Operation = "Fiyat Optimizasyonu",
                        Status = "Basarili",
                        Result = "38 urun icin fiyat onerisi hesaplandi (%12 kar artisi)",
                        Duration = "4.1s"
                    },
                    new AIActivityItem
                    {
                        Date = DateTime.Now.AddHours(-2),
                        Operation = "Anomali Tespiti",
                        Status = "Uyari",
                        Result = "3 urunde olagan disi stok hareketi tespit edildi",
                        Duration = "1.8s"
                    },
                    new AIActivityItem
                    {
                        Date = DateTime.Now.AddHours(-4),
                        Operation = "AI Urun Aciklamasi",
                        Status = "Basarili",
                        Result = "56 urun aciklamasi otomatik olusturuldu",
                        Duration = "8.5s"
                    },
                    new AIActivityItem
                    {
                        Date = DateTime.Now.AddHours(-6),
                        Operation = "Talep Tahmini",
                        Status = "Basarili",
                        Result = "Haftalik talep raporu olusturuldu (dogruluk: %91.7)",
                        Duration = "3.2s"
                    },
                    new AIActivityItem
                    {
                        Date = DateTime.Now.AddDays(-1),
                        Operation = "Bot Yapilandirmasi",
                        Status = "Basarili",
                        Result = "Otomatik siparis botu guncellendi (esik: 10 adet)",
                        Duration = "0.4s"
                    }
                };

                foreach (var activity in mockActivities)
                {
                    _activityLog.Add(activity);
                }

                if (ActivityCountText != null)
                {
                    ActivityCountText.Text = $"({_activityLog.Count} islem)";
                }
            });
        }

        #endregion

        #region Event Handlers

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await InitializeAsync();
                GlobalLogger.Instance.LogInfo("MESA AI verileri yenilendi", "MesaAIToolsView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"MESA AI yenileme hatasi: {ex.Message}", "MesaAIToolsView");
                ToastManager.ShowError("Yenileme sirasinda hata olustu!", "Hata");
            }
        }

        private async void RunStockPrediction_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsLoading = true;
                GlobalLogger.Instance.LogInfo("Stok tahminleme basladi", "MesaAIToolsView");

                // Simulate AI processing
                await Task.Delay(1500);

                var result = new AIActivityItem
                {
                    Date = DateTime.Now,
                    Operation = "Stok Tahminleme",
                    Status = "Basarili",
                    Result = "187 urun icin 30 gunluk stok tahmini olusturuldu. 12 urunde kritik stok uyarisi.",
                    Duration = "1.5s"
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _activityLog.Insert(0, result);
                    if (ActivityCountText != null)
                        ActivityCountText.Text = $"({_activityLog.Count} islem)";
                });

                ToastManager.ShowSuccess("Stok tahminleme tamamlandi! 187 urun analiz edildi.", "Stok Tahminleme");
                GlobalLogger.Instance.LogInfo("Stok tahminleme tamamlandi: 187 urun", "MesaAIToolsView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Stok tahminleme hatasi: {ex.Message}", "MesaAIToolsView");
                ToastManager.ShowError("Stok tahminleme sirasinda hata olustu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void RunDescriptionGenerator_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsLoading = true;
                GlobalLogger.Instance.LogInfo("AI urun aciklama olusturma basladi", "MesaAIToolsView");

                await Task.Delay(1500);

                var result = new AIActivityItem
                {
                    Date = DateTime.Now,
                    Operation = "AI Urun Aciklamasi",
                    Status = "Basarili",
                    Result = "64 urun icin SEO uyumlu aciklama olusturuldu. Kategori: Elektronik (28), Giyim (22), Ev (14).",
                    Duration = "1.5s"
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _activityLog.Insert(0, result);
                    if (ActivityCountText != null)
                        ActivityCountText.Text = $"({_activityLog.Count} islem)";
                });

                ToastManager.ShowSuccess("Urun aciklamalari olusturuldu! 64 urun guncellendi.", "AI Aciklama");
                GlobalLogger.Instance.LogInfo("AI urun aciklama tamamlandi: 64 urun", "MesaAIToolsView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"AI aciklama olusturma hatasi: {ex.Message}", "MesaAIToolsView");
                ToastManager.ShowError("Aciklama olusturma sirasinda hata olustu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void RunPriceOptimization_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsLoading = true;
                GlobalLogger.Instance.LogInfo("Fiyat optimizasyonu basladi", "MesaAIToolsView");

                await Task.Delay(1500);

                var result = new AIActivityItem
                {
                    Date = DateTime.Now,
                    Operation = "Fiyat Optimizasyonu",
                    Status = "Basarili",
                    Result = "45 urun icin fiyat onerisi hesaplandi. Tahmini kar artisi: %8.3, ortalama fiyat degisimi: %4.2.",
                    Duration = "1.5s"
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _activityLog.Insert(0, result);
                    if (ActivityCountText != null)
                        ActivityCountText.Text = $"({_activityLog.Count} islem)";
                });

                ToastManager.ShowSuccess("Fiyat optimizasyonu tamamlandi! 45 urun icin oneri hazir.", "Fiyat Optimizasyonu");
                GlobalLogger.Instance.LogInfo("Fiyat optimizasyonu tamamlandi: 45 urun", "MesaAIToolsView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Fiyat optimizasyonu hatasi: {ex.Message}", "MesaAIToolsView");
                ToastManager.ShowError("Fiyat optimizasyonu sirasinda hata olustu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void RunAnomalyDetection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsLoading = true;
                GlobalLogger.Instance.LogInfo("Anomali tespiti basladi", "MesaAIToolsView");

                await Task.Delay(1500);

                var result = new AIActivityItem
                {
                    Date = DateTime.Now,
                    Operation = "Anomali Tespiti",
                    Status = "Uyari",
                    Result = "1,423 stok hareketi taranadi. 5 urunde olagan disi desen tespit edildi. Detay raporu olusturuldu.",
                    Duration = "1.5s"
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _activityLog.Insert(0, result);
                    if (ActivityCountText != null)
                        ActivityCountText.Text = $"({_activityLog.Count} islem)";
                });

                ToastManager.ShowWarning("Anomali tespiti tamamlandi! 5 urunde uyari tespit edildi.", "Anomali Tespiti");
                GlobalLogger.Instance.LogInfo("Anomali tespiti tamamlandi: 5 uyari", "MesaAIToolsView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Anomali tespiti hatasi: {ex.Message}", "MesaAIToolsView");
                ToastManager.ShowError("Anomali tespiti sirasinda hata olustu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void RunDemandForecast_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsLoading = true;
                GlobalLogger.Instance.LogInfo("Talep tahmini basladi", "MesaAIToolsView");

                await Task.Delay(1500);

                var result = new AIActivityItem
                {
                    Date = DateTime.Now,
                    Operation = "Talep Tahmini",
                    Status = "Basarili",
                    Result = "Gelecek 4 hafta icin talep tahmini olusturuldu. Dogruluk: %92.1, yuksek talep: 23 urun.",
                    Duration = "1.5s"
                };

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _activityLog.Insert(0, result);
                    if (ActivityCountText != null)
                        ActivityCountText.Text = $"({_activityLog.Count} islem)";
                });

                ToastManager.ShowSuccess("Talep tahmini tamamlandi! 4 haftalik tahmin hazir.", "Talep Tahmini");
                GlobalLogger.Instance.LogInfo("Talep tahmini tamamlandi: dogruluk %92.1", "MesaAIToolsView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Talep tahmini hatasi: {ex.Message}", "MesaAIToolsView");
                ToastManager.ShowError("Talep tahmini sirasinda hata olustu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ConfigureBot_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GlobalLogger.Instance.LogInfo("Bot yapilandirma dialog acildi", "MesaAIToolsView");

                // Mock bot configuration dialog
                var result = MessageBox.Show(
                    "MESA Bot Yapilandirmasi\n\n" +
                    "Aktif Botlar:\n" +
                    "  - Otomatik Siparis Botu (Aktif, esik: 10 adet)\n" +
                    "  - Stok Uyari Botu (Aktif, kritik seviye: 5)\n" +
                    "  - Fiyat Izleme Botu (Aktif, degisim esigi: %5)\n" +
                    "  - Raporlama Botu (Aktif, gunluk 09:00)\n\n" +
                    "Bot ayarlarini sifirlamak ister misiniz?",
                    "MESA Bot Yonetimi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    var activity = new AIActivityItem
                    {
                        Date = DateTime.Now,
                        Operation = "Bot Yapilandirmasi",
                        Status = "Basarili",
                        Result = "Tum bot ayarlari varsayilana sifirlandi.",
                        Duration = "0.2s"
                    };

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _activityLog.Insert(0, activity);
                        if (ActivityCountText != null)
                            ActivityCountText.Text = $"({_activityLog.Count} islem)";
                    });

                    ToastManager.ShowSuccess("Bot ayarlari varsayilana sifirlandi.", "Bot Yonetimi");
                    GlobalLogger.Instance.LogInfo("Bot ayarlari sifirlandi", "MesaAIToolsView");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Bot yapilandirma hatasi: {ex.Message}", "MesaAIToolsView");
                ToastManager.ShowError("Bot yapilandirmasi sirasinda hata olustu!", "Hata");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
