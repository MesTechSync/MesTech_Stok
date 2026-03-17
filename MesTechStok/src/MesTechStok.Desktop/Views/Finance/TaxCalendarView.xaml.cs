using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesTechStok.Desktop.Views.Finance
{
    public partial class TaxCalendarView : UserControl
    {
        private static readonly SolidColorBrush _paidBrush;
        private static readonly SolidColorBrush _pendingBrush;
        private static readonly SolidColorBrush _overdueBrush;
        private static readonly SolidColorBrush _upcomingBrush;

        static TaxCalendarView()
        {
            _paidBrush = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
            _paidBrush.Freeze();
            _pendingBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));
            _pendingBrush.Freeze();
            _overdueBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45));
            _overdueBrush.Freeze();
            _upcomingBrush = new SolidColorBrush(Color.FromRgb(0x28, 0x55, 0xAC));
            _upcomingBrush.Freeze();
        }

        private readonly ObservableCollection<TaxCalendarEntry> _entries = new();

        public TaxCalendarView()
        {
            InitializeComponent();
            TaxGrid.ItemsSource = _entries;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _entries.Clear();
            var today = DateTime.Today;

            // 2026 vergi takvimi (gercekci TR vergi tarihleri)
            _entries.Add(new TaxCalendarEntry { DueDate = new DateTime(2026, 1, 26), TaxType = "KDV Beyannamesi", Period = "Aralik 2025", Amount = 8450.00m, Status = "Odendi" });
            _entries.Add(new TaxCalendarEntry { DueDate = new DateTime(2026, 1, 26), TaxType = "Muhtasar Beyanname", Period = "Aralik 2025", Amount = 3200.00m, Status = "Odendi" });
            _entries.Add(new TaxCalendarEntry { DueDate = new DateTime(2026, 2, 17), TaxType = "Gecici Vergi (4. Donem)", Period = "Ekim-Aralik 2025", Amount = 12750.00m, Status = "Odendi" });
            _entries.Add(new TaxCalendarEntry { DueDate = new DateTime(2026, 2, 26), TaxType = "KDV Beyannamesi", Period = "Ocak 2026", Amount = 9120.50m, Status = "Odendi" });
            _entries.Add(new TaxCalendarEntry { DueDate = new DateTime(2026, 2, 26), TaxType = "Muhtasar Beyanname", Period = "Ocak 2026", Amount = 3450.00m, Status = "Odendi" });
            _entries.Add(new TaxCalendarEntry { DueDate = new DateTime(2026, 3, 26), TaxType = "KDV Beyannamesi", Period = "Subat 2026", Amount = 10240.75m, Status = "Bekleyen" });
            _entries.Add(new TaxCalendarEntry { DueDate = new DateTime(2026, 3, 26), TaxType = "Muhtasar Beyanname", Period = "Subat 2026", Amount = 3680.00m, Status = "Bekleyen" });
            _entries.Add(new TaxCalendarEntry { DueDate = new DateTime(2026, 4, 26), TaxType = "KDV Beyannamesi", Period = "Mart 2026", Amount = 11500.00m, Status = "Yaklasan" });
            _entries.Add(new TaxCalendarEntry { DueDate = new DateTime(2026, 4, 26), TaxType = "Muhtasar Beyanname", Period = "Mart 2026", Amount = 3900.00m, Status = "Yaklasan" });
            _entries.Add(new TaxCalendarEntry { DueDate = new DateTime(2026, 5, 17), TaxType = "Gecici Vergi (1. Donem)", Period = "Ocak-Mart 2026", Amount = 14200.00m, Status = "Yaklasan" });
            _entries.Add(new TaxCalendarEntry { DueDate = new DateTime(2026, 4, 30), TaxType = "Yillik Gelir Vergisi", Period = "2025 Yili", Amount = 48000.00m, Status = "Yaklasan" });

            // Renk kodlama ve kalan gun hesabi
            foreach (var entry in _entries)
            {
                var daysRemaining = (entry.DueDate - today).Days;

                if (entry.Status == "Odendi")
                {
                    entry.StatusColor = _paidBrush;
                    entry.StatusLabel = "Odendi";
                    entry.DaysRemaining = "-";
                }
                else if (daysRemaining < 0)
                {
                    entry.StatusColor = _overdueBrush;
                    entry.StatusLabel = "Gecikti";
                    entry.DaysRemaining = $"{Math.Abs(daysRemaining)} gun gecikti";
                    entry.Status = "Gecikti";
                }
                else if (daysRemaining <= 7)
                {
                    entry.StatusColor = _pendingBrush;
                    entry.StatusLabel = "Acil";
                    entry.DaysRemaining = $"{daysRemaining} gun";
                }
                else
                {
                    entry.StatusColor = _upcomingBrush;
                    entry.StatusLabel = "Yaklasan";
                    entry.DaysRemaining = $"{daysRemaining} gun";
                }
            }

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var totalTax = _entries.Sum(e => e.Amount);
            var paidTax = _entries.Where(e => e.Status == "Odendi").Sum(e => e.Amount);
            var pendingTax = _entries.Where(e => e.Status != "Odendi").Sum(e => e.Amount);
            var overdueCount = _entries.Count(e => e.Status == "Gecikti");

            TotalTaxText.Text = $"{totalTax:N2} TL";
            PaidTaxText.Text = $"{paidTax:N2} TL";
            PendingTaxText.Text = $"{pendingTax:N2} TL";
            OverdueTaxText.Text = overdueCount.ToString();
        }

        private void YearFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            LoadMockData();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadMockData();
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class TaxCalendarEntry
    {
        public DateTime DueDate { get; set; }
        public string TaxType { get; set; } = "";
        public string Period { get; set; } = "";
        public decimal Amount { get; set; }
        public string Status { get; set; } = "";
        public string StatusLabel { get; set; } = "";
        public SolidColorBrush StatusColor { get; set; } = Brushes.Gray;
        public string DaysRemaining { get; set; } = "";
        public string Note { get; set; } = "";
    }
}
