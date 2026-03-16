using System;
using System.Collections.Generic;
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
        private static readonly SolidColorBrush _trendUpBrush;
        private static readonly SolidColorBrush _trendDownBrush;

        static KarZararView()
        {
            _profitBrush = new SolidColorBrush(Color.FromRgb(0x28, 0xA7, 0x45));
            _profitBrush.Freeze();
            _lossBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0x35, 0x45));
            _lossBrush.Freeze();
            _trendUpBrush = new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81));
            _trendUpBrush.Freeze();
            _trendDownBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
            _trendDownBrush.Freeze();
        }

        private readonly ObservableCollection<ProfitReportRow> _profitRows = new();
        private readonly ObservableCollection<PlatformComparisonRow> _platformComparison = new();
        private readonly ObservableCollection<CostBreakdownItem> _costBreakdown = new();

        public KarZararView()
        {
            InitializeComponent();
            ProfitGrid.ItemsSource = _profitRows;
            PlatformComparisonGrid.ItemsSource = _platformComparison;
            CostBreakdownPanel.ItemsSource = _costBreakdown;
            LoadMockData();
        }

        private void LoadMockData()
        {
            _profitRows.Clear();

            // Mart 2026 data
            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "Trendyol", Revenue = 45200.50m, Cost = 22600.25m, Commission = 6780.08m, Shipping = 2260.00m, Tax = 1808.02m, NetProfit = 11752.15m, ProfitMargin = 26.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "Hepsiburada", Revenue = 18750.00m, Cost = 9375.00m, Commission = 3187.50m, Shipping = 937.50m, Tax = 750.00m, NetProfit = 4500.00m, ProfitMargin = 24.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "N11", Revenue = 12400.00m, Cost = 6200.00m, Commission = 1488.00m, Shipping = 620.00m, Tax = 496.00m, NetProfit = 3596.00m, ProfitMargin = 29.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "Ciceksepeti", Revenue = 9840.75m, Cost = 4920.38m, Commission = 1968.15m, Shipping = 492.04m, Tax = 393.63m, NetProfit = 2066.55m, ProfitMargin = 21.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "Pazarama", Revenue = 5600.00m, Cost = 2800.00m, Commission = 560.00m, Shipping = 280.00m, Tax = 224.00m, NetProfit = 1736.00m, ProfitMargin = 31.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Mart 2026", Platform = "OpenCart", Revenue = 8200.00m, Cost = 4100.00m, Commission = 0.00m, Shipping = 410.00m, Tax = 328.00m, NetProfit = 3362.00m, ProfitMargin = 41.0 });

            // Subat 2026 data
            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "Trendyol", Revenue = 42100.00m, Cost = 21050.00m, Commission = 6315.00m, Shipping = 2105.00m, Tax = 1684.00m, NetProfit = 10946.00m, ProfitMargin = 26.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "Hepsiburada", Revenue = 16200.00m, Cost = 8100.00m, Commission = 2754.00m, Shipping = 810.00m, Tax = 648.00m, NetProfit = 3888.00m, ProfitMargin = 24.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "N11", Revenue = 10800.00m, Cost = 5400.00m, Commission = 1296.00m, Shipping = 540.00m, Tax = 432.00m, NetProfit = 3132.00m, ProfitMargin = 29.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "Ciceksepeti", Revenue = 8500.00m, Cost = 4250.00m, Commission = 1700.00m, Shipping = 425.00m, Tax = 340.00m, NetProfit = 1785.00m, ProfitMargin = 21.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "Pazarama", Revenue = 4800.00m, Cost = 2400.00m, Commission = 480.00m, Shipping = 240.00m, Tax = 192.00m, NetProfit = 1488.00m, ProfitMargin = 31.0 });
            _profitRows.Add(new ProfitReportRow { Period = "Subat 2026", Platform = "OpenCart", Revenue = 7100.00m, Cost = 3550.00m, Commission = 0.00m, Shipping = 355.00m, Tax = 284.00m, NetProfit = 2911.00m, ProfitMargin = 41.0 });

            UpdateKpis();
            BuildCostBreakdown();
            BuildPlatformComparison();
        }

        private void UpdateKpis()
        {
            var currentPeriod = _profitRows.Where(r => r.Period == "Mart 2026").ToList();
            var prevPeriod = _profitRows.Where(r => r.Period == "Subat 2026").ToList();

            var totalRevenue = currentPeriod.Sum(r => r.Revenue);
            var totalCost = currentPeriod.Sum(r => r.Cost);
            var totalCommission = currentPeriod.Sum(r => r.Commission);
            var totalShipping = currentPeriod.Sum(r => r.Shipping);
            var totalNetProfit = currentPeriod.Sum(r => r.NetProfit);
            var grossMargin = totalRevenue > 0 ? (double)((totalRevenue - totalCost) / totalRevenue * 100m) : 0;

            var prevRevenue = prevPeriod.Sum(r => r.Revenue);
            var prevCost = prevPeriod.Sum(r => r.Cost);
            var prevNetProfit = prevPeriod.Sum(r => r.NetProfit);
            var prevGrossMargin = prevRevenue > 0 ? (double)((prevRevenue - prevCost) / prevRevenue * 100m) : 0;

            RevenueText.Text = $"{totalRevenue:N2} TL";
            CostText.Text = $"{totalCost:N2} TL";
            CommissionText.Text = $"{totalCommission:N2} TL";
            ShippingText.Text = $"{totalShipping:N2} TL";
            GrossMarginText.Text = $"{grossMargin:F1}%";
            NetProfitText.Text = $"{totalNetProfit:N2} TL";
            NetProfitText.Foreground = totalNetProfit >= 0 ? _profitBrush : _lossBrush;

            SetTrendIndicator(RevenueTrendText, totalRevenue, prevRevenue);
            SetTrendIndicator(CostTrendText, totalCost, prevCost, invertColor: true);
            SetTrendIndicator(GrossMarginTrendText, (decimal)grossMargin, (decimal)prevGrossMargin);
            SetTrendIndicator(NetProfitTrendText, totalNetProfit, prevNetProfit);
        }

        private void SetTrendIndicator(System.Windows.Controls.TextBlock textBlock, decimal current, decimal previous, bool invertColor = false)
        {
            if (previous == 0)
            {
                textBlock.Text = "";
                return;
            }
            var changePercent = (double)((current - previous) / previous * 100m);
            var isUp = changePercent >= 0;
            var arrow = isUp ? "\u2191" : "\u2193";
            textBlock.Text = $"{arrow} {Math.Abs(changePercent):F1}%";

            var isPositive = invertColor ? !isUp : isUp;
            textBlock.Foreground = isPositive ? _trendUpBrush : _trendDownBrush;
        }

        private void BuildCostBreakdown()
        {
            _costBreakdown.Clear();
            var currentPeriod = _profitRows.Where(r => r.Period == "Mart 2026").ToList();

            foreach (var row in currentPeriod)
            {
                var total = row.Revenue;
                if (total <= 0) continue;

                var cogsRatio = (double)(row.Cost / total);
                var commRatio = (double)(row.Commission / total);
                var cargoRatio = (double)(row.Shipping / total);
                var taxRatio = (double)(row.Tax / total);
                var netRatio = Math.Max(0, 1.0 - cogsRatio - commRatio - cargoRatio - taxRatio);

                _costBreakdown.Add(new CostBreakdownItem
                {
                    Platform = row.Platform,
                    CogsWidth = new GridLength(cogsRatio, GridUnitType.Star),
                    CommissionWidth = new GridLength(commRatio, GridUnitType.Star),
                    CargoWidth = new GridLength(cargoRatio, GridUnitType.Star),
                    TaxWidth = new GridLength(taxRatio, GridUnitType.Star),
                    NetWidth = new GridLength(netRatio, GridUnitType.Star),
                    TotalCostText = $"{(row.Cost + row.Commission + row.Shipping + row.Tax):N0} TL",
                    MarginText = $"{row.ProfitMargin:F0}%",
                    MarginColor = row.ProfitMargin >= 30 ? _profitBrush : (row.ProfitMargin >= 20 ? new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)) : _lossBrush),
                    CogsTooltip = $"COGS: {row.Cost:N2} TL ({cogsRatio:P0})",
                    CommissionTooltip = $"Komisyon: {row.Commission:N2} TL ({commRatio:P0})",
                    CargoTooltip = $"Kargo: {row.Shipping:N2} TL ({cargoRatio:P0})",
                    TaxTooltip = $"Vergi: {row.Tax:N2} TL ({taxRatio:P0})",
                    NetTooltip = $"Net: {row.NetProfit:N2} TL ({netRatio:P0})"
                });
            }
        }

        private void BuildPlatformComparison()
        {
            _platformComparison.Clear();
            var currentPeriod = _profitRows.Where(r => r.Period == "Mart 2026").ToList();
            var prevPeriod = _profitRows.Where(r => r.Period == "Subat 2026").ToList();

            foreach (var row in currentPeriod)
            {
                var prev = prevPeriod.FirstOrDefault(p => p.Platform == row.Platform);
                var grossMargin = row.Revenue > 0 ? (double)((row.Revenue - row.Cost) / row.Revenue * 100m) : 0;
                var trendText = "";
                if (prev != null && prev.NetProfit != 0)
                {
                    var change = (double)((row.NetProfit - prev.NetProfit) / prev.NetProfit * 100m);
                    var arrow = change >= 0 ? "\u2191" : "\u2193";
                    trendText = $"{arrow} {Math.Abs(change):F1}%";
                }

                _platformComparison.Add(new PlatformComparisonRow
                {
                    Platform = row.Platform,
                    Revenue = row.Revenue,
                    Cost = row.Cost,
                    Commission = row.Commission,
                    Shipping = row.Shipping,
                    Tax = row.Tax,
                    NetProfit = row.NetProfit,
                    GrossMarginText = $"{grossMargin:F1}%",
                    ProfitMargin = row.ProfitMargin,
                    TrendText = trendText
                });
            }
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

        private void ShowLoading() { LoadingOverlay.Visibility = Visibility.Visible; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowEmpty() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Visible; ErrorState.Visibility = Visibility.Collapsed; }
        private void ShowError(string msg = "Bir hata olustu") { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Visible; ErrorMessage.Text = msg; }
        private void ShowContent() { LoadingOverlay.Visibility = Visibility.Collapsed; EmptyState.Visibility = Visibility.Collapsed; ErrorState.Visibility = Visibility.Collapsed; }
        private void RetryButton_Click(object sender, RoutedEventArgs e) { ShowContent(); LoadMockData(); }
    }

    internal sealed class ProfitReportRow
    {
        public string Period { get; set; } = "";
        public string Platform { get; set; } = "";
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Commission { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal NetProfit { get; set; }
        public double ProfitMargin { get; set; }
        public string GrossMarginText => Revenue > 0 ? $"{(double)((Revenue - Cost) / Revenue * 100m):F1}%" : "0.0%";
    }

    internal sealed class PlatformComparisonRow
    {
        public string Platform { get; set; } = "";
        public decimal Revenue { get; set; }
        public decimal Cost { get; set; }
        public decimal Commission { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal NetProfit { get; set; }
        public string GrossMarginText { get; set; } = "";
        public double ProfitMargin { get; set; }
        public string TrendText { get; set; } = "";
    }

    internal sealed class CostBreakdownItem
    {
        public string Platform { get; set; } = "";
        public GridLength CogsWidth { get; set; }
        public GridLength CommissionWidth { get; set; }
        public GridLength CargoWidth { get; set; }
        public GridLength TaxWidth { get; set; }
        public GridLength NetWidth { get; set; }
        public string TotalCostText { get; set; } = "";
        public string MarginText { get; set; } = "";
        public SolidColorBrush MarginColor { get; set; } = Brushes.Gray;
        public string CogsTooltip { get; set; } = "";
        public string CommissionTooltip { get; set; } = "";
        public string CargoTooltip { get; set; } = "";
        public string TaxTooltip { get; set; } = "";
        public string NetTooltip { get; set; } = "";
    }
}
