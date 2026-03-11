using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views
{
    public partial class QuotationView : UserControl
    {
        private readonly ObservableCollection<QuotationItem> _allQuotations = new();
        private readonly ObservableCollection<QuotationItem> _filteredQuotations = new();

        public QuotationView()
        {
            InitializeComponent();
            QuotationGrid.ItemsSource = _filteredQuotations;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _allQuotations.Clear();
            _allQuotations.Add(new QuotationItem { QuotationNumber = "TKL-2026-0001", CustomerName = "ABC Ticaret Ltd.", Date = DateTime.Today.AddDays(-15), ValidUntil = DateTime.Today.AddDays(15), Status = "Onaylandi", Amount = 12500.00m, PreparedBy = "Ahmet Yilmaz" });
            _allQuotations.Add(new QuotationItem { QuotationNumber = "TKL-2026-0002", CustomerName = "XYZ Insaat A.S.", Date = DateTime.Today.AddDays(-10), ValidUntil = DateTime.Today.AddDays(20), Status = "Onay Bekliyor", Amount = 8750.50m, PreparedBy = "Fatma Kaya" });
            _allQuotations.Add(new QuotationItem { QuotationNumber = "TKL-2026-0003", CustomerName = "Mehmet Celik Kuyumculuk", Date = DateTime.Today.AddDays(-7), ValidUntil = DateTime.Today.AddDays(23), Status = "Taslak", Amount = 3200.00m, PreparedBy = "Ali Demir" });
            _allQuotations.Add(new QuotationItem { QuotationNumber = "TKL-2026-0004", CustomerName = "Delta Elektronik San.", Date = DateTime.Today.AddDays(-5), ValidUntil = DateTime.Today.AddDays(25), Status = "Onay Bekliyor", Amount = 45600.75m, PreparedBy = "Ahmet Yilmaz" });
            _allQuotations.Add(new QuotationItem { QuotationNumber = "TKL-2026-0005", CustomerName = "Gamma Gida Paz. Ltd.", Date = DateTime.Today.AddDays(-20), ValidUntil = DateTime.Today.AddDays(-5), Status = "Reddedildi", Amount = 6800.00m, PreparedBy = "Fatma Kaya" });
            _allQuotations.Add(new QuotationItem { QuotationNumber = "TKL-2026-0006", CustomerName = "Epsilon Tekstil A.S.", Date = DateTime.Today.AddDays(-30), ValidUntil = DateTime.Today.AddDays(-15), Status = "Faturalandi", Amount = 22300.00m, PreparedBy = "Ali Demir" });

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            var searchText = SearchBox?.Text?.Trim().ToLowerInvariant() ?? string.Empty;
            var statusSel = (StatusFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tum Durumlar";

            _filteredQuotations.Clear();
            foreach (var q in _allQuotations)
            {
                var matchesSearch = string.IsNullOrEmpty(searchText)
                    || q.QuotationNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || q.CustomerName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                    || q.PreparedBy.Contains(searchText, StringComparison.OrdinalIgnoreCase);

                var matchesStatus = statusSel == "Tum Durumlar" || q.Status == statusSel;

                if (matchesSearch && matchesStatus)
                    _filteredQuotations.Add(q);
            }

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            TotalQuotationsText.Text = _allQuotations.Count.ToString();
            PendingText.Text = _allQuotations.Count(q => q.Status == "Onay Bekliyor").ToString();
            ApprovedText.Text = _allQuotations.Count(q => q.Status == "Onaylandi" || q.Status == "Faturalandi").ToString();
            TotalAmountText.Text = $"{_allQuotations.Sum(q => q.Amount):N2} TL";
        }

        private void NewQuotation_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Yeni teklif olusturma ekrani yakinda aktif olacak.\n(DEV 1 Quotation domain entity tamamlandiginda etkinlestirilecek.)",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SendForApproval_Click(object sender, RoutedEventArgs e)
        {
            if (QuotationGrid.SelectedItem is not QuotationItem selected)
            {
                MessageBox.Show("Onaya gondermek icin bir teklif secin.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (selected.Status != "Taslak")
            {
                MessageBox.Show($"Yalnizca 'Taslak' durumundaki teklifler onaya gonderilebilir.\nSecilen: {selected.Status}", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            selected.Status = "Onay Bekliyor";
            ApplyFilter();
        }

        private void RejectQuotation_Click(object sender, RoutedEventArgs e)
        {
            if (QuotationGrid.SelectedItem is not QuotationItem selected)
            {
                MessageBox.Show("Reddetmek icin bir teklif secin.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var confirm = MessageBox.Show($"Teklif reddedilecek: {selected.QuotationNumber}\nEmin misiniz?",
                "Reddet", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            selected.Status = "Reddedildi";
            ApplyFilter();
        }

        private void ConvertToInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (QuotationGrid.SelectedItem is not QuotationItem selected)
            {
                MessageBox.Show("Faturaya donusturmek icin bir teklif secin.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (selected.Status != "Onaylandi")
            {
                MessageBox.Show($"Yalnizca 'Onaylandi' durumundaki teklifler faturaya donusturulebilir.\nSecilen: {selected.Status}", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var confirm = MessageBox.Show($"Teklif faturaya donusturulecek: {selected.QuotationNumber} — {selected.Amount:N2} TL\nEmin misiniz?",
                "Faturaya Donustur", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;
            selected.Status = "Faturalandi";
            ApplyFilter();
            MessageBox.Show("Teklif basariyla faturaya donusturuldu.\n(DEV 1 fatura entegrasyonu tamamlandiginda e-fatura otomatik kesilecek.)",
                "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DownloadPdf_Click(object sender, RoutedEventArgs e)
        {
            if (QuotationGrid.SelectedItem is not QuotationItem selected)
            {
                MessageBox.Show("PDF indirmek icin bir teklif secin.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MessageBox.Show($"PDF indirme: {selected.QuotationNumber}\n(PDF uretimi DEV 1 domain entity tamamlandiginda aktif olacak.)",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

        private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();

        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadMockData();
    }

    internal sealed class QuotationItem
    {
        public string QuotationNumber { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public DateTime Date { get; set; }
        public DateTime ValidUntil { get; set; }
        public string Status { get; set; } = "";
        public decimal Amount { get; set; }
        public string PreparedBy { get; set; } = "";
    }
}
