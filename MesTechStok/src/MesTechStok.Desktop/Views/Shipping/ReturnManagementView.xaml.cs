using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Shipping
{
    public partial class ReturnManagementView : UserControl
    {
        private readonly ObservableCollection<ReturnRow> _returnRows = new();

        public ReturnManagementView()
        {
            InitializeComponent();
            ReturnsGrid.ItemsSource = _returnRows;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _returnRows.Clear();

            _returnRows.Add(new ReturnRow { ReturnNo = "RET-TY-20260315-001", OrderNo = "TY-2026031401", Platform = "Trendyol", ProductName = "Samsung Galaxy A55 Kilif", Reason = "Yanlis urun gonderildi", Amount = 149.90m, RequestDate = new DateTime(2026, 3, 15), TrackingNo = "YK-8847291034", Status = "Kargoda" });
            _returnRows.Add(new ReturnRow { ReturnNo = "RET-HB-20260314-001", OrderNo = "HB-2026031201", Platform = "Hepsiburada", ProductName = "Bluetooth Kulaklik TWS", Reason = "Urun arizali", Amount = 299.00m, RequestDate = new DateTime(2026, 3, 14), TrackingNo = "AR-5521839047", Status = "Teslim Alindi" });
            _returnRows.Add(new ReturnRow { ReturnNo = "RET-TY-20260314-002", OrderNo = "TY-2026031302", Platform = "Trendyol", ProductName = "Spor Ayakkabi 42 Numara", Reason = "Beden uyumsuz", Amount = 899.00m, RequestDate = new DateTime(2026, 3, 14), TrackingNo = "-", Status = "Talep Alindi" });
            _returnRows.Add(new ReturnRow { ReturnNo = "RET-N11-20260313-001", OrderNo = "N11-2026031101", Platform = "N11", ProductName = "USB-C Hub 7in1", Reason = "Kargo hasari", Amount = 450.00m, RequestDate = new DateTime(2026, 3, 13), TrackingNo = "SR-7734210982", Status = "Iade Onaylandi" });
            _returnRows.Add(new ReturnRow { ReturnNo = "RET-CS-20260312-001", OrderNo = "CS-2026030901", Platform = "Ciceksepeti", ProductName = "Organik Cilt Bakim Seti", Reason = "Vazgectim", Amount = 340.00m, RequestDate = new DateTime(2026, 3, 12), TrackingNo = "-", Status = "Reddedildi" });
            _returnRows.Add(new ReturnRow { ReturnNo = "RET-TY-20260311-003", OrderNo = "TY-2026030801", Platform = "Trendyol", ProductName = "Akilli Saat Band", Reason = "Yanlis renk", Amount = 79.90m, RequestDate = new DateTime(2026, 3, 11), TrackingNo = "YK-9912384756", Status = "Iade Onaylandi" });
            _returnRows.Add(new ReturnRow { ReturnNo = "RET-PZ-20260310-001", OrderNo = "PZ-2026030701", Platform = "Pazarama", ProductName = "Termos 500ml", Reason = "Urun arizali", Amount = 189.00m, RequestDate = new DateTime(2026, 3, 10), TrackingNo = "AR-6639201847", Status = "Kargoda" });
            _returnRows.Add(new ReturnRow { ReturnNo = "RET-HB-20260309-002", OrderNo = "HB-2026030601", Platform = "Hepsiburada", ProductName = "Yoga Mati", Reason = "Beklentimi karsilamadi", Amount = 220.00m, RequestDate = new DateTime(2026, 3, 9), TrackingNo = "SR-4418293756", Status = "Iade Onaylandi" });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            TotalReturnsText.Text = _returnRows.Count.ToString();
            PendingReturnsText.Text = _returnRows.Count(r => r.Status == "Talep Alindi" || r.Status == "Kargoda" || r.Status == "Teslim Alindi").ToString();
            ApprovedReturnsText.Text = _returnRows.Count(r => r.Status == "Iade Onaylandi").ToString();
            RejectedReturnsText.Text = _returnRows.Count(r => r.Status == "Reddedildi").ToString();
            ReturnAmountText.Text = $"{_returnRows.Sum(r => r.Amount):N2} TL";
        }

        private void PlatformFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadMockData();
        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadMockData();
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadMockData();
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Iade raporu disa aktarma islemi yakin zamanda aktif olacak.",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class ReturnRow
    {
        public string ReturnNo { get; set; } = "";
        public string OrderNo { get; set; } = "";
        public string Platform { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string Reason { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime RequestDate { get; set; }
        public string TrackingNo { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
