using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesTechStok.Desktop.Views.Finance
{
    public partial class FixedExpenseView : UserControl
    {
        private static readonly SolidColorBrush _activeBrush;
        private static readonly SolidColorBrush _inactiveBrush;

        static FixedExpenseView()
        {
            _activeBrush = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
            _activeBrush.Freeze();
            _inactiveBrush = new SolidColorBrush(Color.FromRgb(0x6C, 0x75, 0x7D));
            _inactiveBrush.Freeze();
        }

        private readonly ObservableCollection<FixedExpenseEntry> _entries = new();

        public FixedExpenseView()
        {
            InitializeComponent();
            FixedExpenseGrid.ItemsSource = _entries;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _entries.Clear();

            _entries.Add(new FixedExpenseEntry { Name = "Depo Kirasi - Esenyurt", Category = "Kira", MonthlyAmount = 18500.00m, PaymentDay = 1, IsActive = true, StartDate = new DateTime(2024, 6, 1), Note = "2+1 yillik sozlesme" });
            _entries.Add(new FixedExpenseEntry { Name = "Ofis Kirasi - Levent", Category = "Kira", MonthlyAmount = 12000.00m, PaymentDay = 1, IsActive = true, StartDate = new DateTime(2025, 1, 1), Note = "1+1 yillik" });
            _entries.Add(new FixedExpenseEntry { Name = "Internet + Telefon (Turk Telekom)", Category = "Iletisim", MonthlyAmount = 1850.00m, PaymentDay = 15, IsActive = true, StartDate = new DateTime(2024, 3, 1), Note = "Fiber 100Mbps + 5 hat" });
            _entries.Add(new FixedExpenseEntry { Name = "Elektrik (AYEDAS)", Category = "Fatura", MonthlyAmount = 3200.00m, PaymentDay = 20, IsActive = true, StartDate = new DateTime(2024, 6, 1), Note = "Depo + ofis" });
            _entries.Add(new FixedExpenseEntry { Name = "Dogalgaz (IGDAS)", Category = "Fatura", MonthlyAmount = 1100.00m, PaymentDay = 18, IsActive = true, StartDate = new DateTime(2024, 6, 1), Note = "Kis donemi yuksek" });
            _entries.Add(new FixedExpenseEntry { Name = "Su (ISKI)", Category = "Fatura", MonthlyAmount = 450.00m, PaymentDay = 22, IsActive = true, StartDate = new DateTime(2024, 6, 1) });
            _entries.Add(new FixedExpenseEntry { Name = "Muhasebe Danismanligi", Category = "Hizmet", MonthlyAmount = 6500.00m, PaymentDay = 5, IsActive = true, StartDate = new DateTime(2025, 1, 1), Note = "SMMM aylik hizmet" });
            _entries.Add(new FixedExpenseEntry { Name = "Guvenlik Sistemi (Pronet)", Category = "Guvenlik", MonthlyAmount = 980.00m, PaymentDay = 10, IsActive = true, StartDate = new DateTime(2024, 9, 1), Note = "Depo alarm + kamera" });
            _entries.Add(new FixedExpenseEntry { Name = "Arac Kiralama (2 arac)", Category = "Ulasim", MonthlyAmount = 14000.00m, PaymentDay = 1, IsActive = true, StartDate = new DateTime(2025, 3, 1), Note = "Operasyon + yonetim" });
            _entries.Add(new FixedExpenseEntry { Name = "Yazilim Lisanslari", Category = "IT", MonthlyAmount = 2400.00m, PaymentDay = 1, IsActive = true, StartDate = new DateTime(2025, 1, 1), Note = "Office 365 + domain + hosting" });
            _entries.Add(new FixedExpenseEntry { Name = "Eski Depo Kirasi", Category = "Kira", MonthlyAmount = 8000.00m, PaymentDay = 1, IsActive = false, StartDate = new DateTime(2023, 1, 1), Note = "Sozlesme bitti" });

            foreach (var entry in _entries)
            {
                entry.ActiveColor = entry.IsActive ? _activeBrush : _inactiveBrush;
                entry.ActiveLabel = entry.IsActive ? "Aktif" : "Pasif";
            }

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var activeEntries = _entries.Where(e => e.IsActive).ToList();
            var monthlyTotal = activeEntries.Sum(e => e.MonthlyAmount);
            var annualTotal = monthlyTotal * 12m;
            var activeCount = activeEntries.Count;
            var inactiveCount = _entries.Count(e => !e.IsActive);

            MonthlyTotalText.Text = $"{monthlyTotal:N2} TL";
            AnnualTotalText.Text = $"{annualTotal:N2} TL";
            ActiveCountText.Text = activeCount.ToString();
            InactiveCountText.Text = inactiveCount.ToString();
        }

        private void AddFixedExpense_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Sabit gider ekleme formu — backend hazir olunca aktif edilecek.", "MesTech Sabit Gider", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class FixedExpenseEntry
    {
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal MonthlyAmount { get; set; }
        public int PaymentDay { get; set; }
        public bool IsActive { get; set; }
        public DateTime StartDate { get; set; }
        public string Note { get; set; } = "";
        public string ActiveLabel { get; set; } = "";
        public SolidColorBrush ActiveColor { get; set; } = Brushes.Gray;
    }
}
