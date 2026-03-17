using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Shipping
{
    public partial class ShipmentQueueView : UserControl
    {
        private readonly ObservableCollection<ShipmentQueueRow> _queueRows = new();

        public ShipmentQueueView()
        {
            InitializeComponent();
            ShipmentQueueGrid.ItemsSource = _queueRows;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _queueRows.Clear();

            _queueRows.Add(new ShipmentQueueRow { OrderNo = "TY-2026031601", Platform = "Trendyol", CustomerName = "Ahmet Yilmaz", ItemCount = 3, Carrier = "Yurtici Kargo", Priority = "Acil", OrderDate = DateTime.Today.AddHours(-2), Deadline = DateTime.Today, LabelStatus = "Basildi", Status = "Hazir" });
            _queueRows.Add(new ShipmentQueueRow { OrderNo = "HB-2026031601", Platform = "Hepsiburada", CustomerName = "Fatma Demir", ItemCount = 1, Carrier = "Aras Kargo", Priority = "Acil", OrderDate = DateTime.Today.AddHours(-4), Deadline = DateTime.Today, LabelStatus = "Basildi", Status = "Hazir" });
            _queueRows.Add(new ShipmentQueueRow { OrderNo = "TY-2026031602", Platform = "Trendyol", CustomerName = "Mehmet Kaya", ItemCount = 2, Carrier = "Yurtici Kargo", Priority = "Normal", OrderDate = DateTime.Today.AddHours(-6), Deadline = DateTime.Today.AddDays(1), LabelStatus = "Basilmadi", Status = "Paketleniyor" });
            _queueRows.Add(new ShipmentQueueRow { OrderNo = "N11-2026031601", Platform = "N11", CustomerName = "Ayse Celik", ItemCount = 1, Carrier = "Surat Kargo", Priority = "Normal", OrderDate = DateTime.Today.AddHours(-8), Deadline = DateTime.Today.AddDays(1), LabelStatus = "Basilmadi", Status = "Beklemede" });
            _queueRows.Add(new ShipmentQueueRow { OrderNo = "CS-2026031601", Platform = "Ciceksepeti", CustomerName = "Ali Ozturk", ItemCount = 4, Carrier = "MNG Kargo", Priority = "Normal", OrderDate = DateTime.Today.AddDays(-1), Deadline = DateTime.Today.AddDays(1), LabelStatus = "Basildi", Status = "Hazir" });
            _queueRows.Add(new ShipmentQueueRow { OrderNo = "TY-2026031503", Platform = "Trendyol", CustomerName = "Zeynep Sahin", ItemCount = 1, Carrier = "Yurtici Kargo", Priority = "Dusuk", OrderDate = DateTime.Today.AddDays(-1).AddHours(-3), Deadline = DateTime.Today.AddDays(2), LabelStatus = "Basilmadi", Status = "Beklemede" });
            _queueRows.Add(new ShipmentQueueRow { OrderNo = "PZ-2026031601", Platform = "Pazarama", CustomerName = "Huseyin Arslan", ItemCount = 2, Carrier = "Aras Kargo", Priority = "Normal", OrderDate = DateTime.Today.AddDays(-1).AddHours(-5), Deadline = DateTime.Today.AddDays(1), LabelStatus = "Basildi", Status = "Paketleniyor" });
            _queueRows.Add(new ShipmentQueueRow { OrderNo = "HB-2026031502", Platform = "Hepsiburada", CustomerName = "Elif Korkmaz", ItemCount = 1, Carrier = "Surat Kargo", Priority = "Normal", OrderDate = DateTime.Today.AddDays(-1).AddHours(-7), Deadline = DateTime.Today, LabelStatus = "Basildi", Status = "Hazir" });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            QueueCountText.Text = _queueRows.Count.ToString();
            UrgentCountText.Text = _queueRows.Count(r => r.Priority == "Acil").ToString();
            ShippedTodayText.Text = "12"; // Mock: today's shipped count
            NoPrintText.Text = _queueRows.Count(r => r.LabelStatus == "Basilmadi").ToString();
            AvgWaitText.Text = "4.2 saat";
        }

        private void CarrierFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadMockData();
        private void PriorityFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadMockData();
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadMockData();

        private void BulkShip_Click(object sender, RoutedEventArgs e)
        {
            var selected = _queueRows.Where(r => r.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Toplu gonderim icin en az bir siparis seciniz.\n(CheckBox ile isaretle)",
                    "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"{selected.Count} siparis toplu gonderim islemine alinacak.\n(Kargo API entegrasyonu tamamlandiginda etkinlestirilecek.)",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PrintLabels_Click(object sender, RoutedEventArgs e)
        {
            var selected = _queueRows.Where(r => r.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Etiket basilacak siparisleri seciniz.\n(CheckBox ile isaretle)",
                    "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"{selected.Count} siparis icin kargo etiketi yazdirilacak.\n(Etiket yazdirma modulu yakin zamanda aktif olacak.)",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class ShipmentQueueRow : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string OrderNo { get; set; } = "";
        public string Platform { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public int ItemCount { get; set; }
        public string Carrier { get; set; } = "";
        public string Priority { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public DateTime Deadline { get; set; }
        public string LabelStatus { get; set; } = "";
        public string Status { get; set; } = "";

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
