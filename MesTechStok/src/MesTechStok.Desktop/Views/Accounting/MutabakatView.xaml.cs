using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Accounting
{
    public partial class MutabakatView : UserControl
    {
        private readonly ObservableCollection<ReconciliationItem> _autoMatched = new();
        private readonly ObservableCollection<ReconciliationItem> _needsReview = new();
        private readonly ObservableCollection<ReconciliationItem> _unmatched = new();

        public MutabakatView()
        {
            InitializeComponent();
            AutoMatchGrid.ItemsSource = _autoMatched;
            ReviewGrid.ItemsSource = _needsReview;
            UnmatchedGrid.ItemsSource = _unmatched;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _autoMatched.Clear();
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-1), BankDescription = "TRENDYOL HAKEDIS 2026-03-14", BankAmount = 15420.50m, OrderNumber = "TY-2026031401", OrderAmount = 15420.50m, Confidence = 98, Status = "Eslesti", Source = "Trendyol" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-2), BankDescription = "HEPSiBURADA TRANSFER", BankAmount = 9870.25m, OrderNumber = "HB-2026031201", OrderAmount = 9870.25m, Confidence = 97, Status = "Eslesti", Source = "Hepsiburada" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-3), BankDescription = "N11 HAKEDIS MART", BankAmount = 4280.00m, OrderNumber = "N11-2026031101", OrderAmount = 4280.00m, Confidence = 95, Status = "Eslesti", Source = "N11" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-4), BankDescription = "OPENCART ODEME #4521", BankAmount = 2100.75m, OrderNumber = "OC-4521", OrderAmount = 2100.75m, Confidence = 92, Status = "Eslesti", Source = "OpenCart" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-5), BankDescription = "CICEKSEPETI HAKEDIS", BankAmount = 6320.00m, OrderNumber = "CS-2026030901", OrderAmount = 6320.00m, Confidence = 96, Status = "Eslesti", Source = "Ciceksepeti" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-7), BankDescription = "PAZARAMA TRANSFER MAR", BankAmount = 3150.00m, OrderNumber = "PZ-2026030701", OrderAmount = 3150.00m, Confidence = 94, Status = "Eslesti", Source = "Pazarama" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-8), BankDescription = "TRENDYOL HAKEDIS 2 DON", BankAmount = 18900.00m, OrderNumber = "TY-2026030601", OrderAmount = 18900.00m, Confidence = 99, Status = "Eslesti", Source = "Trendyol" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-10), BankDescription = "N11 SIPARIS TAHSILAT", BankAmount = 5640.00m, OrderNumber = "N11-2026030401", OrderAmount = 5640.00m, Confidence = 91, Status = "Eslesti", Source = "N11" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-12), BankDescription = "HB HAKEDIS SUBAT SON", BankAmount = 12400.00m, OrderNumber = "HB-2026030201", OrderAmount = 12400.00m, Confidence = 93, Status = "Eslesti", Source = "Hepsiburada" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-14), BankDescription = "OPENCART POS TAHSILAT", BankAmount = 3800.00m, OrderNumber = "OC-4498", OrderAmount = 3800.00m, Confidence = 88, Status = "Eslesti", Source = "OpenCart" });

            _needsReview.Clear();
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-1), BankDescription = "HAVALE - MUSTERI A.Y.", BankAmount = 2400.00m, OrderNumber = "TY-2026031399?", OrderAmount = 2350.00m, Confidence = 72, Status = "Inceleme", Source = "Trendyol" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-3), BankDescription = "EFT MEHMET DEMIR", BankAmount = 1250.50m, OrderNumber = "HB-2026031098?", OrderAmount = 1300.00m, Confidence = 65, Status = "Inceleme", Source = "Hepsiburada" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-5), BankDescription = "KARGO IADE BEDELI", BankAmount = 320.00m, OrderNumber = "N11-RET-442?", OrderAmount = 280.00m, Confidence = 58, Status = "Inceleme", Source = "N11" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-6), BankDescription = "POS TAHSILAT 0314", BankAmount = 890.00m, OrderNumber = "OC-4510?", OrderAmount = 920.00m, Confidence = 68, Status = "Inceleme", Source = "OpenCart" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-8), BankDescription = "TRANSFER - FATMA K", BankAmount = 5600.00m, OrderNumber = "CS-2026030502?", OrderAmount = 5450.00m, Confidence = 55, Status = "Inceleme", Source = "Ciceksepeti" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-9), BankDescription = "HAVALE - OZEL SAGLIK", BankAmount = 3600.00m, OrderNumber = "TY-2026030501?", OrderAmount = 3500.00m, Confidence = 60, Status = "Inceleme", Source = "Trendyol" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-11), BankDescription = "EFT CAN ELEKTRONIK", BankAmount = 3400.00m, OrderNumber = "PZ-2026030301?", OrderAmount = 3250.00m, Confidence = 62, Status = "Inceleme", Source = "Pazarama" });

            _unmatched.Clear();
            _unmatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-2), BankDescription = "DEPO KIRA ODEMESI", BankAmount = -8500.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-4), BankDescription = "SGK PRIMI ODEMESI", BankAmount = -6200.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-6), BankDescription = "VERGI DAIRESI ODEME", BankAmount = -4500.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-7), BankDescription = "PERSONEL MAAS ODEMESI", BankAmount = -12000.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-9), BankDescription = "NAKIT CEKME ATM", BankAmount = -2000.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-10), BankDescription = "BILINMEYEN HAVALE #8821", BankAmount = 750.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Bilinmiyor" });
            _unmatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-13), BankDescription = "KREDI KARTI TAKSIT", BankAmount = -3400.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-15), BankDescription = "BANKA FAIZ GELIRI", BankAmount = 125.50m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var total = _autoMatched.Count + _needsReview.Count + _unmatched.Count;
            TotalText.Text = total.ToString();
            MatchedText.Text = _autoMatched.Count.ToString();
            ReviewText.Text = _needsReview.Count.ToString();
            UnmatchedText.Text = _unmatched.Count.ToString();
        }

        private void AutoMatch_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Otomatik eslestirme islemi yakin zamanda aktif olacak.\n(Banka API entegrasyonu tamamlandiginda etkinlestirilecek.)",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadMockData();
        }

        private void ApproveMatch_Click(object sender, RoutedEventArgs e)
        {
            if (ReviewGrid.SelectedItem is not ReconciliationItem selected)
            {
                MessageBox.Show("Onaylanacak eslestirmeyi seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            selected.Status = "Eslesti";
            _needsReview.Remove(selected);
            _autoMatched.Add(selected);
            UpdateKpis();
        }

        private void RejectMatch_Click(object sender, RoutedEventArgs e)
        {
            if (ReviewGrid.SelectedItem is not ReconciliationItem selected)
            {
                MessageBox.Show("Reddedilecek eslestirmeyi seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            selected.Status = "Eslenemedi";
            selected.Confidence = 0;
            _needsReview.Remove(selected);
            _unmatched.Add(selected);
            UpdateKpis();
        }
    }

    internal sealed class ReconciliationItem
    {
        public DateTime Date { get; set; }
        public string BankDescription { get; set; } = "";
        public decimal BankAmount { get; set; }
        public string OrderNumber { get; set; } = "";
        public decimal OrderAmount { get; set; }
        public int Confidence { get; set; }
        public string Status { get; set; } = "";
        public string Source { get; set; } = "";
    }
}
