using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Accounting
{
    public partial class ChartOfAccountsView : UserControl
    {
        private readonly ObservableCollection<AccountRow> _accountRows = new();

        public ChartOfAccountsView()
        {
            InitializeComponent();
            AccountsGrid.ItemsSource = _accountRows;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _accountRows.Clear();

            _accountRows.Add(new AccountRow { AccountCode = "100", AccountName = "Kasa", GroupName = "1 - Donen Varliklar", ParentCode = "-", DebitBalance = 25400.00m, CreditBalance = 0.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "102", AccountName = "Bankalar", GroupName = "1 - Donen Varliklar", ParentCode = "-", DebitBalance = 148500.00m, CreditBalance = 0.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "102.01", AccountName = "Garanti TL Hesabi", GroupName = "1 - Donen Varliklar", ParentCode = "102", DebitBalance = 85200.00m, CreditBalance = 0.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "102.02", AccountName = "Ziraat TL Hesabi", GroupName = "1 - Donen Varliklar", ParentCode = "102", DebitBalance = 63300.00m, CreditBalance = 0.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "120", AccountName = "Alicilar", GroupName = "1 - Donen Varliklar", ParentCode = "-", DebitBalance = 42800.00m, CreditBalance = 0.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "153", AccountName = "Ticari Mallar", GroupName = "1 - Donen Varliklar", ParentCode = "-", DebitBalance = 320000.00m, CreditBalance = 0.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "320", AccountName = "Saticilar", GroupName = "3 - Kisa Vade Borclar", ParentCode = "-", DebitBalance = 0.00m, CreditBalance = 67200.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "360", AccountName = "Odenecek Vergi", GroupName = "3 - Kisa Vade Borclar", ParentCode = "-", DebitBalance = 0.00m, CreditBalance = 12400.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "600", AccountName = "Yurtici Satislar", GroupName = "6 - Gelir Tablosu", ParentCode = "-", DebitBalance = 0.00m, CreditBalance = 245000.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "621", AccountName = "Satilan Ticari Mal Maliyeti", GroupName = "6 - Gelir Tablosu", ParentCode = "-", DebitBalance = 142000.00m, CreditBalance = 0.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "653", AccountName = "Komisyon Giderleri", GroupName = "6 - Gelir Tablosu", ParentCode = "-", DebitBalance = 28600.00m, CreditBalance = 0.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "760", AccountName = "Pazarlama Giderleri", GroupName = "7 - Maliyet", ParentCode = "-", DebitBalance = 8500.00m, CreditBalance = 0.00m, Status = "Aktif" });
            _accountRows.Add(new AccountRow { AccountCode = "770", AccountName = "Genel Yonetim Giderleri", GroupName = "7 - Maliyet", ParentCode = "-", DebitBalance = 15200.00m, CreditBalance = 0.00m, Status = "Aktif" });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            TotalAccountsText.Text = _accountRows.Count.ToString();
            ActiveAccountsText.Text = _accountRows.Count(r => r.Status == "Aktif").ToString();
            MainGroupText.Text = _accountRows.Select(r => r.GroupName).Distinct().Count().ToString();
            SubAccountsText.Text = _accountRows.Count(r => r.ParentCode != "-").ToString();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => LoadMockData();
        private void GroupFilter_Changed(object sender, SelectionChangedEventArgs e) => LoadMockData();
        private void Refresh_Click(object sender, RoutedEventArgs e) => LoadMockData();
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Hesap plani disa aktarma islemi yakin zamanda aktif olacak.",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class AccountRow
    {
        public string AccountCode { get; set; } = "";
        public string AccountName { get; set; } = "";
        public string GroupName { get; set; } = "";
        public string ParentCode { get; set; } = "";
        public decimal DebitBalance { get; set; }
        public decimal CreditBalance { get; set; }
        public string Status { get; set; } = "";
    }
}
