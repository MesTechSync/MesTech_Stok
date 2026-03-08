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
using System.Collections.Generic;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// UnifiedOrdersView - Birlesik cok platformlu siparis gorunumu
    /// Tum pazaryeri platformlarinin siparisleri tek panelde
    /// </summary>
    public partial class UnifiedOrdersView : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        private readonly ObservableCollection<UnifiedOrderItem> _allOrders;
        private readonly ObservableCollection<UnifiedOrderItem> _filteredOrders;
        private readonly DispatcherTimer _autoRefreshTimer;

        private bool _isLoading;
        private int _trendyolCount;
        private int _ciceksepetiCount;
        private int _hepsiburadaCount;
        private int _opencartCount;

        #endregion

        #region Properties

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

        public int TrendyolCount
        {
            get => _trendyolCount;
            set { _trendyolCount = value; OnPropertyChanged(); }
        }

        public int CiceksepetiCount
        {
            get => _ciceksepetiCount;
            set { _ciceksepetiCount = value; OnPropertyChanged(); }
        }

        public int HepsiburadaCount
        {
            get => _hepsiburadaCount;
            set { _hepsiburadaCount = value; OnPropertyChanged(); }
        }

        public int OpencartCount
        {
            get => _opencartCount;
            set { _opencartCount = value; OnPropertyChanged(); }
        }

        #endregion

        #region Constructor

        public UnifiedOrdersView()
        {
            _allOrders = new ObservableCollection<UnifiedOrderItem>();
            _filteredOrders = new ObservableCollection<UnifiedOrderItem>();

            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(60)
            };
            _autoRefreshTimer.Tick += async (s, e) =>
            {
                await LoadOrdersAsync();
                GlobalLogger.Instance.LogInfo("Birlesik siparisler otomatik yenilendi", "UnifiedOrdersView");
            };

            InitializeComponent();
            DataContext = this;

            OrdersDataGrid.ItemsSource = _filteredOrders;

            _ = InitializeAsync();
        }

        #endregion

        #region Initialization

        private async Task InitializeAsync()
        {
            try
            {
                IsLoading = true;
                await LoadOrdersAsync();
                _autoRefreshTimer.Start();

                GlobalLogger.Instance.LogInfo("UnifiedOrdersView basariyla baslatildi", "UnifiedOrdersView");
                ToastManager.ShowSuccess("Birlesik siparis paneli yuklendi!", "Siparis Paneli");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"UnifiedOrdersView baslatma hatasi: {ex.Message}", "UnifiedOrdersView");
                ToastManager.ShowError("Birlesik siparis paneli yuklenirken hata olustu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Data Loading

        private async Task LoadOrdersAsync()
        {
            try
            {
                IsLoading = true;

                var tasks = new[]
                {
                    LoadPlatformOrdersAsync("Trendyol"),
                    LoadPlatformOrdersAsync("Ciceksepeti"),
                    LoadPlatformOrdersAsync("Hepsiburada"),
                    LoadPlatformOrdersAsync("OpenCart")
                };
                var results = await Task.WhenAll(tasks);

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allOrders.Clear();
                    foreach (var platformOrders in results)
                    {
                        foreach (var order in platformOrders)
                        {
                            _allOrders.Add(order);
                        }
                    }

                    UpdatePlatformCounts();
                    FilterOrders();
                });

                GlobalLogger.Instance.LogInfo($"Birlesik siparisler yuklendi: {_allOrders.Count} toplam siparis", "UnifiedOrdersView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Siparis yukleme hatasi: {ex.Message}", "UnifiedOrdersView");
                ToastManager.ShowError("Siparisler yuklenirken hata olustu!", "Hata");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private Task<List<UnifiedOrderItem>> LoadPlatformOrdersAsync(string platform)
        {
            return Task.Run(() =>
            {
                var orders = new List<UnifiedOrderItem>();

                switch (platform)
                {
                    case "Trendyol":
                        orders.Add(new UnifiedOrderItem { Platform = "Trendyol", OrderNumber = "TY-240301-001", CustomerName = "Ahmet Yilmaz", ProductSummary = "Samsung Galaxy S24 Ultra", Amount = 64999.00m, Status = "Kargoda", OrderDate = new DateTime(2026, 3, 1) });
                        orders.Add(new UnifiedOrderItem { Platform = "Trendyol", OrderNumber = "TY-240302-015", CustomerName = "Fatma Demir", ProductSummary = "Apple MacBook Air M3", Amount = 42999.00m, Status = "Bekleyen", OrderDate = new DateTime(2026, 3, 2) });
                        orders.Add(new UnifiedOrderItem { Platform = "Trendyol", OrderNumber = "TY-240303-042", CustomerName = "Mehmet Kaya", ProductSummary = "Sony WH-1000XM5 Kulaklik", Amount = 8499.00m, Status = "Teslim", OrderDate = new DateTime(2026, 3, 3) });
                        orders.Add(new UnifiedOrderItem { Platform = "Trendyol", OrderNumber = "TY-240305-078", CustomerName = "Zeynep Aksoy", ProductSummary = "Dyson V15 Detect Supurge", Amount = 24990.00m, Status = "Isleniyor", OrderDate = new DateTime(2026, 3, 5) });
                        break;

                    case "Ciceksepeti":
                        orders.Add(new UnifiedOrderItem { Platform = "Ciceksepeti", OrderNumber = "CS-240301-003", CustomerName = "Elif Sahin", ProductSummary = "Kirmizi Gul Buketi (51 Adet)", Amount = 1299.00m, Status = "Teslim", OrderDate = new DateTime(2026, 3, 1) });
                        orders.Add(new UnifiedOrderItem { Platform = "Ciceksepeti", OrderNumber = "CS-240304-019", CustomerName = "Burak Ozturk", ProductSummary = "Cikolata Hediye Kutusu Premium", Amount = 749.00m, Status = "Kargoda", OrderDate = new DateTime(2026, 3, 4) });
                        orders.Add(new UnifiedOrderItem { Platform = "Ciceksepeti", OrderNumber = "CS-240306-027", CustomerName = "Selin Arslan", ProductSummary = "Orkide Aranjmani + Balon", Amount = 1899.00m, Status = "Bekleyen", OrderDate = new DateTime(2026, 3, 6) });
                        break;

                    case "Hepsiburada":
                        orders.Add(new UnifiedOrderItem { Platform = "Hepsiburada", OrderNumber = "HB-240302-007", CustomerName = "Can Erdogan", ProductSummary = "iPhone 15 Pro Max 256GB", Amount = 74999.00m, Status = "Kargoda", OrderDate = new DateTime(2026, 3, 2) });
                        orders.Add(new UnifiedOrderItem { Platform = "Hepsiburada", OrderNumber = "HB-240303-021", CustomerName = "Ayse Celik", ProductSummary = "LG OLED 65 inch 4K TV", Amount = 52990.00m, Status = "Isleniyor", OrderDate = new DateTime(2026, 3, 3) });
                        orders.Add(new UnifiedOrderItem { Platform = "Hepsiburada", OrderNumber = "HB-240305-034", CustomerName = "Emre Yildiz", ProductSummary = "Bosch Bulasikyikama Makinesi", Amount = 18750.00m, Status = "Iptal", OrderDate = new DateTime(2026, 3, 5) });
                        orders.Add(new UnifiedOrderItem { Platform = "Hepsiburada", OrderNumber = "HB-240307-055", CustomerName = "Deniz Korkmaz", ProductSummary = "Nike Air Max 270 Ayakkabi", Amount = 4299.00m, Status = "Bekleyen", OrderDate = new DateTime(2026, 3, 7) });
                        break;

                    case "OpenCart":
                        orders.Add(new UnifiedOrderItem { Platform = "OpenCart", OrderNumber = "OC-240301-002", CustomerName = "Hakan Polat", ProductSummary = "Mekanik Klavye RGB Cherry MX", Amount = 3299.00m, Status = "Teslim", OrderDate = new DateTime(2026, 3, 1) });
                        orders.Add(new UnifiedOrderItem { Platform = "OpenCart", OrderNumber = "OC-240304-011", CustomerName = "Gizem Aydin", ProductSummary = "Logitech MX Master 3S Mouse", Amount = 2499.00m, Status = "Kargoda", OrderDate = new DateTime(2026, 3, 4) });
                        orders.Add(new UnifiedOrderItem { Platform = "OpenCart", OrderNumber = "OC-240306-018", CustomerName = "Omer Tas", ProductSummary = "27 inch IPS Monitor 165Hz", Amount = 8990.00m, Status = "Isleniyor", OrderDate = new DateTime(2026, 3, 6) });
                        break;
                }

                return orders;
            });
        }

        #endregion

        #region Filtering

        private void FilterOrders()
        {
            _filteredOrders.Clear();

            var query = _allOrders.AsEnumerable();

            // Platform checkbox filters
            var enabledPlatforms = new List<string>();
            if (ChkTrendyol?.IsChecked == true) enabledPlatforms.Add("Trendyol");
            if (ChkCiceksepeti?.IsChecked == true) enabledPlatforms.Add("Ciceksepeti");
            if (ChkHepsiburada?.IsChecked == true) enabledPlatforms.Add("Hepsiburada");
            if (ChkOpencart?.IsChecked == true) enabledPlatforms.Add("OpenCart");

            query = query.Where(o => enabledPlatforms.Contains(o.Platform));

            // Status filter
            var statusItem = StatusFilterComboBox?.SelectedItem as ComboBoxItem;
            var statusText = statusItem?.Content?.ToString() ?? "Tumu";
            if (statusText != "Tumu")
            {
                query = query.Where(o => o.Status == statusText);
            }

            // Date range filter
            if (StartDatePicker?.SelectedDate.HasValue == true)
            {
                var startDate = StartDatePicker.SelectedDate.Value;
                query = query.Where(o => o.OrderDate >= startDate);
            }
            if (EndDatePicker?.SelectedDate.HasValue == true)
            {
                var endDate = EndDatePicker.SelectedDate.Value.AddDays(1);
                query = query.Where(o => o.OrderDate < endDate);
            }

            // Search text filter
            var searchText = SearchTextBox?.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(searchText))
            {
                query = query.Where(o =>
                    (o.OrderNumber ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (o.CustomerName ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (o.ProductSummary ?? "").Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var order in query.OrderByDescending(o => o.OrderDate))
            {
                _filteredOrders.Add(order);
            }
        }

        private void UpdatePlatformCounts()
        {
            TrendyolCount = _allOrders.Count(o => o.Platform == "Trendyol");
            CiceksepetiCount = _allOrders.Count(o => o.Platform == "Ciceksepeti");
            HepsiburadaCount = _allOrders.Count(o => o.Platform == "Hepsiburada");
            OpencartCount = _allOrders.Count(o => o.Platform == "OpenCart");

            if (TrendyolCountText != null) TrendyolCountText.Text = $"{TrendyolCount} siparis";
            if (CiceksepetiCountText != null) CiceksepetiCountText.Text = $"{CiceksepetiCount} siparis";
            if (HepsiburadaCountText != null) HepsiburadaCountText.Text = $"{HepsiburadaCount} siparis";
            if (OpencartCountText != null) OpencartCountText.Text = $"{OpencartCount} siparis";
        }

        #endregion

        #region Event Handlers

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadOrdersAsync();
            GlobalLogger.Instance.LogInfo("Birlesik siparisler manuel yenilendi", "UnifiedOrdersView");
            ToastManager.ShowSuccess("Siparis listesi yenilendi!", "Siparis Paneli");
        }

        private void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Dosyasi (*.csv)|*.csv",
                    FileName = $"birlesik_siparisler_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() == true)
                {
                    var lines = new List<string>();
                    lines.Add("Platform;SiparisNo;Musteri;Urun;Tutar;Durum;Tarih");
                    foreach (var o in _filteredOrders)
                    {
                        lines.Add($"{o.Platform};{o.OrderNumber};{o.CustomerName};{o.ProductSummary};{o.Amount:N2};{o.Status};{o.OrderDate:yyyy-MM-dd}");
                    }
                    System.IO.File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);
                    ToastManager.ShowSuccess($"Excel disa aktarildi: {sfd.FileName}", "Disa Aktarim");
                    GlobalLogger.Instance.LogInfo($"Excel export tamamlandi: {_filteredOrders.Count} siparis", "UnifiedOrdersView");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Excel export hatasi: {ex.Message}", "UnifiedOrdersView");
                ToastManager.ShowError("Excel disa aktarma basarisiz!", "Hata");
            }
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Dosyasi (*.csv)|*.csv",
                    FileName = $"birlesik_siparisler_csv_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() == true)
                {
                    var lines = new List<string>();
                    lines.Add("Platform,SiparisNo,Musteri,Urun,Tutar,Durum,Tarih");
                    foreach (var o in _filteredOrders)
                    {
                        lines.Add($"\"{o.Platform}\",\"{o.OrderNumber}\",\"{o.CustomerName}\",\"{o.ProductSummary}\",{o.Amount:N2},\"{o.Status}\",\"{o.OrderDate:yyyy-MM-dd}\"");
                    }
                    System.IO.File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);
                    ToastManager.ShowSuccess($"CSV disa aktarildi: {sfd.FileName}", "Disa Aktarim");
                    GlobalLogger.Instance.LogInfo($"CSV export tamamlandi: {_filteredOrders.Count} siparis", "UnifiedOrdersView");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"CSV export hatasi: {ex.Message}", "UnifiedOrdersView");
                ToastManager.ShowError("CSV disa aktarma basarisiz!", "Hata");
            }
        }

        private void FilterChanged(object sender, RoutedEventArgs e)
        {
            FilterOrders();
        }

        private void FilterChanged_ComboBox(object sender, SelectionChangedEventArgs e)
        {
            FilterOrders();
        }

        private void FilterChanged_Date(object? sender, SelectionChangedEventArgs e)
        {
            FilterOrders();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterOrders();
        }

        private void ViewOrderDetail_Click(object sender, RoutedEventArgs e)
        {
            if (((Button)sender).DataContext is UnifiedOrderItem order)
            {
                var details = $"Platform: {order.Platform}\n" +
                             $"Siparis No: {order.OrderNumber}\n" +
                             $"Musteri: {order.CustomerName}\n" +
                             $"Urun: {order.ProductSummary}\n" +
                             $"Tutar: {order.FormattedAmount}\n" +
                             $"Durum: {order.Status}\n" +
                             $"Tarih: {order.FormattedDate}";

                MessageBox.Show(details, "Siparis Detaylari", MessageBoxButton.OK, MessageBoxImage.Information);
                GlobalLogger.Instance.LogInfo($"Siparis detayi goruntulendi: {order.OrderNumber}", "UnifiedOrdersView");
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

        #region Nested Class - UnifiedOrderItem

        public class UnifiedOrderItem : INotifyPropertyChanged
        {
            private string _platform = string.Empty;
            private string _orderNumber = string.Empty;
            private string _customerName = string.Empty;
            private string _productSummary = string.Empty;
            private decimal _amount;
            private string _status = string.Empty;
            private DateTime _orderDate;

            public string Platform
            {
                get => _platform;
                set { _platform = value; OnPropertyChanged(); }
            }

            public string OrderNumber
            {
                get => _orderNumber;
                set { _orderNumber = value; OnPropertyChanged(); }
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

            public decimal Amount
            {
                get => _amount;
                set { _amount = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedAmount)); }
            }

            public string Status
            {
                get => _status;
                set { _status = value; OnPropertyChanged(); }
            }

            public DateTime OrderDate
            {
                get => _orderDate;
                set { _orderDate = value; OnPropertyChanged(); OnPropertyChanged(nameof(FormattedDate)); }
            }

            public string FormattedAmount => $"{Amount:N2} TL";

            public string FormattedDate => OrderDate.ToString("dd.MM.yyyy");

            public event PropertyChangedEventHandler? PropertyChanged;

            protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
