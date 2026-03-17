using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesTechStok.Desktop.Views.Finance
{
    public partial class PenaltyView : UserControl
    {
        private static readonly SolidColorBrush _paidBrush;
        private static readonly SolidColorBrush _pendingBrush;
        private static readonly SolidColorBrush _overdueBrush;
        private static readonly SolidColorBrush _contestedBrush;

        static PenaltyView()
        {
            _paidBrush = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
            _paidBrush.Freeze();
            _pendingBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07));
            _pendingBrush.Freeze();
            _overdueBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45));
            _overdueBrush.Freeze();
            _contestedBrush = new SolidColorBrush(Color.FromRgb(0x28, 0x55, 0xAC));
            _contestedBrush.Freeze();
        }

        private readonly ObservableCollection<PenaltyEntry> _entries = new();

        public PenaltyView()
        {
            InitializeComponent();
            PenaltyGrid.ItemsSource = _entries;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _entries.Clear();
            var today = DateTime.Today;

            _entries.Add(new PenaltyEntry { Source = "Trendyol", Description = "Gec kargo teslimi — siparis #TY-2026-48721", Amount = 125.00m, PenaltyDate = today.AddDays(-45), DueDate = today.AddDays(-15), Status = "Gecikti", Reference = "TY-PEN-0048" });
            _entries.Add(new PenaltyEntry { Source = "Trendyol", Description = "Urun iade islem gecikmesi", Amount = 75.50m, PenaltyDate = today.AddDays(-30), DueDate = today.AddDays(-5), Status = "Gecikti", Reference = "TY-PEN-0051" });
            _entries.Add(new PenaltyEntry { Source = "Hepsiburada", Description = "Stok bilgisi guncelleme gecikmesi", Amount = 200.00m, PenaltyDate = today.AddDays(-20), DueDate = today.AddDays(5), Status = "Bekleyen", Reference = "HB-PEN-0012" });
            _entries.Add(new PenaltyEntry { Source = "N11", Description = "Iptal orani asimi (%8 > %5)", Amount = 350.00m, PenaltyDate = today.AddDays(-15), DueDate = today.AddDays(10), Status = "Bekleyen", Reference = "N11-PEN-0008" });
            _entries.Add(new PenaltyEntry { Source = "Ciceksepeti", Description = "Paketleme hatasi — musteri sikayeti", Amount = 150.00m, PenaltyDate = today.AddDays(-10), DueDate = today.AddDays(15), Status = "Itiraz", Reference = "CS-PEN-0003" });
            _entries.Add(new PenaltyEntry { Source = "Vergi Dairesi", Description = "KDV beyanname gecikme cezasi", Amount = 890.00m, PenaltyDate = today.AddDays(-60), DueDate = today.AddDays(-30), Status = "Odendi", Reference = "VD-2026-001" });
            _entries.Add(new PenaltyEntry { Source = "SGK", Description = "Prim bildirge gecikme cezasi", Amount = 1250.00m, PenaltyDate = today.AddDays(-90), DueDate = today.AddDays(-60), Status = "Odendi", Reference = "SGK-2026-002" });
            _entries.Add(new PenaltyEntry { Source = "Trendyol", Description = "Sahte urun tespiti — haksiz", Amount = 500.00m, PenaltyDate = today.AddDays(-40), DueDate = today.AddDays(-10), Status = "Itiraz", Reference = "TY-PEN-0045" });
            _entries.Add(new PenaltyEntry { Source = "Hepsiburada", Description = "Kargo hasari — mudahil degil", Amount = 180.00m, PenaltyDate = today.AddDays(-25), DueDate = today.AddDays(-5), Status = "Odendi", Reference = "HB-PEN-0010" });
            _entries.Add(new PenaltyEntry { Source = "Trendyol", Description = "Geciken siparis — kargo firmasi kaynakli", Amount = 95.00m, PenaltyDate = today.AddDays(-8), DueDate = today.AddDays(20), Status = "Itiraz", Reference = "TY-PEN-0055" });

            // Renk kodlama
            foreach (var entry in _entries)
            {
                switch (entry.Status)
                {
                    case "Odendi":
                        entry.StatusColor = _paidBrush;
                        entry.StatusLabel = "Odendi";
                        break;
                    case "Bekleyen":
                        entry.StatusColor = _pendingBrush;
                        entry.StatusLabel = "Bekleyen";
                        break;
                    case "Gecikti":
                        entry.StatusColor = _overdueBrush;
                        entry.StatusLabel = "Gecikti";
                        break;
                    case "Itiraz":
                        entry.StatusColor = _contestedBrush;
                        entry.StatusLabel = "Itiraz";
                        break;
                    default:
                        entry.StatusColor = _pendingBrush;
                        entry.StatusLabel = entry.Status;
                        break;
                }
            }

            UpdateKpis();
            UpdateOverdueAlert();
        }

        private void UpdateKpis()
        {
            var totalPenalty = _entries.Sum(e => e.Amount);
            var paidPenalty = _entries.Where(e => e.Status == "Odendi").Sum(e => e.Amount);
            var pendingPenalty = _entries.Where(e => e.Status != "Odendi").Sum(e => e.Amount);
            var overdueCount = _entries.Count(e => e.Status == "Gecikti");

            TotalPenaltyText.Text = $"{totalPenalty:N2} TL";
            PaidPenaltyText.Text = $"{paidPenalty:N2} TL";
            PendingPenaltyText.Text = $"{pendingPenalty:N2} TL";
            OverdueCountText.Text = overdueCount.ToString();
        }

        private void UpdateOverdueAlert()
        {
            var overdueEntries = _entries.Where(e => e.Status == "Gecikti").ToList();
            if (overdueEntries.Count > 0)
            {
                var totalOverdue = overdueEntries.Sum(e => e.Amount);
                OverdueAlertText.Inlines.Clear();
                OverdueAlertText.Inlines.Add(new System.Windows.Documents.Run($"Dikkat: {overdueEntries.Count} adet vadesi gecmis ceza kaydi — toplam {totalOverdue:N2} TL") { FontWeight = FontWeights.SemiBold });
                OverdueAlert.Visibility = Visibility.Visible;
            }
            else
            {
                OverdueAlert.Visibility = Visibility.Collapsed;
            }
        }

        private void SourceFilter_Changed(object sender, SelectionChangedEventArgs e)
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

    internal sealed class PenaltyEntry
    {
        public string Source { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public DateTime PenaltyDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Status { get; set; } = "";
        public string StatusLabel { get; set; } = "";
        public SolidColorBrush StatusColor { get; set; } = Brushes.Gray;
        public string Reference { get; set; } = "";
    }
}
