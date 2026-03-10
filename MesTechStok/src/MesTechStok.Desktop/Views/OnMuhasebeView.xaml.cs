using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesTechStok.Desktop.Views
{
    public partial class OnMuhasebeView : UserControl
    {
        private static readonly SolidColorBrush _profitBrush;
        private static readonly SolidColorBrush _lossBrush;

        static OnMuhasebeView()
        {
            _profitBrush = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
            _profitBrush.Freeze();
            _lossBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45));
            _lossBrush.Freeze();
        }

        private readonly ObservableCollection<IncomeItem> _incomes = new();
        private readonly ObservableCollection<ExpenseItem> _expenses = new();
        private readonly ObservableCollection<CariHesapItem> _cariAccounts = new();

        public OnMuhasebeView()
        {
            InitializeComponent();
            PeriodStart.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            PeriodEnd.SelectedDate = DateTime.Today;
            IncomeGrid.ItemsSource = _incomes;
            ExpenseGrid.ItemsSource = _expenses;
            CariGrid.ItemsSource = _cariAccounts;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _incomes.Clear();
            _incomes.Add(new IncomeItem { Date = DateTime.Today.AddDays(-10), Description = "Trendyol Mart 1. Hafta Satışları", Category = "Marketplace Satış", Platform = "Trendyol", Amount = 15420.50m });
            _incomes.Add(new IncomeItem { Date = DateTime.Today.AddDays(-7), Description = "N11 Sipariş Tahsilatı", Category = "Marketplace Satış", Platform = "N11", Amount = 4280.00m });
            _incomes.Add(new IncomeItem { Date = DateTime.Today.AddDays(-3), Description = "OpenCart Web Satış", Category = "Direkt Satış", Platform = "OpenCart", Amount = 2100.75m });
            _incomes.Add(new IncomeItem { Date = DateTime.Today.AddDays(-1), Description = "Hepsiburada Komisyon İadesi", Category = "İade/Düzeltme", Platform = "Hepsiburada", Amount = 320.00m });

            _expenses.Clear();
            _expenses.Add(new ExpenseItem { Date = DateTime.Today.AddDays(-12), Description = "Depo Kira Ödemesi", Category = "Kira", Amount = 8500.00m });
            _expenses.Add(new ExpenseItem { Date = DateTime.Today.AddDays(-8), Description = "Kargo Giderleri (Yurtiçi)", Category = "Lojistik", Amount = 1250.30m });
            _expenses.Add(new ExpenseItem { Date = DateTime.Today.AddDays(-5), Description = "Marketplace Komisyonları", Category = "Komisyon", Amount = 3200.00m });
            _expenses.Add(new ExpenseItem { Date = DateTime.Today.AddDays(-2), Description = "Personel Gideri", Category = "İnsan Kaynakları", Amount = 12000.00m });

            _cariAccounts.Clear();
            _cariAccounts.Add(new CariHesapItem { AccountName = "ABC Tedarikçi Ltd.", TaxNumber = "3456789012", AccountType = "Tedarikçi", Debt = 0m, Credit = 25000m, Balance = -25000m });
            _cariAccounts.Add(new CariHesapItem { AccountName = "XYZ Lojistik A.Ş.", TaxNumber = "9876543210", AccountType = "Hizmet", Debt = 1250.30m, Credit = 0m, Balance = 1250.30m });
            _cariAccounts.Add(new CariHesapItem { AccountName = "Trendyol Marketplace", TaxNumber = "4567890123", AccountType = "Platform", Debt = 0m, Credit = 15420.50m, Balance = -15420.50m });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var totalIncome = _incomes.Sum(i => i.Amount);
            var totalExpense = _expenses.Sum(e => e.Amount);
            var netProfit = totalIncome - totalExpense;
            // Overdue: cari accounts where balance < 0 (you owe them money)
            var overdue = _cariAccounts.Count(c => c.Balance < 0);

            TotalIncomeText.Text = $"{totalIncome:N2} TL";
            TotalExpenseText.Text = $"{totalExpense:N2} TL";
            NetProfitText.Text = $"{netProfit:N2} TL";
            NetProfitText.Foreground = netProfit >= 0 ? _profitBrush : _lossBrush;
            OverdueText.Text = overdue.ToString();
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            // Intentional: reloads mock data on filter — any UI-only deletes are reset.
            // Real date-range filtering will be wired when DEV 1 Income/Expense domain entities are complete.
            LoadMockData();
        }

        private void AddIncome_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Gelir ekleme ekranı yakında aktif olacak.\n(DEV 1 domain entity tamamlandığında etkinleştirilecek.)",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteIncome_Click(object sender, RoutedEventArgs e)
        {
            if (IncomeGrid.SelectedItem is not IncomeItem selected) { MessageBox.Show("Silinecek gelir kaydını seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var confirm = MessageBox.Show($"Gelir kaydı silinecek: {selected.Description}\nEmin misiniz?", "Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            _incomes.Remove(selected);
            UpdateKpis();
        }

        private void AddExpense_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Gider ekleme ekranı yakında aktif olacak.\n(DEV 1 domain entity tamamlandığında etkinleştirilecek.)",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteExpense_Click(object sender, RoutedEventArgs e)
        {
            if (ExpenseGrid.SelectedItem is not ExpenseItem selected) { MessageBox.Show("Silinecek gider kaydını seçiniz.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var confirm = MessageBox.Show($"Gider kaydı silinecek: {selected.Description}\nEmin misiniz?", "Sil", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            _expenses.Remove(selected);
            UpdateKpis();
        }
    }

    internal sealed class IncomeItem
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Platform { get; set; } = "";
        public decimal Amount { get; set; }
    }

    internal sealed class ExpenseItem
    {
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal Amount { get; set; }
    }

    internal sealed class CariHesapItem
    {
        public string AccountName { get; set; } = "";
        public string TaxNumber { get; set; } = "";
        public string AccountType { get; set; } = "";
        public decimal Debt { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }
}
