using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MesTechStok.Desktop.Views.Accounting
{
    public partial class MutabakatView : UserControl
    {
        private readonly ObservableCollection<ReconciliationItem> _autoMatched = new();
        private readonly ObservableCollection<ReconciliationItem> _needsReview = new();
        private readonly ObservableCollection<ReconciliationItem> _unmatchedSettlements = new();
        private readonly ObservableCollection<ReconciliationItem> _unmatchedBankTx = new();

        public MutabakatView()
        {
            InitializeComponent();
            AutoMatchGrid.ItemsSource = _autoMatched;
            ReviewGrid.ItemsSource = _needsReview;
            UnmatchedSettlementGrid.ItemsSource = _unmatchedSettlements;
            UnmatchedBankGrid.ItemsSource = _unmatchedBankTx;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _autoMatched.Clear();
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-1), SettlementRef = "STL-TY-20260314-001", BankDescription = "TRENDYOL HAKEDIS 2026-03-14", BankAmount = 15420.50m, OrderNumber = "TY-2026031401", OrderAmount = 15420.50m, Confidence = 98, Status = "Eslesti", Source = "Trendyol" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-2), SettlementRef = "STL-HB-20260312-001", BankDescription = "HEPSIBURADA TRANSFER", BankAmount = 9870.25m, OrderNumber = "HB-2026031201", OrderAmount = 9870.25m, Confidence = 97, Status = "Eslesti", Source = "Hepsiburada" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-3), SettlementRef = "STL-N11-20260311-001", BankDescription = "N11 HAKEDIS MART", BankAmount = 4280.00m, OrderNumber = "N11-2026031101", OrderAmount = 4280.00m, Confidence = 95, Status = "Eslesti", Source = "N11" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-4), SettlementRef = "STL-OC-20260310-001", BankDescription = "OPENCART ODEME #4521", BankAmount = 2100.75m, OrderNumber = "OC-4521", OrderAmount = 2100.75m, Confidence = 92, Status = "Eslesti", Source = "OpenCart" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-5), SettlementRef = "STL-CS-20260309-001", BankDescription = "CICEKSEPETI HAKEDIS", BankAmount = 6320.00m, OrderNumber = "CS-2026030901", OrderAmount = 6320.00m, Confidence = 96, Status = "Eslesti", Source = "Ciceksepeti" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-7), SettlementRef = "STL-PZ-20260307-001", BankDescription = "PAZARAMA TRANSFER MAR", BankAmount = 3150.00m, OrderNumber = "PZ-2026030701", OrderAmount = 3150.00m, Confidence = 94, Status = "Eslesti", Source = "Pazarama" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-8), SettlementRef = "STL-TY-20260306-002", BankDescription = "TRENDYOL HAKEDIS 2 DON", BankAmount = 18900.00m, OrderNumber = "TY-2026030601", OrderAmount = 18900.00m, Confidence = 99, Status = "Eslesti", Source = "Trendyol" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-10), SettlementRef = "STL-N11-20260304-001", BankDescription = "N11 SIPARIS TAHSILAT", BankAmount = 5640.00m, OrderNumber = "N11-2026030401", OrderAmount = 5640.00m, Confidence = 91, Status = "Eslesti", Source = "N11" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-12), SettlementRef = "STL-HB-20260302-001", BankDescription = "HB HAKEDIS SUBAT SON", BankAmount = 12400.00m, OrderNumber = "HB-2026030201", OrderAmount = 12400.00m, Confidence = 93, Status = "Eslesti", Source = "Hepsiburada" });
            _autoMatched.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-14), SettlementRef = "STL-OC-20260228-001", BankDescription = "OPENCART POS TAHSILAT", BankAmount = 3800.00m, OrderNumber = "OC-4498", OrderAmount = 3800.00m, Confidence = 88, Status = "Eslesti", Source = "OpenCart" });

            _needsReview.Clear();
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-1), SettlementRef = "STL-TY-20260313-099", BankDescription = "HAVALE - MUSTERI A.Y.", BankAmount = 2400.00m, OrderNumber = "TY-2026031399?", OrderAmount = 2350.00m, Confidence = 72, Status = "Inceleme", Source = "Trendyol" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-3), SettlementRef = "STL-HB-20260310-098", BankDescription = "EFT MEHMET DEMIR", BankAmount = 1250.50m, OrderNumber = "HB-2026031098?", OrderAmount = 1300.00m, Confidence = 65, Status = "Inceleme", Source = "Hepsiburada" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-5), SettlementRef = "STL-N11-RET-442", BankDescription = "KARGO IADE BEDELI", BankAmount = 320.00m, OrderNumber = "N11-RET-442?", OrderAmount = 280.00m, Confidence = 58, Status = "Inceleme", Source = "N11" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-6), SettlementRef = "STL-OC-20260308-010", BankDescription = "POS TAHSILAT 0314", BankAmount = 890.00m, OrderNumber = "OC-4510?", OrderAmount = 920.00m, Confidence = 68, Status = "Inceleme", Source = "OpenCart" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-8), SettlementRef = "STL-CS-20260305-002", BankDescription = "TRANSFER - FATMA K", BankAmount = 5600.00m, OrderNumber = "CS-2026030502?", OrderAmount = 5450.00m, Confidence = 55, Status = "Inceleme", Source = "Ciceksepeti" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-9), SettlementRef = "STL-TY-20260305-001", BankDescription = "HAVALE - OZEL SAGLIK", BankAmount = 3600.00m, OrderNumber = "TY-2026030501?", OrderAmount = 3500.00m, Confidence = 60, Status = "Inceleme", Source = "Trendyol" });
            _needsReview.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-11), SettlementRef = "STL-PZ-20260303-001", BankDescription = "EFT CAN ELEKTRONIK", BankAmount = 3400.00m, OrderNumber = "PZ-2026030301?", OrderAmount = 3250.00m, Confidence = 62, Status = "Inceleme", Source = "Pazarama" });

            _unmatchedSettlements.Clear();
            _unmatchedSettlements.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-2), SettlementRef = "STL-TY-20260312-UNM", BankDescription = "Trendyol hakedis fark", BankAmount = 4200.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Trendyol" });
            _unmatchedSettlements.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-5), SettlementRef = "STL-HB-20260309-UNM", BankDescription = "HB iade mahsubu", BankAmount = 1850.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Hepsiburada" });
            _unmatchedSettlements.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-9), SettlementRef = "STL-CS-20260305-UNM", BankDescription = "CS komisyon fark", BankAmount = 620.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Ciceksepeti" });

            _unmatchedBankTx.Clear();
            _unmatchedBankTx.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-2), BankDescription = "DEPO KIRA ODEMESI", BankAmount = -8500.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatchedBankTx.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-4), BankDescription = "SGK PRIMI ODEMESI", BankAmount = -6200.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatchedBankTx.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-6), BankDescription = "VERGI DAIRESI ODEME", BankAmount = -4500.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatchedBankTx.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-7), BankDescription = "PERSONEL MAAS ODEMESI", BankAmount = -12000.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatchedBankTx.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-9), BankDescription = "NAKIT CEKME ATM", BankAmount = -2000.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatchedBankTx.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-10), BankDescription = "BILINMEYEN HAVALE #8821", BankAmount = 750.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Bilinmiyor", Source = "Bilinmiyor" });
            _unmatchedBankTx.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-13), BankDescription = "KREDI KARTI TAKSIT", BankAmount = -3400.00m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });
            _unmatchedBankTx.Add(new ReconciliationItem { Date = DateTime.Today.AddDays(-15), BankDescription = "BANKA FAIZ GELIRI", BankAmount = 125.50m, OrderNumber = "-", OrderAmount = 0m, Confidence = 0, Status = "Eslenemedi", Source = "Banka" });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var unmatchedTotal = _unmatchedSettlements.Count + _unmatchedBankTx.Count;
            var total = _autoMatched.Count + _needsReview.Count + unmatchedTotal;
            TotalText.Text = total.ToString();
            MatchedText.Text = _autoMatched.Count.ToString();
            ReviewText.Text = _needsReview.Count.ToString();
            UnmatchedText.Text = unmatchedTotal.ToString();

            var matchedAmount = _autoMatched.Sum(x => Math.Abs(x.BankAmount));
            var reviewAmount = _needsReview.Sum(x => Math.Abs(x.BankAmount));
            var unmatchedAmount = _unmatchedSettlements.Sum(x => Math.Abs(x.BankAmount)) + _unmatchedBankTx.Sum(x => Math.Abs(x.BankAmount));
            var totalAmount = matchedAmount + reviewAmount + unmatchedAmount;

            TotalAmountText.Text = $"{totalAmount:N2} TL";
            MatchedAmountText.Text = $"{matchedAmount:N2} TL";
            ReviewAmountText.Text = $"{reviewAmount:N2} TL";
            UnmatchedAmountText.Text = $"{unmatchedAmount:N2} TL";

            AutoMatchInfoText.Text = $"{_autoMatched.Count} eslestirme, toplam {matchedAmount:N2} TL";
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

        private void BulkApprove_Click(object sender, RoutedEventArgs e)
        {
            var selected = _autoMatched.Where(x => x.IsSelected).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Toplu onay icin en az bir satir seciniz.\n(CheckBox ile isaretle)", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"{selected.Count} eslestirme toplu olarak onaylanacak.\nDevam edilsin mi?",
                "Toplu Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                foreach (var item in selected)
                {
                    item.Status = "Onaylandi";
                    item.IsSelected = false;
                }
                MessageBox.Show($"{selected.Count} eslestirme onaylandi.", "Basarili", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateKpis();
            }
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
            ClearDetailPanel();
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
            _unmatchedSettlements.Add(selected);
            ClearDetailPanel();
            UpdateKpis();
        }

        private void RematchItem_Click(object sender, RoutedEventArgs e)
        {
            if (ReviewGrid.SelectedItem is not ReconciliationItem selected)
            {
                MessageBox.Show("Farkli eslestirme yapilacak satiri seciniz.", "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MessageBox.Show(
                $"'{selected.SettlementRef}' icin farkli eslestirme secimi yakin zamanda aktif olacak.\n(Manuel eslestirme motoru tamamlandiginda etkinlestirilecek.)",
                "Bilgi", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ManualMatch_Click(object sender, RoutedEventArgs e)
        {
            var settlement = UnmatchedSettlementGrid.SelectedItem as ReconciliationItem;
            var bankTx = UnmatchedBankGrid.SelectedItem as ReconciliationItem;

            if (settlement == null || bankTx == null)
            {
                MessageBox.Show("Sol taraftan bir settlement ve sag taraftan bir banka islemi seciniz.",
                    "Uyari", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var diff = Math.Abs(settlement.BankAmount - Math.Abs(bankTx.BankAmount));
            var result = MessageBox.Show(
                $"Settlement: {settlement.SettlementRef} ({settlement.BankAmount:N2} TL)\n" +
                $"Banka: {bankTx.BankDescription} ({bankTx.BankAmount:N2} TL)\n" +
                $"Fark: {diff:N2} TL\n\nBu eslestirme yapilsin mi?",
                "Manuel Eslestirme", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                settlement.Status = "Manuel Eslesti";
                settlement.OrderNumber = bankTx.BankDescription;
                settlement.Confidence = 100;
                _unmatchedSettlements.Remove(settlement);
                _unmatchedBankTx.Remove(bankTx);
                _autoMatched.Add(settlement);
                UpdateKpis();
            }
        }

        private void ReviewGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReviewGrid.SelectedItem is ReconciliationItem selected)
            {
                DetailSettlementRef.Text = selected.SettlementRef;
                DetailSettlementAmount.Text = $"Tutar: {selected.OrderAmount:N2} TL";
                DetailSettlementPlatform.Text = $"Platform: {selected.Source}";

                DetailBankDesc.Text = selected.BankDescription;
                DetailBankAmount.Text = $"Tutar: {selected.BankAmount:N2} TL";
                DetailBankDate.Text = $"Tarih: {selected.Date:dd.MM.yyyy}";

                DetailSkorText.Text = $"{selected.Confidence}%";

                if (selected.Confidence >= 95)
                {
                    DetailSkorBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xD1, 0xFA, 0xE5));
                    DetailSkorText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x06, 0x5F, 0x46));
                }
                else if (selected.Confidence >= 70)
                {
                    DetailSkorBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFE, 0xF3, 0xC7));
                    DetailSkorText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x92, 0x40, 0x0E));
                }
                else
                {
                    DetailSkorBorder.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFE, 0xE2, 0xE2));
                    DetailSkorText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x99, 0x1B, 0x1B));
                }
            }
            else
            {
                ClearDetailPanel();
            }
        }

        private void ClearDetailPanel()
        {
            DetailSettlementRef.Text = "-";
            DetailSettlementAmount.Text = "-";
            DetailSettlementPlatform.Text = "-";
            DetailBankDesc.Text = "-";
            DetailBankAmount.Text = "-";
            DetailBankDate.Text = "-";
            DetailSkorText.Text = "-";
        }

        private void PlatformFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Intentional: reload mock data — real filtering will query backend via MediatR
            LoadMockData();
        }
    }

    internal sealed class ReconciliationItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public DateTime Date { get; set; }
        public string SettlementRef { get; set; } = "";
        public string BankDescription { get; set; } = "";
        public decimal BankAmount { get; set; }
        public string OrderNumber { get; set; } = "";
        public decimal OrderAmount { get; set; }
        public int Confidence { get; set; }
        public string Status { get; set; } = "";
        public string Source { get; set; } = "";

        public decimal AmountDifference => Math.Abs(BankAmount - OrderAmount);

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
