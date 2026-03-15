using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesTechStok.Desktop.Views.Accounting
{
    public partial class GelirGiderView : UserControl
    {
        private static readonly SolidColorBrush _profitBrush;
        private static readonly SolidColorBrush _lossBrush;

        static GelirGiderView()
        {
            _profitBrush = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
            _profitBrush.Freeze();
            _lossBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45));
            _lossBrush.Freeze();
        }

        private readonly ObservableCollection<GelirGiderEntry> _entries = new();

        public GelirGiderView()
        {
            InitializeComponent();
            DateFrom.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTo.SelectedDate = DateTime.Today;
            EntryDate.SelectedDate = DateTime.Today;
            EntriesGrid.ItemsSource = _entries;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _entries.Clear();
            _entries.Add(new GelirGiderEntry { Date = DateTime.Today.AddDays(-12), EntryType = "Gelir", Title = "Trendyol Mart 1. Hafta Satislari", Category = "Marketplace Satis", Source = "Trendyol", Amount = 15420.50m });
            _entries.Add(new GelirGiderEntry { Date = DateTime.Today.AddDays(-10), EntryType = "Gider", Title = "Depo Kira Odemesi", Category = "Kira", Source = "Manuel", Amount = 8500.00m });
            _entries.Add(new GelirGiderEntry { Date = DateTime.Today.AddDays(-8), EntryType = "Gelir", Title = "N11 Siparis Tahsilati", Category = "Marketplace Satis", Source = "N11", Amount = 4280.00m });
            _entries.Add(new GelirGiderEntry { Date = DateTime.Today.AddDays(-7), EntryType = "Gider", Title = "Kargo Giderleri (Yurtici)", Category = "Lojistik", Source = "Manuel", Amount = 1250.30m });
            _entries.Add(new GelirGiderEntry { Date = DateTime.Today.AddDays(-6), EntryType = "Gelir", Title = "Hepsiburada Haftalik Hakedis", Category = "Marketplace Satis", Source = "Hepsiburada", Amount = 9870.25m });
            _entries.Add(new GelirGiderEntry { Date = DateTime.Today.AddDays(-5), EntryType = "Gider", Title = "Marketplace Komisyonlari", Category = "Komisyon", Source = "Trendyol", Amount = 3200.00m });
            _entries.Add(new GelirGiderEntry { Date = DateTime.Today.AddDays(-4), EntryType = "Gelir", Title = "OpenCart Web Satis", Category = "Direkt Satis", Source = "OpenCart", Amount = 2100.75m });
            _entries.Add(new GelirGiderEntry { Date = DateTime.Today.AddDays(-3), EntryType = "Gider", Title = "Ambalaj Malzemesi", Category = "Diger", Source = "Manuel", Amount = 750.00m });
            _entries.Add(new GelirGiderEntry { Date = DateTime.Today.AddDays(-2), EntryType = "Gider", Title = "Personel Gideri", Category = "Insan Kaynaklari", Source = "Manuel", Amount = 12000.00m });
            _entries.Add(new GelirGiderEntry { Date = DateTime.Today.AddDays(-1), EntryType = "Gelir", Title = "Ciceksepeti Hakedis", Category = "Marketplace Satis", Source = "Manuel", Amount = 6320.00m });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var totalIncome = _entries.Where(e => e.EntryType == "Gelir").Sum(e => e.Amount);
            var totalExpense = _entries.Where(e => e.EntryType == "Gider").Sum(e => e.Amount);
            var net = totalIncome - totalExpense;

            TotalIncomeText.Text = $"{totalIncome:N2} TL";
            TotalExpenseText.Text = $"{totalExpense:N2} TL";
            NetText.Text = $"{net:N2} TL";
            NetText.Foreground = net >= 0 ? _profitBrush : _lossBrush;
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            // Intentional: reloads mock data — real filtering will be wired to backend
            LoadMockData();
        }

        private void AddIncome_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseAmount(out var amount)) return;

            _entries.Insert(0, new GelirGiderEntry
            {
                Date = EntryDate.SelectedDate ?? DateTime.Today,
                EntryType = "Gelir",
                Title = EntryTitle.Text?.Trim() ?? "Gelir",
                Category = (EntryCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Diger",
                Source = (EntrySource.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Manuel",
                Amount = amount
            });
            UpdateKpis();
            ClearForm();
        }

        private void AddExpense_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseAmount(out var amount)) return;

            _entries.Insert(0, new GelirGiderEntry
            {
                Date = EntryDate.SelectedDate ?? DateTime.Today,
                EntryType = "Gider",
                Title = EntryTitle.Text?.Trim() ?? "Gider",
                Category = (EntryCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Diger",
                Source = (EntrySource.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Manuel",
                Amount = amount
            });
            UpdateKpis();
            ClearForm();
        }

        private void DeleteEntry_Click(object sender, RoutedEventArgs e)
        {
            if (EntriesGrid.SelectedItem is not GelirGiderEntry selected)
            {
                MessageBox.Show("Silinecek kaydi seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var confirm = MessageBox.Show($"Kayit silinecek: {selected.Title}\nEmin misiniz?", "Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            _entries.Remove(selected);
            UpdateKpis();
        }

        private bool TryParseAmount(out decimal amount)
        {
            amount = 0;
            if (string.IsNullOrWhiteSpace(EntryAmount.Text))
            {
                MessageBox.Show("Tutar giriniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (!decimal.TryParse(EntryAmount.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out amount) || amount <= 0)
            {
                MessageBox.Show("Gecerli bir tutar giriniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void ClearForm()
        {
            EntryTitle.Text = "";
            EntryAmount.Text = "";
            EntryCategory.SelectedIndex = 0;
            EntryDate.SelectedDate = DateTime.Today;
            EntrySource.SelectedIndex = 0;
        }
    }

    internal sealed class GelirGiderEntry
    {
        public DateTime Date { get; set; }
        public string EntryType { get; set; } = "";
        public string Title { get; set; } = "";
        public string Category { get; set; } = "";
        public string Source { get; set; } = "";
        public decimal Amount { get; set; }
    }
}
