using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Accounting
{
    public partial class CariHesaplarView : UserControl
    {
        private readonly ObservableCollection<CariAccountItem> _platformAccounts = new();
        private readonly ObservableCollection<CariAccountItem> _customerAccounts = new();
        private readonly ObservableCollection<CariAccountItem> _supplierAccounts = new();

        public CariHesaplarView()
        {
            InitializeComponent();
            PlatformGrid.ItemsSource = _platformAccounts;
            CustomerGrid.ItemsSource = _customerAccounts;
            SupplierGrid.ItemsSource = _supplierAccounts;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _platformAccounts.Clear();
            _platformAccounts.Add(new CariAccountItem { AccountName = "Trendyol Marketplace", TaxNumber = "4567890123", AccountType = "Platform", Debt = 0m, Credit = 45200.50m, Balance = -45200.50m, Phone = "-", LastTransaction = DateTime.Today.AddDays(-1) });
            _platformAccounts.Add(new CariAccountItem { AccountName = "Hepsiburada", TaxNumber = "5678901234", AccountType = "Platform", Debt = 0m, Credit = 18750.00m, Balance = -18750.00m, Phone = "-", LastTransaction = DateTime.Today.AddDays(-2) });
            _platformAccounts.Add(new CariAccountItem { AccountName = "N11", TaxNumber = "6789012345", AccountType = "Platform", Debt = 3200.00m, Credit = 0m, Balance = 3200.00m, Phone = "-", LastTransaction = DateTime.Today.AddDays(-3) });
            _platformAccounts.Add(new CariAccountItem { AccountName = "Ciceksepeti", TaxNumber = "7890123456", AccountType = "Platform", Debt = 0m, Credit = 9840.75m, Balance = -9840.75m, Phone = "-", LastTransaction = DateTime.Today.AddDays(-5) });
            _platformAccounts.Add(new CariAccountItem { AccountName = "Pazarama", TaxNumber = "8901234567", AccountType = "Platform", Debt = 1500.00m, Credit = 0m, Balance = 1500.00m, Phone = "-", LastTransaction = DateTime.Today.AddDays(-7) });

            _customerAccounts.Clear();
            _customerAccounts.Add(new CariAccountItem { AccountName = "Ahmet Yilmaz", TaxNumber = "12345678901", AccountType = "Musteri", Debt = 2400.00m, Credit = 0m, Balance = 2400.00m, Phone = "0532 111 2233", LastTransaction = DateTime.Today.AddDays(-1) });
            _customerAccounts.Add(new CariAccountItem { AccountName = "Mehmet Demir A.S.", TaxNumber = "9876543210", AccountType = "Musteri", Debt = 0m, Credit = 1250.50m, Balance = -1250.50m, Phone = "0212 555 4455", LastTransaction = DateTime.Today.AddDays(-3) });
            _customerAccounts.Add(new CariAccountItem { AccountName = "Fatma Kaya", TaxNumber = "11223344556", AccountType = "Musteri", Debt = 890.00m, Credit = 0m, Balance = 890.00m, Phone = "0544 666 7788", LastTransaction = DateTime.Today.AddDays(-4) });
            _customerAccounts.Add(new CariAccountItem { AccountName = "Ozel Saglik Ltd.", TaxNumber = "4455667788", AccountType = "Musteri", Debt = 5600.00m, Credit = 2000.00m, Balance = 3600.00m, Phone = "0216 333 9900", LastTransaction = DateTime.Today.AddDays(-6) });
            _customerAccounts.Add(new CariAccountItem { AccountName = "Can Elektronik", TaxNumber = "5566778899", AccountType = "Musteri", Debt = 0m, Credit = 3400.00m, Balance = -3400.00m, Phone = "0312 444 1122", LastTransaction = DateTime.Today.AddDays(-8) });

            _supplierAccounts.Clear();
            _supplierAccounts.Add(new CariAccountItem { AccountName = "ABC Tedarikci Ltd.", TaxNumber = "3456789012", AccountType = "Tedarikci", Debt = 0m, Credit = 25000.00m, Balance = -25000.00m, Phone = "0212 777 8899", LastTransaction = DateTime.Today.AddDays(-2) });
            _supplierAccounts.Add(new CariAccountItem { AccountName = "XYZ Lojistik A.S.", TaxNumber = "2345678901", AccountType = "Tedarikci", Debt = 1250.30m, Credit = 0m, Balance = 1250.30m, Phone = "0216 888 9900", LastTransaction = DateTime.Today.AddDays(-4) });
            _supplierAccounts.Add(new CariAccountItem { AccountName = "Mega Ambalaj San.", TaxNumber = "1234567890", AccountType = "Tedarikci", Debt = 0m, Credit = 8400.00m, Balance = -8400.00m, Phone = "0232 111 2233", LastTransaction = DateTime.Today.AddDays(-5) });
            _supplierAccounts.Add(new CariAccountItem { AccountName = "Guven Matbaa", TaxNumber = "0123456789", AccountType = "Tedarikci", Debt = 3200.00m, Credit = 0m, Balance = 3200.00m, Phone = "0322 444 5566", LastTransaction = DateTime.Today.AddDays(-7) });
            _supplierAccounts.Add(new CariAccountItem { AccountName = "Star Tekstil", TaxNumber = "9012345678", AccountType = "Tedarikci", Debt = 0m, Credit = 15800.00m, Balance = -15800.00m, Phone = "0224 666 7788", LastTransaction = DateTime.Today.AddDays(-9) });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var allAccounts = _platformAccounts.Concat(_customerAccounts).Concat(_supplierAccounts).ToList();
            TotalAccountsText.Text = allAccounts.Count.ToString();
            PlatformBalanceText.Text = $"{_platformAccounts.Sum(a => a.Balance):N2} TL";
            CustomerBalanceText.Text = $"{_customerAccounts.Sum(a => a.Balance):N2} TL";
            SupplierBalanceText.Text = $"{_supplierAccounts.Sum(a => a.Balance):N2} TL";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Intentional: basic client-side filter on mock data
            // Real search will query backend when DEV 1 CariHesap domain entity is ready
            var query = SearchBox.Text?.Trim().ToLowerInvariant() ?? "";
            if (string.IsNullOrEmpty(query))
            {
                LoadMockData();
                return;
            }
            FilterGrids(query);
        }

        private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Intentional: placeholder for type-based filter — reloads mock data
            LoadMockData();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadMockData();
        }

        private void FilterGrids(string query)
        {
            var platforms = _platformAccounts.Where(a => a.AccountName.ToLowerInvariant().Contains(query)).ToList();
            _platformAccounts.Clear();
            foreach (var item in platforms) _platformAccounts.Add(item);

            var customers = _customerAccounts.Where(a => a.AccountName.ToLowerInvariant().Contains(query)).ToList();
            _customerAccounts.Clear();
            foreach (var item in customers) _customerAccounts.Add(item);

            var suppliers = _supplierAccounts.Where(a => a.AccountName.ToLowerInvariant().Contains(query)).ToList();
            _supplierAccounts.Clear();
            foreach (var item in suppliers) _supplierAccounts.Add(item);

            UpdateKpis();
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class CariAccountItem
    {
        public string AccountName { get; set; } = "";
        public string TaxNumber { get; set; } = "";
        public string AccountType { get; set; } = "";
        public string Phone { get; set; } = "";
        public decimal Debt { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public DateTime LastTransaction { get; set; }
    }
}
