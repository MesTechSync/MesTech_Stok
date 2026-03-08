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
    /// CargoShipmentView - Kargo Gonderim Yonetimi
    /// Siparisleri kargoya verme, etiket yazdirma ve takip ekrani
    /// </summary>
    public partial class CargoShipmentView : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        private readonly ObservableCollection<ShipmentItem> _shipments;
        private readonly DispatcherTimer _refreshTimer;

        private bool _isLoading;
        private int _pendingCount;
        private int _shippedCount;
        private int _deliveredCount;
        private int _returnedCount;

        #endregion

        #region Properties

        public ObservableCollection<ShipmentItem> Shipments => _shipments;

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

        public int PendingCount
        {
            get => _pendingCount;
            set { _pendingCount = value; OnPropertyChanged(); }
        }

        public int ShippedCount
        {
            get => _shippedCount;
            set { _shippedCount = value; OnPropertyChanged(); }
        }

        public int DeliveredCount
        {
            get => _deliveredCount;
            set { _deliveredCount = value; OnPropertyChanged(); }
        }

        public int ReturnedCount
        {
            get => _returnedCount;
            set { _returnedCount = value; OnPropertyChanged(); }
        }

        #endregion

        #region Constructor

        public CargoShipmentView()
        {
            _shipments = new ObservableCollection<ShipmentItem>();

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _refreshTimer.Tick += async (s, e) =>
            {
                await LoadShipmentsAsync();
                GlobalLogger.Instance.LogInfo("Cargo shipments auto-refreshed", "CargoShipmentView");
            };

            InitializeComponent();
            DataContext = this;

            ShipmentsDataGrid.ItemsSource = _shipments;

            _ = InitializeAsync();
        }

        #endregion

        #region Initialization

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;

                await LoadShipmentsAsync();

                _refreshTimer.Start();

                GlobalLogger.Instance.LogInfo("CargoShipmentView basariyla baslatildi", "CargoShipmentView");
                ToastManager.ShowSuccess("📦 Kargo gonderim sistemi yuklendi!", "Kargo Yonetimi");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"CargoShipmentView baslatma hatasi: {ex.Message}", "CargoShipmentView");
                ToastManager.ShowError("❌ Kargo sistemi yuklenirken hata olustu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Data Loading

        private async Task LoadShipmentsAsync()
        {
            try
            {
                await Task.Delay(200); // Simulate async loading

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _shipments.Clear();

                    // Mock data — 8 items across 4 platforms
                    _shipments.Add(new ShipmentItem
                    {
                        OrderNumber = "TY-100201",
                        Platform = "Trendyol",
                        CustomerName = "Ahmet Yilmaz",
                        ProductSummary = "Samsung Galaxy S24 Ultra",
                        Status = "Bekleyen",
                        TrackingNumber = "",
                        OrderDate = DateTime.Now.AddDays(-1)
                    });
                    _shipments.Add(new ShipmentItem
                    {
                        OrderNumber = "TY-100202",
                        Platform = "Trendyol",
                        CustomerName = "Fatma Demir",
                        ProductSummary = "Apple iPhone 15 Pro",
                        Status = "Kargoda",
                        TrackingNumber = "YK-7890123456",
                        OrderDate = DateTime.Now.AddDays(-2)
                    });
                    _shipments.Add(new ShipmentItem
                    {
                        OrderNumber = "CS-200301",
                        Platform = "Ciceksepeti",
                        CustomerName = "Mehmet Kaya",
                        ProductSummary = "Orkide Buket + Cikolata",
                        Status = "Bekleyen",
                        TrackingNumber = "",
                        OrderDate = DateTime.Now.AddHours(-5)
                    });
                    _shipments.Add(new ShipmentItem
                    {
                        OrderNumber = "CS-200302",
                        Platform = "Ciceksepeti",
                        CustomerName = "Zeynep Aksoy",
                        ProductSummary = "Kirmizi Gul Demeti",
                        Status = "Teslim Edildi",
                        TrackingNumber = "AR-3456789012",
                        OrderDate = DateTime.Now.AddDays(-5)
                    });
                    _shipments.Add(new ShipmentItem
                    {
                        OrderNumber = "HB-300401",
                        Platform = "Hepsiburada",
                        CustomerName = "Ali Ozturk",
                        ProductSummary = "Dyson V15 Detect Supurge",
                        Status = "Kargoda",
                        TrackingNumber = "SK-5678901234",
                        OrderDate = DateTime.Now.AddDays(-3)
                    });
                    _shipments.Add(new ShipmentItem
                    {
                        OrderNumber = "HB-300402",
                        Platform = "Hepsiburada",
                        CustomerName = "Ayse Celik",
                        ProductSummary = "Sony WH-1000XM5 Kulaklik",
                        Status = "Iade",
                        TrackingNumber = "YK-1122334455",
                        OrderDate = DateTime.Now.AddDays(-7)
                    });
                    _shipments.Add(new ShipmentItem
                    {
                        OrderNumber = "OC-400501",
                        Platform = "OpenCart",
                        CustomerName = "Mustafa Sahin",
                        ProductSummary = "Logitech MX Master 3S",
                        Status = "Bekleyen",
                        TrackingNumber = "",
                        OrderDate = DateTime.Now.AddHours(-2)
                    });
                    _shipments.Add(new ShipmentItem
                    {
                        OrderNumber = "OC-400502",
                        Platform = "OpenCart",
                        CustomerName = "Elif Yildiz",
                        ProductSummary = "Mechanical Keyboard Set",
                        Status = "Teslim Edildi",
                        TrackingNumber = "AR-9988776655",
                        OrderDate = DateTime.Now.AddDays(-4)
                    });

                    UpdateStats();

                    if (ShipmentCountText != null)
                    {
                        ShipmentCountText.Text = $"({_shipments.Count} gonderi)";
                    }
                });

                GlobalLogger.Instance.LogInfo($"Kargo verileri yuklendi: {_shipments.Count} gonderi", "CargoShipmentView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Kargo verileri yukleme hatasi: {ex.Message}", "CargoShipmentView");
                ToastManager.ShowError("❌ Kargo verileri yuklenirken hata olustu!", "Hata");
            }
        }

        private void UpdateStats()
        {
            PendingCount = _shipments.Count(s => s.Status == "Bekleyen");
            ShippedCount = _shipments.Count(s => s.Status == "Kargoda");
            DeliveredCount = _shipments.Count(s => s.Status == "Teslim Edildi");
            ReturnedCount = _shipments.Count(s => s.Status == "Iade");

            if (PendingCountText != null) PendingCountText.Text = PendingCount.ToString();
            if (ShippedCountText != null) ShippedCountText.Text = ShippedCount.ToString();
            if (DeliveredCountText != null) DeliveredCountText.Text = DeliveredCount.ToString();
            if (ReturnedCountText != null) ReturnedCountText.Text = ReturnedCount.ToString();
        }

        #endregion

        #region Event Handlers

        private void CargoProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LabelFormatComboBox == null) return;

            var selectedItem = CargoProviderComboBox.SelectedItem as ComboBoxItem;
            var provider = selectedItem?.Content?.ToString() ?? string.Empty;

            // Reset label format options based on provider
            LabelFormatComboBox.Items.Clear();

            switch (provider)
            {
                case "Yurtici Kargo":
                    LabelFormatComboBox.Items.Add(new ComboBoxItem { Content = "PDF", IsSelected = true });
                    LabelFormatComboBox.Items.Add(new ComboBoxItem { Content = "ZPL Termal" });
                    LabelFormatComboBox.Items.Add(new ComboBoxItem { Content = "PNG" });
                    break;
                case "Aras Kargo":
                    LabelFormatComboBox.Items.Add(new ComboBoxItem { Content = "PDF", IsSelected = true });
                    LabelFormatComboBox.Items.Add(new ComboBoxItem { Content = "ZPL Termal" });
                    break;
                case "Surat Kargo":
                    LabelFormatComboBox.Items.Add(new ComboBoxItem { Content = "PDF", IsSelected = true });
                    LabelFormatComboBox.Items.Add(new ComboBoxItem { Content = "PNG" });
                    break;
                default:
                    LabelFormatComboBox.Items.Add(new ComboBoxItem { Content = "PDF", IsSelected = true });
                    LabelFormatComboBox.Items.Add(new ComboBoxItem { Content = "ZPL Termal" });
                    LabelFormatComboBox.Items.Add(new ComboBoxItem { Content = "PNG" });
                    break;
            }

            LabelFormatComboBox.SelectedIndex = 0;

            GlobalLogger.Instance.LogInfo($"Kargo firmasi degisti: {provider}", "CargoShipmentView");
        }

        private async void BulkShip_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedPending = _shipments
                    .Where(s => s.IsSelected && s.Status == "Bekleyen")
                    .ToList();

                if (!selectedPending.Any())
                {
                    ToastManager.ShowWarning("⚠️ Kargoya verilecek bekleyen siparis secilmedi!", "Kargo Yonetimi");
                    return;
                }

                var providerItem = CargoProviderComboBox.SelectedItem as ComboBoxItem;
                var provider = providerItem?.Content?.ToString() ?? "Yurtici Kargo";

                var result = MessageBox.Show(
                    $"{selectedPending.Count} siparis '{provider}' ile kargoya verilecek.\nDevam etmek istiyor musunuz?",
                    "Kargo Onay",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;

                IsLoading = true;
                await Task.Delay(500); // Simulate API call

                var random = new Random();
                var prefixes = new[] { "YK", "AR", "SK" };
                var prefixIndex = provider switch
                {
                    "Yurtici Kargo" => 0,
                    "Aras Kargo" => 1,
                    "Surat Kargo" => 2,
                    _ => 0
                };

                foreach (var item in selectedPending)
                {
                    item.Status = "Kargoda";
                    item.TrackingNumber = $"{prefixes[prefixIndex]}-{random.Next(1000000000, 2000000000)}";
                    item.IsSelected = false;
                }

                UpdateStats();

                GlobalLogger.Instance.LogInfo($"{selectedPending.Count} siparis kargoya verildi ({provider})", "CargoShipmentView");
                ToastManager.ShowSuccess($"📦 {selectedPending.Count} siparis basariyla kargoya verildi!", "Kargo Yonetimi");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Toplu kargo verme hatasi: {ex.Message}", "CargoShipmentView");
                ToastManager.ShowError("❌ Kargo isleminde hata olustu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void PrintLabel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedWithTracking = _shipments
                    .Where(s => s.IsSelected && !string.IsNullOrWhiteSpace(s.TrackingNumber))
                    .ToList();

                if (!selectedWithTracking.Any())
                {
                    ToastManager.ShowWarning("⚠️ Takip numarasi olan gonderi secilmedi! Once kargoya verin.", "Etiket Yazdirma");
                    return;
                }

                var formatItem = LabelFormatComboBox.SelectedItem as ComboBoxItem;
                var format = formatItem?.Content?.ToString() ?? "PDF";

                var providerItem = CargoProviderComboBox.SelectedItem as ComboBoxItem;
                var provider = providerItem?.Content?.ToString() ?? "Yurtici Kargo";

                var trackingNumbers = string.Join("\n", selectedWithTracking.Select(s => $"  - {s.OrderNumber}: {s.TrackingNumber}"));
                MessageBox.Show(
                    $"Etiket Yazdirma\n\nFormat: {format}\nKargo: {provider}\nSecilen: {selectedWithTracking.Count} gonderi\n\nTakip Numaralari:\n{trackingNumbers}\n\n[Yazici entegrasyonu ileride eklenecek]",
                    "Etiket Yazdirma",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                GlobalLogger.Instance.LogInfo($"Etiket yazdirma istendi: {selectedWithTracking.Count} gonderi, format={format}, kargo={provider}", "CargoShipmentView");
                ToastManager.ShowInfo($"🏷️ {selectedWithTracking.Count} etiket {format} formatinda hazirlandi!", "Etiket Yazdirma");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Etiket yazdirma hatasi: {ex.Message}", "CargoShipmentView");
                ToastManager.ShowError("❌ Etiket yazdirma sirasinda hata olustu!", "Hata");
            }
        }

        private async void Refresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsLoading = true;
                await LoadShipmentsAsync();

                GlobalLogger.Instance.LogInfo("Kargo listesi manuel yenilendi", "CargoShipmentView");
                ToastManager.ShowSuccess("🔄 Kargo listesi basariyla yenilendi!", "Kargo Yonetimi");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Kargo yenileme hatasi: {ex.Message}", "CargoShipmentView");
                ToastManager.ShowError("❌ Kargo verileri yenilenirken hata olustu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Dosyasi (*.csv)|*.csv",
                    FileName = $"kargo_gonderim_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };

                if (sfd.ShowDialog() == true)
                {
                    var lines = new System.Collections.Generic.List<string>
                    {
                        "SiparisNo;Platform;Musteri;Urun;Durum;TakipNo;Tarih"
                    };

                    foreach (var s in _shipments)
                    {
                        lines.Add($"{s.OrderNumber};{s.Platform};{s.CustomerName};{s.ProductSummary};{s.Status};{s.TrackingNumber};{s.OrderDate:dd.MM.yyyy}");
                    }

                    System.IO.File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);

                    GlobalLogger.Instance.LogInfo($"Kargo verileri disa aktarildi: {sfd.FileName}", "CargoShipmentView");
                    ToastManager.ShowSuccess($"📤 Disa aktarildi: {sfd.FileName}", "Disa Aktarim");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Disa aktarma hatasi: {ex.Message}", "CargoShipmentView");
                ToastManager.ShowError("❌ Disa aktarma basarisiz!", "Hata");
            }
        }

        private void PlatformFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PlatformFilterComboBox != null) PlatformFilterComboBox.SelectedIndex = 0;
                if (StatusFilterComboBox != null) StatusFilterComboBox.SelectedIndex = 0;
                SearchTextBox?.Clear();
                StartDatePicker?.SelectedDate?.GetHashCode(); // Reset
                if (StartDatePicker != null) StartDatePicker.SelectedDate = null;
                if (EndDatePicker != null) EndDatePicker.SelectedDate = null;
            }
            catch { }

            _ = LoadShipmentsAsync();
            ToastManager.ShowInfo("🗑️ Filtreler temizlendi", "Kargo Yonetimi");
        }

        #endregion

        #region Filtering

        private void ApplyFilters()
        {
            if (ShipmentsDataGrid == null) return;

            try
            {
                var view = System.Windows.Data.CollectionViewSource.GetDefaultView(_shipments);
                if (view == null) return;

                view.Filter = obj =>
                {
                    if (obj is not ShipmentItem item) return false;

                    // Platform filter
                    var platformItem = PlatformFilterComboBox?.SelectedItem as ComboBoxItem;
                    var platformFilter = platformItem?.Content?.ToString() ?? "Tumu";
                    if (platformFilter != "Tumu" && item.Platform != platformFilter)
                        return false;

                    // Status filter
                    var statusItem = StatusFilterComboBox?.SelectedItem as ComboBoxItem;
                    var statusFilter = statusItem?.Content?.ToString() ?? "Tumu";
                    if (statusFilter != "Tumu" && item.Status != statusFilter)
                        return false;

                    // Search text
                    var searchText = SearchTextBox?.Text ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(searchText))
                    {
                        var match = (item.OrderNumber ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    (item.CustomerName ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    (item.ProductSummary ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                    (item.TrackingNumber ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase);
                        if (!match) return false;
                    }

                    // Date range
                    if (StartDatePicker?.SelectedDate != null && item.OrderDate < StartDatePicker.SelectedDate.Value)
                        return false;
                    if (EndDatePicker?.SelectedDate != null && item.OrderDate > EndDatePicker.SelectedDate.Value.AddDays(1))
                        return false;

                    return true;
                };

                view.Refresh();
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Filtre uygulama hatasi: {ex.Message}", "CargoShipmentView");
            }
        }

        #endregion

        #region Lifecycle

        public void Dispose()
        {
            _refreshTimer?.Stop();
            GlobalLogger.Instance.LogInfo("CargoShipmentView disposed", "CargoShipmentView");
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// ShipmentItem - Kargo gonderi veri modeli
        /// </summary>
        public class ShipmentItem : INotifyPropertyChanged
        {
            private bool _isSelected;
            private string _orderNumber = string.Empty;
            private string _platform = string.Empty;
            private string _customerName = string.Empty;
            private string _productSummary = string.Empty;
            private string _status = string.Empty;
            private string _trackingNumber = string.Empty;
            private DateTime _orderDate;

            public bool IsSelected
            {
                get => _isSelected;
                set { _isSelected = value; OnPropertyChanged(); }
            }

            public string OrderNumber
            {
                get => _orderNumber;
                set { _orderNumber = value; OnPropertyChanged(); }
            }

            public string Platform
            {
                get => _platform;
                set { _platform = value; OnPropertyChanged(); }
            }

            public string CustomerName
            {
                get => _customerName;
                set { _customerName = value; OnPropertyChanged(); }
            }

            public string ProductSummary
            {
                get => _productSummary;
                set { _productSummary = value; OnPropertyChanged(); }
            }

            public string Status
            {
                get => _status;
                set { _status = value; OnPropertyChanged(); }
            }

            public string TrackingNumber
            {
                get => _trackingNumber;
                set { _trackingNumber = value; OnPropertyChanged(); }
            }

            public DateTime OrderDate
            {
                get => _orderDate;
                set { _orderDate = value; OnPropertyChanged(); }
            }

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
