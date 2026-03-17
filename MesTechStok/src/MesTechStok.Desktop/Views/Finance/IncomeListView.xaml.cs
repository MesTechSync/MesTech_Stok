using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Finance
{
    public partial class IncomeListView : UserControl
    {
        private readonly ObservableCollection<IncomeEntry> _entries = new();

        public IncomeListView()
        {
            InitializeComponent();
            DateFrom.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTo.SelectedDate = DateTime.Today;
            IncomeGrid.ItemsSource = _entries;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _entries.Clear();

            _entries.Add(new IncomeEntry { Date = DateTime.Today.AddDays(-1), Platform = "Trendyol", Description = "Haftalik hakedis #2026-M3-W2", Amount = 24850.75m, Commission = 3727.61m });
            _entries.Add(new IncomeEntry { Date = DateTime.Today.AddDays(-3), Platform = "Hepsiburada", Description = "Mart 2. hafta hakedis", Amount = 12480.00m, Commission = 2121.60m });
            _entries.Add(new IncomeEntry { Date = DateTime.Today.AddDays(-4), Platform = "N11", Description = "Siparis tahsilatlari", Amount = 8640.50m, Commission = 1036.86m });
            _entries.Add(new IncomeEntry { Date = DateTime.Today.AddDays(-5), Platform = "Ciceksepeti", Description = "Hakedis transferi", Amount = 6320.00m, Commission = 1264.00m });
            _entries.Add(new IncomeEntry { Date = DateTime.Today.AddDays(-7), Platform = "Pazarama", Description = "Haftalik odeme", Amount = 3450.25m, Commission = 345.03m });
            _entries.Add(new IncomeEntry { Date = DateTime.Today.AddDays(-8), Platform = "OpenCart", Description = "Web satis - kredi karti", Amount = 5280.00m, Commission = 0.00m });
            _entries.Add(new IncomeEntry { Date = DateTime.Today.AddDays(-10), Platform = "Trendyol", Description = "Haftalik hakedis #2026-M3-W1", Amount = 21340.50m, Commission = 3201.08m });
            _entries.Add(new IncomeEntry { Date = DateTime.Today.AddDays(-12), Platform = "Hepsiburada", Description = "Mart 1. hafta hakedis", Amount = 9870.25m, Commission = 1677.94m });
            _entries.Add(new IncomeEntry { Date = DateTime.Today.AddDays(-14), Platform = "N11", Description = "Haftalik siparis tahsilati", Amount = 7200.00m, Commission = 864.00m });
            _entries.Add(new IncomeEntry { Date = DateTime.Today.AddDays(-15), Platform = "OpenCart", Description = "Web satis - havale", Amount = 4100.00m, Commission = 0.00m });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var totalIncome = _entries.Sum(e => e.Amount);
            var totalCommission = _entries.Sum(e => e.Commission);
            var netIncome = totalIncome - totalCommission;

            TotalIncomeText.Text = $"{totalIncome:N2} TL";
            TotalCommissionText.Text = $"{totalCommission:N2} TL";
            NetIncomeText.Text = $"{netIncome:N2} TL";
            RecordCountText.Text = _entries.Count.ToString();
        }

        private void PlatformFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            LoadMockData();
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadMockData();
        }

        private void AddIncome_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Gelir ekleme formu — backend hazir olunca aktif edilecek.", "MesTech Gelir", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class IncomeEntry
    {
        public DateTime Date { get; set; }
        public string Platform { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal Commission { get; set; }
        public decimal NetAmount => Amount - Commission;
    }
}
