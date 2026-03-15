using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Accounting
{
    public partial class BankaHesaplariView : UserControl
    {
        private readonly ObservableCollection<BankTransactionItem> _transactions = new();

        private readonly BankAccountInfo[] _accounts =
        {
            new()
            {
                Name = "Garanti BBVA - Ticari Hesap",
                Iban = "TR12 **** **** **** 4567 89",
                Balance = 142580.75m
            },
            new()
            {
                Name = "Is Bankasi - Vadesiz TL",
                Iban = "TR98 **** **** **** 1234 56",
                Balance = 38920.00m
            }
        };

        public BankaHesaplariView()
        {
            InitializeComponent();
            TransactionsGrid.ItemsSource = _transactions;
            LoadAccountData(0);
        }

        private void LoadAccountData(int accountIndex)
        {
            if (accountIndex < 0 || accountIndex >= _accounts.Length) return;

            var account = _accounts[accountIndex];
            BalanceText.Text = $"{account.Balance:N2} TL";
            IbanText.Text = account.Iban;

            _transactions.Clear();

            if (accountIndex == 0)
            {
                LoadGarantiMockTransactions(account.Balance);
            }
            else
            {
                LoadIsBankMockTransactions(account.Balance);
            }

            var monthIn = _transactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var monthOut = _transactions.Where(t => t.Amount < 0).Sum(t => Math.Abs(t.Amount));
            MonthInText.Text = $"{monthIn:N2} TL";
            MonthOutText.Text = $"{monthOut:N2} TL";
        }

        private void LoadGarantiMockTransactions(decimal startBalance)
        {
            var balance = startBalance;
            var items = new[]
            {
                new { Days = -1, Desc = "Trendyol Hakedis Transferi", Type = "Havale", Amt = 15420.50m },
                new { Days = -2, Desc = "Kira Odemesi - ABC Holding", Type = "EFT", Amt = -8500.00m },
                new { Days = -3, Desc = "Hepsiburada Hakedis", Type = "Havale", Amt = 9870.25m },
                new { Days = -4, Desc = "N11 Hakedis Transferi", Type = "Havale", Amt = 4280.00m },
                new { Days = -5, Desc = "Kargo Firmasi Odemesi", Type = "EFT", Amt = -1250.30m },
                new { Days = -6, Desc = "Tedarikci Odemesi - Mega Amb.", Type = "EFT", Amt = -8400.00m },
                new { Days = -7, Desc = "OpenCart Satis Tahsilati", Type = "Havale", Amt = 2100.75m },
                new { Days = -8, Desc = "Vergi Odemesi", Type = "EFT", Amt = -4500.00m },
                new { Days = -9, Desc = "Ciceksepeti Hakedis", Type = "Havale", Amt = 6320.00m },
                new { Days = -10, Desc = "Personel Maasi", Type = "EFT", Amt = -12000.00m },
                new { Days = -11, Desc = "Pazarama Hakedis", Type = "Havale", Amt = 3150.00m },
                new { Days = -12, Desc = "Ambalaj Malzeme Odemesi", Type = "EFT", Amt = -750.00m },
                new { Days = -14, Desc = "Trendyol Hakedis - 2. Donem", Type = "Havale", Amt = 18900.00m },
                new { Days = -15, Desc = "SGK Primi Odemesi", Type = "EFT", Amt = -6200.00m },
                new { Days = -18, Desc = "Musteri Iade Transferi", Type = "EFT", Amt = -320.00m },
            };

            foreach (var item in items)
            {
                _transactions.Add(new BankTransactionItem
                {
                    Date = DateTime.Today.AddDays(item.Days),
                    Description = item.Desc,
                    TransactionType = item.Type,
                    Amount = item.Amt,
                    RunningBalance = balance,
                    IsReconciled = item.Days < -5
                });
                balance -= item.Amt;
            }
        }

        private void LoadIsBankMockTransactions(decimal startBalance)
        {
            var balance = startBalance;
            var items = new[]
            {
                new { Days = -1, Desc = "POS Tahsilat - Magazadan", Type = "POS", Amt = 3200.00m },
                new { Days = -2, Desc = "Elektrik Faturasi", Type = "Otomatik", Amt = -1850.00m },
                new { Days = -3, Desc = "Dogalgaz Faturasi", Type = "Otomatik", Amt = -920.00m },
                new { Days = -5, Desc = "Internet Faturasi", Type = "Otomatik", Amt = -450.00m },
                new { Days = -6, Desc = "POS Tahsilat - Magazadan", Type = "POS", Amt = 2800.00m },
                new { Days = -7, Desc = "Nakit Cekme", Type = "ATM", Amt = -2000.00m },
                new { Days = -9, Desc = "Banka Faiz Geliri", Type = "Faiz", Amt = 125.50m },
                new { Days = -10, Desc = "Kredi Karti Taksit", Type = "Otomatik", Amt = -3400.00m },
                new { Days = -12, Desc = "POS Tahsilat - Magazadan", Type = "POS", Amt = 4100.00m },
                new { Days = -14, Desc = "Sigorta Primi", Type = "EFT", Amt = -1200.00m },
                new { Days = -15, Desc = "Kira Geliri", Type = "Havale", Amt = 5500.00m },
                new { Days = -17, Desc = "Bakim Onarim Gideri", Type = "EFT", Amt = -800.00m },
                new { Days = -19, Desc = "POS Tahsilat - Magazadan", Type = "POS", Amt = 2650.00m },
                new { Days = -20, Desc = "Muhasebeci Odemesi", Type = "EFT", Amt = -3000.00m },
                new { Days = -22, Desc = "Havale - Tedarikci", Type = "Havale", Amt = -4500.00m },
            };

            foreach (var item in items)
            {
                _transactions.Add(new BankTransactionItem
                {
                    Date = DateTime.Today.AddDays(item.Days),
                    Description = item.Desc,
                    TransactionType = item.Type,
                    Amount = item.Amt,
                    RunningBalance = balance,
                    IsReconciled = item.Days < -10
                });
                balance -= item.Amt;
            }
        }

        private void AccountSelector_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (AccountSelector?.SelectedIndex >= 0)
            {
                LoadAccountData(AccountSelector.SelectedIndex);
            }
        }

        private void ImportStatement_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ekstre aktarma islevi yakin zamanda aktif olacak.\n(Banka API entegrasyonu tamamlandiginda etkinlestirilecek.)",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    internal sealed class BankAccountInfo
    {
        public string Name { get; set; } = "";
        public string Iban { get; set; } = "";
        public decimal Balance { get; set; }
    }

    internal sealed class BankTransactionItem
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public string TransactionType { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal RunningBalance { get; set; }
        public bool IsReconciled { get; set; }
    }
}
