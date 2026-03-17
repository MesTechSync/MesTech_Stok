using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Finance
{
    public partial class SalaryView : UserControl
    {
        private readonly ObservableCollection<SalaryEntry> _entries = new();

        public SalaryView()
        {
            InitializeComponent();
            SalaryGrid.ItemsSource = _entries;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _entries.Clear();

            // 2026 asgari ucret baz: 22.104 TL brut
            // SGK isci %14, isveren %20.5 (tesvikli), issizlik isci %1, isveren %2
            // Gelir vergisi %15 (ilk dilim), damga vergisi %0.759
            _entries.Add(CreateSalaryEntry("Ahmet Yilmaz", "Depo Sorumlusu", 28000.00m));
            _entries.Add(CreateSalaryEntry("Fatma Kaya", "Satis Uzmani", 32000.00m));
            _entries.Add(CreateSalaryEntry("Mehmet Demir", "Muhasebe", 35000.00m));
            _entries.Add(CreateSalaryEntry("Ayse Celik", "Lojistik Operasyon", 26000.00m));
            _entries.Add(CreateSalaryEntry("Ali Ozturk", "IT Destek", 30000.00m));
            _entries.Add(CreateSalaryEntry("Zeynep Sahin", "E-Ticaret Uzmani", 33000.00m));
            _entries.Add(CreateSalaryEntry("Hasan Arslan", "Depo Personeli", 22104.00m));
            _entries.Add(CreateSalaryEntry("Elif Yildiz", "Musteri Hizmetleri", 24000.00m));

            UpdateKpis();
        }

        private static SalaryEntry CreateSalaryEntry(string name, string position, decimal gross)
        {
            // SGK isci: %14 + %1 issizlik = %15
            var sgkEmployee = gross * 0.15m;
            // SGK isveren: %20.5 + %2 issizlik = %22.5
            var sgkEmployer = gross * 0.225m;
            // Gelir vergisi matrah: brut - SGK isci
            var taxBase = gross - sgkEmployee;
            // Gelir vergisi: %15 (basit hesaplama, ilk dilim)
            var incomeTax = taxBase * 0.15m;
            // Damga vergisi: %0.759
            var stampTax = gross * 0.00759m;
            // Net maas
            var net = gross - sgkEmployee - incomeTax - stampTax;
            // Toplam isveren maliyeti
            var totalCost = gross + sgkEmployer;

            return new SalaryEntry
            {
                EmployeeName = name,
                Position = position,
                GrossSalary = Math.Round(gross, 2),
                SgkEmployee = Math.Round(sgkEmployee, 2),
                SgkEmployer = Math.Round(sgkEmployer, 2),
                IncomeTax = Math.Round(incomeTax, 2),
                StampTax = Math.Round(stampTax, 2),
                NetSalary = Math.Round(net, 2),
                TotalCost = Math.Round(totalCost, 2)
            };
        }

        private void UpdateKpis()
        {
            var totalGross = _entries.Sum(e => e.GrossSalary);
            var totalSgk = _entries.Sum(e => e.SgkEmployee + e.SgkEmployer);
            var totalTax = _entries.Sum(e => e.IncomeTax + e.StampTax);
            var totalNet = _entries.Sum(e => e.NetSalary);
            var annualCost = _entries.Sum(e => e.TotalCost) * 12m;

            TotalGrossText.Text = $"{totalGross:N2} TL";
            TotalSgkText.Text = $"{totalSgk:N2} TL";
            TotalTaxText.Text = $"{totalTax:N2} TL";
            TotalNetText.Text = $"{totalNet:N2} TL";
            AnnualCostText.Text = $"{annualCost:N2} TL";
            EmployeeCountText.Text = $"{_entries.Count} calisan";
        }

        private void PeriodFilter_Changed(object sender, SelectionChangedEventArgs e)
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

    internal sealed class SalaryEntry
    {
        public string EmployeeName { get; set; } = "";
        public string Position { get; set; } = "";
        public decimal GrossSalary { get; set; }
        public decimal SgkEmployee { get; set; }
        public decimal SgkEmployer { get; set; }
        public decimal IncomeTax { get; set; }
        public decimal StampTax { get; set; }
        public decimal NetSalary { get; set; }
        public decimal TotalCost { get; set; }
    }
}
