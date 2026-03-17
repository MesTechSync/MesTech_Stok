using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Finance
{
    public partial class SettlementView : UserControl
    {
        private readonly ObservableCollection<SettlementRow> _settlementRows = new();

        public SettlementView()
        {
            InitializeComponent();
            SettlementsGrid.ItemsSource = _settlementRows;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _settlementRows.Clear();

            _settlementRows.Add(new SettlementRow { SettlementNo = "STL-TY-20260314-001", Platform = "Trendyol", Period = "14.03.2026", GrossAmount = 45200.50m, Commission = 6780.08m, CargoDeduction = 2260.00m, NetAmount = 36160.42m, PaymentDate = new DateTime(2026, 3, 16), Status = "Odendi" });
            _settlementRows.Add(new SettlementRow { SettlementNo = "STL-HB-20260312-001", Platform = "Hepsiburada", Period = "12.03.2026", GrossAmount = 18750.00m, Commission = 3187.50m, CargoDeduction = 937.50m, NetAmount = 14625.00m, PaymentDate = new DateTime(2026, 3, 15), Status = "Odendi" });
            _settlementRows.Add(new SettlementRow { SettlementNo = "STL-N11-20260311-001", Platform = "N11", Period = "11.03.2026", GrossAmount = 12400.00m, Commission = 1488.00m, CargoDeduction = 620.00m, NetAmount = 10292.00m, PaymentDate = new DateTime(2026, 3, 14), Status = "Odendi" });
            _settlementRows.Add(new SettlementRow { SettlementNo = "STL-CS-20260310-001", Platform = "Ciceksepeti", Period = "10.03.2026", GrossAmount = 9840.75m, Commission = 1968.15m, CargoDeduction = 492.04m, NetAmount = 7380.56m, PaymentDate = new DateTime(2026, 3, 17), Status = "Beklemede" });
            _settlementRows.Add(new SettlementRow { SettlementNo = "STL-PZ-20260309-001", Platform = "Pazarama", Period = "09.03.2026", GrossAmount = 5600.00m, Commission = 560.00m, CargoDeduction = 280.00m, NetAmount = 4760.00m, PaymentDate = new DateTime(2026, 3, 18), Status = "Beklemede" });
            _settlementRows.Add(new SettlementRow { SettlementNo = "STL-TY-20260307-002", Platform = "Trendyol", Period = "07.03.2026", GrossAmount = 38400.00m, Commission = 5760.00m, CargoDeduction = 1920.00m, NetAmount = 30720.00m, PaymentDate = new DateTime(2026, 3, 10), Status = "Odendi" });
            _settlementRows.Add(new SettlementRow { SettlementNo = "STL-HB-20260305-001", Platform = "Hepsiburada", Period = "05.03.2026", GrossAmount = 16200.00m, Commission = 2754.00m, CargoDeduction = 810.00m, NetAmount = 12636.00m, PaymentDate = new DateTime(2026, 3, 8), Status = "Odendi" });
            _settlementRows.Add(new SettlementRow { SettlementNo = "STL-N11-20260301-001", Platform = "N11", Period = "01.03.2026", GrossAmount = 8900.00m, Commission = 1068.00m, CargoDeduction = 445.00m, NetAmount = 7387.00m, PaymentDate = new DateTime(2026, 3, 5), Status = "Gecikti" });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var total = _settlementRows.Sum(r => r.NetAmount);
            var paid = _settlementRows.Where(r => r.Status == "Odendi").Sum(r => r.NetAmount);
            var pending = _settlementRows.Where(r => r.Status == "Beklemede").Sum(r => r.NetAmount);
            var overdue = _settlementRows.Where(r => r.Status == "Gecikti").Sum(r => r.NetAmount);
            var commission = _settlementRows.Sum(r => r.Commission);

            TotalSettlementText.Text = $"{total:N2} TL";
            PaidText.Text = $"{paid:N2} TL";
            PendingText.Text = $"{pending:N2} TL";
            OverdueText.Text = $"{overdue:N2} TL";
            CommissionDeductText.Text = $"{commission:N2} TL";
        }

        private void PlatformFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadMockData();
        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadMockData();
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadMockData();
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hakedis raporu disa aktarma islemi yakin zamanda aktif olacak.",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class SettlementRow
    {
        public string SettlementNo { get; set; } = "";
        public string Platform { get; set; } = "";
        public string Period { get; set; } = "";
        public decimal GrossAmount { get; set; }
        public decimal Commission { get; set; }
        public decimal CargoDeduction { get; set; }
        public decimal NetAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } = "";
    }
}
