using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Accounting
{
    public partial class JournalEntryView : UserControl
    {
        private readonly ObservableCollection<JournalEntryRow> _journalRows = new();

        public JournalEntryView()
        {
            InitializeComponent();
            JournalGrid.ItemsSource = _journalRows;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _journalRows.Clear();

            // Satis fisi
            _journalRows.Add(new JournalEntryRow { VoucherNo = "YMF-2026-001", EntryDate = new DateTime(2026, 3, 14), AccountCode = "120", AccountName = "Alicilar", Description = "Trendyol satis hakedisi", Debit = 45200.50m, Credit = 0.00m, Source = "Otomatik", Status = "Onaylandi" });
            _journalRows.Add(new JournalEntryRow { VoucherNo = "YMF-2026-001", EntryDate = new DateTime(2026, 3, 14), AccountCode = "600", AccountName = "Yurtici Satislar", Description = "Trendyol satis hakedisi", Debit = 0.00m, Credit = 38305.08m, Source = "Otomatik", Status = "Onaylandi" });
            _journalRows.Add(new JournalEntryRow { VoucherNo = "YMF-2026-001", EntryDate = new DateTime(2026, 3, 14), AccountCode = "391", AccountName = "Hesaplanan KDV", Description = "Trendyol satis KDV", Debit = 0.00m, Credit = 6895.42m, Source = "Otomatik", Status = "Onaylandi" });

            // Komisyon fisi
            _journalRows.Add(new JournalEntryRow { VoucherNo = "YMF-2026-002", EntryDate = new DateTime(2026, 3, 14), AccountCode = "653", AccountName = "Komisyon Giderleri", Description = "Trendyol komisyon kesintisi", Debit = 6780.08m, Credit = 0.00m, Source = "Otomatik", Status = "Onaylandi" });
            _journalRows.Add(new JournalEntryRow { VoucherNo = "YMF-2026-002", EntryDate = new DateTime(2026, 3, 14), AccountCode = "120", AccountName = "Alicilar", Description = "Trendyol komisyon mahsubu", Debit = 0.00m, Credit = 6780.08m, Source = "Otomatik", Status = "Onaylandi" });

            // Kargo fisi
            _journalRows.Add(new JournalEntryRow { VoucherNo = "YMF-2026-003", EntryDate = new DateTime(2026, 3, 13), AccountCode = "760", AccountName = "Pazarlama Giderleri", Description = "Kargo gideri - Yurtici", Debit = 2260.00m, Credit = 0.00m, Source = "Manuel", Status = "Onaylandi" });
            _journalRows.Add(new JournalEntryRow { VoucherNo = "YMF-2026-003", EntryDate = new DateTime(2026, 3, 13), AccountCode = "320", AccountName = "Saticilar", Description = "Kargo borcu - Yurtici", Debit = 0.00m, Credit = 2260.00m, Source = "Manuel", Status = "Onaylandi" });

            // Banka tahsilat fisi
            _journalRows.Add(new JournalEntryRow { VoucherNo = "YMF-2026-004", EntryDate = new DateTime(2026, 3, 12), AccountCode = "102", AccountName = "Bankalar", Description = "Hepsiburada hakedis tahsilati", Debit = 18750.00m, Credit = 0.00m, Source = "Otomatik", Status = "Onaylandi" });
            _journalRows.Add(new JournalEntryRow { VoucherNo = "YMF-2026-004", EntryDate = new DateTime(2026, 3, 12), AccountCode = "120", AccountName = "Alicilar", Description = "Hepsiburada alacak kapanisi", Debit = 0.00m, Credit = 18750.00m, Source = "Otomatik", Status = "Onaylandi" });

            // Taslak fis
            _journalRows.Add(new JournalEntryRow { VoucherNo = "YMF-2026-005", EntryDate = new DateTime(2026, 3, 15), AccountCode = "770", AccountName = "Genel Yonetim Giderleri", Description = "Ofis kirasi - Mart", Debit = 8500.00m, Credit = 0.00m, Source = "Manuel", Status = "Taslak" });
            _journalRows.Add(new JournalEntryRow { VoucherNo = "YMF-2026-005", EntryDate = new DateTime(2026, 3, 15), AccountCode = "102", AccountName = "Bankalar", Description = "Ofis kirasi odemesi", Debit = 0.00m, Credit = 8500.00m, Source = "Manuel", Status = "Taslak" });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var totalDebit = _journalRows.Sum(r => r.Debit);
            var totalCredit = _journalRows.Sum(r => r.Credit);
            var voucherCount = _journalRows.Select(r => r.VoucherNo).Distinct().Count();
            var draftCount = _journalRows.Where(r => r.Status == "Taslak").Select(r => r.VoucherNo).Distinct().Count();

            TotalEntriesText.Text = voucherCount.ToString();
            TotalDebitText.Text = $"{totalDebit:N2} TL";
            TotalCreditText.Text = $"{totalCredit:N2} TL";
            BalanceDiffText.Text = $"{Math.Abs(totalDebit - totalCredit):N2} TL";
            DraftCountText.Text = draftCount.ToString();
        }

        private void PeriodFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadMockData();
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadMockData();
        private void NewEntry_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Yeni yevmiye fisi olusturma islemi yakin zamanda aktif olacak.",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Yevmiye defteri disa aktarma islemi yakin zamanda aktif olacak.",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class JournalEntryRow
    {
        public string VoucherNo { get; set; } = "";
        public DateTime EntryDate { get; set; }
        public string AccountCode { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Source { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
