using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MesTechStok.Desktop.Views.Accounting
{
    public partial class KarZararView : UserControl
    {
        private static readonly SolidColorBrush _profitBrush;
        private static readonly SolidColorBrush _lossBrush;

        static KarZararView()
        {
            _profitBrush = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
            _profitBrush.Freeze();
            _lossBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45));
            _lossBrush.Freeze();
        }

        private readonly ObservableCollection<ProfitReportRow> _profitRows = new();

        public KarZararView()
        {
            InitializeComponent();
            ProfitGrid.ItemsSource = _profitRows;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _profitRows.Clear();

            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "Trendyol", Revenue = 45200.50m, Cost = 22600.25m, Commission = 6780.08m, Shipping = 2260.00m, NetProfit = 13560.17m, ProfitMargin = 30.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "Hepsiburada", Revenue = 18750.00m, Cost = 9375.00m, Commission = 3187.50m, Shipping = 937.50m, NetProfit = 5250.00m, ProfitMargin = 28.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "N11", Revenue = 12400.00m, Cost = 6200.00m, Commission = 1488.00m, Shipping = 620.00m, NetProfit = 4092.00m, ProfitMargin = 33.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "Ciceksepeti", Revenue = 9840.75m, Cost = 4920.38m, Commission = 1968.15m, Shipping = 492.04m, NetProfit = 2460.18m, ProfitMargin = 25.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "Pazarama", Revenue = 5600.00m, Cost = 2800.00m, Commission = 560.00m, Shipping = 280.00m, NetProfit = 1960.00m, ProfitMargin = 35.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "OpenCart", Revenue = 8200.00m, Cost = 4100.00m, Commission = 0.00m, Shipping = 410.00m, NetProfit = 3690.00m, ProfitMargin = 45.0 });

            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "Trendyol", Revenue = 42100.00m, Cost = 21050.00m, Commission = 6315.00m, Shipping = 2105.00m, NetProfit = 12630.00m, ProfitMargin = 30.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "Hepsiburada", Revenue = 16200.00m, Cost = 8100.00m, Commission = 2754.00m, Shipping = 810.00m, NetProfit = 4536.00m, ProfitMargin = 28.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "N11", Revenue = 10800.00m, Cost = 5400.00m, Commission = 1296.00m, Shipping = 540.00m, NetProfit = 3564.00m, ProfitMargin = 33.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "Ciceksepeti", Revenue = 8500.00m, Cost = 4250.00m, Commission = 1700.00m, Shipping = 425.00m, NetProfit = 2125.00m, ProfitMargin = 25.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "Pazarama", Revenue = 4800.00m, Cost = 2400.00m, Commission = 480.00m, Shipping = 240.00m, NetProfit = 1680.00m, ProfitMargin = 35.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "OpenCart", Revenue = 7100.00m, Cost = 3550.00m, Commission = 0.00m, Shipping = 355.00m, NetProfit = 3195.00m, ProfitMargin = 45.0 });

            UpdateKpis();
        }

        private void UpdateKpis()
        {
            var totalRevenue = _profitRows.Sum(r => r.Revenue);
            var totalCost = _profitRows.Sum(r => r.Cost);
            var totalCommission = _profitRows.Sum(r => r.Commission);
            var totalNetProfit = _profitRows.Sum(r => r.NetProfit);

            RevenueText.Text = $"{totalRevenue:N2} TL";
            CostText.Text = $"{totalCost:N2} TL";
            CommissionText.Text = $"{totalCommission:N2} TL";
            NetProfitText.Text = $"{totalNetProfit:N2} TL";
            NetProfitText.Foreground = totalNetProfit >= 0 ? _profitBrush : _lossBrush;
        }

        private void PlatformFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Intentional: reload mock data — real filtering will query backend
            LoadMockData();
        }

        private void PeriodFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Intentional: reload mock data — real period grouping will be handled by backend
            LoadMockData();
        }
    }

    internal sealed class ProfitReportRow
    {
        public string Period { get; set; } = "";
        public string Platform { get; set; } = "";
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Commission { get; set; }
        public decimal Shipping { get; set; }
        public decimal NetProfit { get; set; }
        public double ProfitMargin { get; set; }
    }
}
