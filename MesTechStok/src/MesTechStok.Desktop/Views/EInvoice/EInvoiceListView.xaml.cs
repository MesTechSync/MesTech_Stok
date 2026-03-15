using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MesTechStok.Desktop.ViewModels.EInvoice;

namespace MesTechStok.Desktop.Views.EInvoice
{
    public partial class EInvoiceListView : UserControl
    {
        // ── pagination state ──────────────────────────────────────────────
        private const int DefaultPageSize = 25;
        private int _pageSize = DefaultPageSize;
        private int _currentPage = 1;
        private int _totalPages = 1;

        // ── data ─────────────────────────────────────────────────────────
        private readonly ObservableCollection<EInvoiceItem> _allItems = new();
        private readonly ObservableCollection<EInvoiceItem> _pageItems = new();
        private readonly ObservableCollection<PageButtonItem> _pageButtons = new();

        /// <summary>C-04 — DI constructor: ViewModel-first approach.</summary>
        public EInvoiceListView(EInvoiceListViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            // Keep legacy grid wiring for pages that don't yet bind via ViewModel
            InvoiceGrid.ItemsSource = _pageItems;
            PageButtons.ItemsSource = _pageButtons;
            LoadMockData();
            ApplyFilter();
        }

        /// <summary>Parameterless constructor kept for XAML designer support.</summary>
        public EInvoiceListView()
        {
            InitializeComponent();
            InvoiceGrid.ItemsSource = _pageItems;
            PageButtons.ItemsSource = _pageButtons;
            LoadMockData();
            ApplyFilter();
        }

        // ── mock data ─────────────────────────────────────────────────────
        private void LoadMockData()
        {
            _allItems.Clear();
            var providers = new[] { "Uyumsoft", "Logo", "Parasutte", "Mikro", "Luca" };
            var statuses = new[] { "Draft", "Sending", "Sent", "Accepted", "Rejected", "Cancelled", "Error" };
            var rng = new Random(42);
            for (int i = 1; i <= 73; i++)
            {
                var status = statuses[rng.Next(statuses.Length)];
                _allItems.Add(new EInvoiceItem
                {
                    Ettn = Guid.NewGuid().ToString().ToUpper(),
                    InvoiceDate = DateTime.Today.AddDays(-rng.Next(0, 90)),
                    BuyerTitle = $"Ornek Musteri {i} A.S.",
                    BuyerVkn = (1000000000L + rng.NextInt64(0, 9000000000L)).ToString(),
                    TotalAmount = Math.Round(rng.NextDouble() * 50000 + 100, 2),
                    TaxAmount = Math.Round(rng.NextDouble() * 9000 + 18, 2),
                    Status = status,
                    ProviderName = providers[rng.Next(providers.Length)]
                });
            }
        }

