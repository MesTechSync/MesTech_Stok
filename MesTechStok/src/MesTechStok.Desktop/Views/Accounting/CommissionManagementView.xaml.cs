using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Accounting
{
    public partial class CommissionManagementView : UserControl
    {
        private readonly ObservableCollection<CommissionRow> _commissionRows = new();

        public CommissionManagementView()
        {
            InitializeComponent();
            CommissionGrid.ItemsSource = _commissionRows;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _commissionRows.Clear();

            _commissionRows.Add(new CommissionRow { Platform = "Trendyol", Category = "Elektronik", Rate = 12.50, ServiceFee = 3.99m, TotalCommission = 6780.08m, TransactionCount = 142, LastUpdated = DateTime.Today.AddDays(-1), Status = "Aktif" });
            _commissionRows.Add(new CommissionRow { Platform = "Trendyol", Category = "Giyim", Rate = 18.00, ServiceFee = 3.99m, TotalCommission = 4520.00m, TransactionCount = 98, LastUpdated = DateTime.Today.AddDays(-1), Status = "Aktif" });
            _commissionRows.Add(new CommissionRow { Platform = "Hepsiburada", Category = "Elektronik", Rate = 10.80, ServiceFee = 4.50m, TotalCommission = 3187.50m, TransactionCount = 67, LastUpdated = DateTime.Today.AddDays(-2), Status = "Aktif" });
            _commissionRows.Add(new CommissionRow { Platform = "Hepsiburada", Category = "Ev & Yasam", Rate = 15.00, ServiceFee = 4.50m, TotalCommission = 2100.00m, TransactionCount = 45, LastUpdated = DateTime.Today.AddDays(-2), Status = "Aktif" });
            _commissionRows.Add(new CommissionRow { Platform = "N11", Category = "Elektronik", Rate = 9.50, ServiceFee = 2.99m, TotalCommission = 1488.00m, TransactionCount = 38, LastUpdated = DateTime.Today.AddDays(-3), Status = "Aktif" });
            _commissionRows.Add(new CommissionRow { Platform = "Ciceksepeti", Category = "Kozmetik", Rate = 20.00, ServiceFee = 5.00m, TotalCommission = 1968.15m, TransactionCount = 52, LastUpdated = DateTime.Today.AddDays(-1), Status = "Aktif" });
            _commissionRows.Add(new CommissionRow { Platform = "Pazarama", Category = "Genel", Rate = 10.00, ServiceFee = 2.50m, TotalCommission = 560.00m, TransactionCount = 22, LastUpdated = DateTime.Today.AddDays(-4), Status = "Aktif" });
            _commissionRows.Add(new CommissionRow { Platform = "OpenCart", Category = "Genel", Rate = 0.00, ServiceFee = 0.00m, TotalCommission = 0.00m, TransactionCount = 31, LastUpdated = DateTime.Today.AddDays(-1), Status = "Kendi Magaza" });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var total = _commissionRows.Sum(r => r.TotalCommission);
            var avgRate = _commissionRows.Where(r => r.Rate > 0).DefaultIfEmpty().Average(r => r?.Rate ?? 0);
            var activePlatforms = _commissionRows.Select(r => r.Platform).Distinct().Count();
            var categories = _commissionRows.Select(r => r.Category).Distinct().Count();

            TotalCommissionText.Text = $"{total:N2} TL";
            AvgRateText.Text = $"{avgRate:F1}%";
            ActivePlatformText.Text = activePlatforms.ToString();
            CategoryCountText.Text = categories.ToString();
        }

        private void PlatformFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadMockData();
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadMockData();
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Komisyon raporu disa aktarma islemi yakin zamanda aktif olacak.",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class CommissionRow
    {
        public string Platform { get; set; } = "";
        public string Category { get; set; } = "";
        public double Rate { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal TotalCommission { get; set; }
        public int TransactionCount { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Status { get; set; } = "";
    }
}