        // ── filter helpers ────────────────────────────────────────────────
        private void ApplyFilter()
        {
            var statusFilter = (CmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tum Durumlar";
            var providerFilter = (CmbProvider.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tum Entegratorler";
            var searchText = TxtSearch?.Text?.Trim() ?? "";
            var dateFrom = DpFrom?.SelectedDate;
            var dateTo = DpTo?.SelectedDate;

            var filtered = _allItems.AsEnumerable();

            if (statusFilter != "Tum Durumlar")
                filtered = filtered.Where(x => x.StatusDisplay == statusFilter);

            if (providerFilter != "Tum Entegratorler")
                filtered = filtered.Where(x => x.ProviderName == providerFilter);

            if (!string.IsNullOrEmpty(searchText))
                filtered = filtered.Where(x =>
                    x.Ettn.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    x.BuyerTitle.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    x.BuyerVkn.Contains(searchText, StringComparison.OrdinalIgnoreCase));

            if (dateFrom.HasValue)
                filtered = filtered.Where(x => x.InvoiceDate >= dateFrom.Value);
            if (dateTo.HasValue)
                filtered = filtered.Where(x => x.InvoiceDate <= dateTo.Value);

            var list = filtered.ToList();
            int total = list.Count;

            _totalPages = Math.Max(1, (int)Math.Ceiling((double)total / _pageSize));
            _currentPage = Math.Min(_currentPage, _totalPages);

            var page = list.Skip((_currentPage - 1) * _pageSize).Take(_pageSize).ToList();
            _pageItems.Clear();
            foreach (var item in page)
                _pageItems.Add(item);

            // update summary counters
            var allFiltered = filtered.ToList();
            TxtAcceptedCount.Text = $"{allFiltered.Count(x => x.Status == "Accepted")} Kabul Edildi";
            TxtPendingCount.Text = $"{allFiltered.Count(x => x.Status is "Sending" or "Sent")} Beklemede";
            TxtErrorCount.Text = $"{allFiltered.Count(x => x.Status is "Error" or "Rejected")} Hata";

            TxtRecordInfo.Text = $"Toplam {total} kayit | Sayfa {_currentPage} / {_totalPages}";
            TxtPageInfo.Text = $"Toplam {total} kayit | Sayfa {_currentPage} / {_totalPages}";

            EmptyState.Visibility = page.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            RefreshPageButtons();
            UpdatePaginationButtonStates();
        }

        private void RefreshPageButtons()
        {
            _pageButtons.Clear();
            int start = Math.Max(1, _currentPage - 2);
            int end = Math.Min(_totalPages, start + 4);
            start = Math.Max(1, end - 4);

            for (int p = start; p <= end; p++)
            {
                _pageButtons.Add(new PageButtonItem
                {
                    Label = p.ToString(),
                    PageNumber = p,
                    IsActive = p == _currentPage
                });
            }
        }

        private void UpdatePaginationButtonStates()
        {
            BtnFirstPage.IsEnabled = _currentPage > 1;
            BtnPrevPage.IsEnabled = _currentPage > 1;
            BtnNextPage.IsEnabled = _currentPage < _totalPages;
            BtnLastPage.IsEnabled = _currentPage < _totalPages;
        }

        // ── event handlers ────────────────────────────────────────────────
        private void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            ApplyFilter();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            DpFrom.SelectedDate = null;
            DpTo.SelectedDate = null;
            CmbStatus.SelectedIndex = 0;
            CmbProvider.SelectedIndex = 0;
            TxtSearch.Text = "";
            _currentPage = 1;
            ApplyFilter();
        }

        private void CmbStatus_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();
        private void CmbProvider_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilter();
        private void TxtSearch_Changed(object sender, TextChangedEventArgs e)
        {
            _currentPage = 1;
            ApplyFilter();
        }

        private void InvoiceGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        private void BtnNewInvoice_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Yeni e-fatura olusturma ekrani yakin zamanda aktif olacak.",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            var ettn = (sender as Button)?.Tag?.ToString() ?? "";
            MessageBox.Show($"Fatura gonderme islemi baslatildi.\nETTN: {ettn}",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnPdf_Click(object sender, RoutedEventArgs e)
        {
            var ettn = (sender as Button)?.Tag?.ToString() ?? "";
            MessageBox.Show($"PDF indirme islemi baslatildi.\nETTN: {ettn}",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var ettn = (sender as Button)?.Tag?.ToString() ?? "";
            var result = MessageBox.Show(
                $"Bu faturay\u0131 iptal etmek istedi\u011finizden emin misiniz?\nETTN: {ettn}",
                "Iptal Onay\u0131", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                MessageBox.Show("Fatura iptal islemi baslatildi.", "Bilgi",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // ── pagination buttons ────────────────────────────────────────────
        private void BtnFirstPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            ApplyFilter();
        }

        private void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1) { _currentPage--; ApplyFilter(); }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages) { _currentPage++; ApplyFilter(); }
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = _totalPages;
            ApplyFilter();
        }

        private void BtnPageNumber_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int page)
            {
                _currentPage = page;
                ApplyFilter();
            }
        }

        private void CmbPageSize_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (CmbPageSize.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Content?.ToString(), out int size))
            {
                _pageSize = size;
                _currentPage = 1;
                ApplyFilter();
            }
        }
    }

    // ── local model types ─────────────────────────────────────────────────
    internal sealed class EInvoiceItem
    {
        public string Ettn { get; set; } = "";
        public DateTime InvoiceDate { get; set; }
        public string BuyerTitle { get; set; } = "";
        public string BuyerVkn { get; set; } = "";
        public double TotalAmount { get; set; }
        public double TaxAmount { get; set; }
        public string Status { get; set; } = "";
        public string ProviderName { get; set; } = "";

        public string StatusDisplay => Status switch
        {
            "Draft"     => "Taslak",
            "Sending"   => "Gonderiliyor",
            "Sent"      => "Gonderildi",
            "Accepted"  => "Kabul Edildi",
            "Rejected"  => "Reddedildi",
            "Cancelled" => "Iptal Edildi",
            "Error"     => "Hata",
            _           => Status
        };
    }

    internal sealed class PageButtonItem
    {
        public string Label { get; set; } = "";
        public int PageNumber { get; set; }
        public bool IsActive { get; set; }
    }
}
