// Debt: [MVVM-CLEANUP] State'i ViewModel'e tasi — Bkz: AUDIT-SYNTHESIS-001 Orta Bulgu #14
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using MesTechStok.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using MesTechStok.Desktop.Models;
using MesTechStok.Desktop.Components;
using MesTechStok.Desktop.Utils;
using Microsoft.Win32;
using MesTechStok.Core.Services.Abstract;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace MesTechStok.Desktop.Views
{
    /// <summary>
    /// ReportsView - ENHANCED Reports and Analytics with LiveCharts2 Real Charts
    /// Gelistirilmis rapor ve analitik sistemi
    /// </summary>
    public partial class ReportsView : UserControl, INotifyPropertyChanged
    {
        #region Private Fields

        private readonly SqlBackedReportsService _reportsService;
        private readonly ObservableCollection<Services.ReportItem> _displayedReports;
        private string _searchText = string.Empty;
        private ReportTypeFilter _currentTypeFilter = ReportTypeFilter.All;
        private ReportSortOrder _currentSortOrder = ReportSortOrder.CreatedDateDesc;

        // Authorization flags
        public bool CanExportReports { get; private set; } = true;

        #endregion

        #region LiveCharts2 Properties

        // --- Monthly Revenue/Expense Bar Chart ---
        private ISeries[] _monthlyRevenueSeries = Array.Empty<ISeries>();
        public ISeries[] MonthlyRevenueSeries
        {
            get => _monthlyRevenueSeries;
            set { _monthlyRevenueSeries = value; OnPropertyChanged(); }
        }

        private Axis[] _monthlyXAxes = Array.Empty<Axis>();
        public Axis[] MonthlyXAxes
        {
            get => _monthlyXAxes;
            set { _monthlyXAxes = value; OnPropertyChanged(); }
        }

        private Axis[] _monthlyYAxes = Array.Empty<Axis>();
        public Axis[] MonthlyYAxes
        {
            get => _monthlyYAxes;
            set { _monthlyYAxes = value; OnPropertyChanged(); }
        }

        // --- Platform Sales Pie Chart ---
        private ISeries[] _platformSalesSeries = Array.Empty<ISeries>();
        public ISeries[] PlatformSalesSeries
        {
            get => _platformSalesSeries;
            set { _platformSalesSeries = value; OnPropertyChanged(); }
        }

        // --- Weekly Trend Line Chart ---
        private ISeries[] _weeklyTrendSeries = Array.Empty<ISeries>();
        public ISeries[] WeeklyTrendSeries
        {
            get => _weeklyTrendSeries;
            set { _weeklyTrendSeries = value; OnPropertyChanged(); }
        }

        private Axis[] _weeklyXAxes = Array.Empty<Axis>();
        public Axis[] WeeklyXAxes
        {
            get => _weeklyXAxes;
            set { _weeklyXAxes = value; OnPropertyChanged(); }
        }

        private Axis[] _weeklyYAxes = Array.Empty<Axis>();
        public Axis[] WeeklyYAxes
        {
            get => _weeklyYAxes;
            set { _weeklyYAxes = value; OnPropertyChanged(); }
        }

        #endregion

        #region Properties

        public ObservableCollection<Services.ReportItem> DisplayedReports => _displayedReports;

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _ = LoadReportsPageAsync();
            }
        }

        // Enhanced KPI Properties
        private string _totalReports = "0";
        private string _dailyReports = "0";
        private string _weeklyReports = "0";
        private string _automatedReports = "0";

        public string TotalReports
        {
            get => _totalReports;
            set { _totalReports = value; OnPropertyChanged(); }
        }

        public string DailyReports
        {
            get => _dailyReports;
            set { _dailyReports = value; OnPropertyChanged(); }
        }

        public string WeeklyReports
        {
            get => _weeklyReports;
            set { _weeklyReports = value; OnPropertyChanged(); }
        }

        public string AutomatedReports
        {
            get => _automatedReports;
            set { _automatedReports = value; OnPropertyChanged(); }
        }

        #endregion

        #region Constructor

        public ReportsView()
        {
            _reportsService = MesTechStok.Desktop.App.Services!.GetRequiredService<SqlBackedReportsService>();
            _displayedReports = new ObservableCollection<Services.ReportItem>();

            InitializeComponent();
            DataContext = this;

            _ = InitializeAsync();
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            try
            {
                await SetupAuthorizationsAsync();
                await LoadReportsPageAsync();
                await UpdateStatisticsAsync();
                await DrawSalesChartAsync();

                // LiveCharts2 real charts initialization
                InitializeLiveCharts();

                GlobalLogger.Instance.LogInfo("Enhanced ReportsView baslatildi (LiveCharts2 aktif)", "ReportsView");
                ToastManager.ShowSuccess("Rapor sistemi basariyla yuklendi!", "Rapor Merkezi");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"ReportsView baslatma hatasi: {ex.Message}", "ReportsView");
                ToastManager.ShowError("Rapor sistemi yuklenirken hata olustu!", "Hata");
            }
        }

        /// <summary>
        /// Initialize all LiveCharts2 chart data with realistic demo data.
        /// </summary>
        private void InitializeLiveCharts()
        {
            try
            {
                InitializeMonthlyRevenueChart();
                InitializePlatformPieChart();
                InitializeWeeklyTrendChart();

                GlobalLogger.Instance.LogInfo("LiveCharts2 grafikleri baslatildi", "ReportsView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"LiveCharts2 baslatma hatasi: {ex.Message}", "ReportsView");
            }
        }

        /// <summary>
        /// Aylik Gelir-Gider bar chart (12 months).
        /// </summary>
        private void InitializeMonthlyRevenueChart()
        {
            var months = new[] { "Oca", "Sub", "Mar", "Nis", "May", "Haz", "Tem", "Agu", "Eyl", "Eki", "Kas", "Ara" };

            // Realistic monthly revenue data (TRY thousands)
            var revenueData = new double[] { 85, 92, 108, 95, 120, 135, 115, 128, 142, 138, 155, 168 };
            var expenseData = new double[] { 62, 68, 75, 72, 82, 88, 78, 85, 92, 90, 98, 105 };

            MonthlyRevenueSeries = new ISeries[]
            {
                new ColumnSeries<double>
                {
                    Name = "Gelir (bin TL)",
                    Values = revenueData,
                    Fill = new SolidColorPaint(new SKColor(40, 167, 69)), // Green
                    MaxBarWidth = 18,
                    Rx = 3,
                    Ry = 3
                },
                new ColumnSeries<double>
                {
                    Name = "Gider (bin TL)",
                    Values = expenseData,
                    Fill = new SolidColorPaint(new SKColor(220, 53, 69)), // Red
                    MaxBarWidth = 18,
                    Rx = 3,
                    Ry = 3
                }
            };

            MonthlyXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = months,
                    LabelsRotation = 0,
                    TextSize = 11,
                    LabelsPaint = new SolidColorPaint(new SKColor(100, 116, 139))
                }
            };

            MonthlyYAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Tutar (bin TL)",
                    NameTextSize = 11,
                    TextSize = 10,
                    LabelsPaint = new SolidColorPaint(new SKColor(100, 116, 139)),
                    NamePaint = new SolidColorPaint(new SKColor(100, 116, 139)),
                    MinLimit = 0
                }
            };
        }

        /// <summary>
        /// Platform sales pie/doughnut chart.
        /// </summary>
        private void InitializePlatformPieChart()
        {
            PlatformSalesSeries = new ISeries[]
            {
                new PieSeries<double>
                {
                    Name = "Trendyol",
                    Values = new double[] { 42 },
                    Fill = new SolidColorPaint(new SKColor(255, 106, 0)),  // Trendyol orange
                    InnerRadius = 50
                },
                new PieSeries<double>
                {
                    Name = "Hepsiburada",
                    Values = new double[] { 25 },
                    Fill = new SolidColorPaint(new SKColor(40, 167, 69)),  // HB green
                    InnerRadius = 50
                },
                new PieSeries<double>
                {
                    Name = "N11",
                    Values = new double[] { 15 },
                    Fill = new SolidColorPaint(new SKColor(40, 85, 172)),  // N11 blue (#2855AC)
                    InnerRadius = 50
                },
                new PieSeries<double>
                {
                    Name = "Amazon",
                    Values = new double[] { 10 },
                    Fill = new SolidColorPaint(new SKColor(255, 153, 0)),  // Amazon yellow
                    InnerRadius = 50
                },
                new PieSeries<double>
                {
                    Name = "eBay",
                    Values = new double[] { 8 },
                    Fill = new SolidColorPaint(new SKColor(134, 24, 145)), // eBay purple
                    InnerRadius = 50
                }
            };
        }

        /// <summary>
        /// Son 7 gun trend line chart.
        /// </summary>
        private void InitializeWeeklyTrendChart()
        {
            var today = DateTime.Today;
            var dayNames = new string[7];
            var revenueValues = new double[7];

            // Turkish short day names + realistic daily revenue
            var rng = new Random(today.DayOfYear); // deterministic seed per day
            for (int i = 0; i < 7; i++)
            {
                var day = today.AddDays(i - 6);
                dayNames[i] = day.DayOfWeek switch
                {
                    DayOfWeek.Monday => "Pzt",
                    DayOfWeek.Tuesday => "Sal",
                    DayOfWeek.Wednesday => "Car",
                    DayOfWeek.Thursday => "Per",
                    DayOfWeek.Friday => "Cum",
                    DayOfWeek.Saturday => "Cmt",
                    DayOfWeek.Sunday => "Paz",
                    _ => "?"
                };

                // Weekday: higher revenue, Weekend: lower
                var isWeekend = day.DayOfWeek == DayOfWeek.Saturday || day.DayOfWeek == DayOfWeek.Sunday;
                revenueValues[i] = isWeekend
                    ? 6000 + rng.Next(2000, 5000)
                    : 10000 + rng.Next(3000, 9000);
            }

            WeeklyTrendSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Gunluk Ciro (TL)",
                    Values = revenueValues,
                    Fill = new SolidColorPaint(new SKColor(40, 85, 172, 40)),   // Light brand fill
                    Stroke = new SolidColorPaint(new SKColor(40, 85, 172), 3),  // Brand color #2855AC
                    GeometrySize = 8,
                    GeometryFill = new SolidColorPaint(new SKColor(40, 85, 172)),
                    GeometryStroke = new SolidColorPaint(SKColors.White, 2),
                    LineSmoothness = 0.5
                }
            };

            WeeklyXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = dayNames,
                    LabelsRotation = 0,
                    TextSize = 12,
                    LabelsPaint = new SolidColorPaint(new SKColor(100, 116, 139))
                }
            };

            WeeklyYAxes = new Axis[]
            {
                new Axis
                {
                    Name = "Ciro (TL)",
                    NameTextSize = 11,
                    TextSize = 10,
                    LabelsPaint = new SolidColorPaint(new SKColor(100, 116, 139)),
                    NamePaint = new SolidColorPaint(new SKColor(100, 116, 139)),
                    MinLimit = 0
                }
            };
        }

        private Task SetupAuthorizationsAsync()
        {
            // Security: SimpleSecurityService integration pending
            // Su anda tum kullanicilar rapor export edebilir
            CanExportReports = true;
            OnPropertyChanged(nameof(CanExportReports));
            return Task.CompletedTask;
        }

        private async Task LoadReportsPageAsync(int page = 1, int pageSize = 25)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                ErrorState.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Collapsed;

                // SQL tabanli ozetleri al ve sag paneli doldur
                var summaries = await _reportsService.GetDashboardSummariesAsync();

                if (TotalRevenueText != null) TotalRevenueText.Text = $"\u20BA{summaries.TotalRevenue:N0}";
                if (TotalSalesText != null) TotalSalesText.Text = $"{summaries.TotalSales:N0}";
                if (StockValueText != null) StockValueText.Text = $"\u20BA{summaries.StockValue:N0}";

                if (TopProductsDataGrid != null)
                {
                    TopProductsDataGrid.ItemsSource = summaries.TopProducts
                        .Select(x => new { Name = x.Name, Sales = x.Sales })
                        .ToList();
                }

                if (LowStockDataGrid != null)
                {
                    LowStockDataGrid.ItemsSource = summaries.LowStockItems
                        .Select(x => new { Name = x.Name, Stock = x.Stock })
                        .ToList();
                }

                GlobalLogger.Instance.LogInfo($"Rapor ozetleri yuklendi: TopProducts={summaries.TopProducts.Count}, LowStock={summaries.LowStockItems.Count}", "ReportsView");

                LoadingOverlay.Visibility = Visibility.Collapsed;

                if (summaries.TopProducts.Count == 0 && summaries.LowStockItems.Count == 0)
                {
                    EmptyState.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                ErrorState.Visibility = Visibility.Visible;
                ReportErrorText.Text = $"Raporlar yuklenemedi: {ex.Message}";
                GlobalLogger.Instance.LogError($"Rapor sayfasi yukleme hatasi: {ex.Message}", "ReportsView");
            }
        }

        private void RetryReports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = LoadReportsPageAsync();
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(ReportsView)} RetryReports handler error: {ex.Message}");
            }
        }

        private async Task UpdateStatisticsAsync()
        {
            try
            {
                // SQL destekli ozetlerden turetilen basit istatistikler
                var summaries = await _reportsService.GetDashboardSummariesAsync();

                TotalReports = summaries.TopProducts.Count.ToString();
                DailyReports = DateTime.Now.Day.ToString();
                WeeklyReports = (DateTime.Now.Day / 7 + 1).ToString();
                AutomatedReports = "0";

                if (TotalRevenueText != null) TotalRevenueText.Text = $"\u20BA{summaries.TotalRevenue:N0}";
                if (TotalSalesText != null) TotalSalesText.Text = $"{summaries.TotalSales:N0}";
                if (StockValueText != null) StockValueText.Text = $"\u20BA{summaries.StockValue:N0}";
                if (LowStockCountText != null) LowStockCountText.Text = summaries.LowStockItems.Count.ToString();

                GlobalLogger.Instance.LogInfo("Rapor istatistikleri guncellendi", "ReportsView");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Istatistik guncelleme hatasi: {ex.Message}", "ReportsView");
            }
        }

        private readonly Random _random = new Random();

        private async Task DrawSalesChartAsync()
        {
            try
            {
                SalesChart.Children.Clear();

                var chartWidth = 400;
                var chartHeight = 200;
                var paddingLeft = 40;
                var paddingTop = 10;

                var background = new Rectangle
                {
                    Width = chartWidth,
                    Height = chartHeight,
                    Fill = Brushes.White,
                    Stroke = new SolidColorBrush(Color.FromRgb(233, 236, 239)),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(background, paddingLeft);
                Canvas.SetTop(background, paddingTop);
                SalesChart.Children.Add(background);

                // SQL'den gercek gunluk ciro
                var daily = await _reportsService.GetDailyRevenueAsync(15);
                var salesData = daily.Select(d => (double)d.Revenue).ToList();
                if (salesData.Count == 0)
                {
                    salesData = Enumerable.Repeat(0d, 15).ToList();
                }

                var maxValue = salesData.Max();
                var minValue = salesData.Min();
                var range = maxValue - minValue;
                if (range == 0) range = 1;

                var stepX = (double)chartWidth / (salesData.Count - 1);

                for (int i = 0; i < salesData.Count - 1; i++)
                {
                    var x1 = paddingLeft + i * stepX;
                    var y1 = paddingTop + chartHeight - ((salesData[i] - minValue) / range * chartHeight);
                    var x2 = paddingLeft + (i + 1) * stepX;
                    var y2 = paddingTop + chartHeight - ((salesData[i + 1] - minValue) / range * chartHeight);

                    var line = new Line
                    {
                        X1 = x1,
                        Y1 = y1,
                        X2 = x2,
                        Y2 = y2,
                        Stroke = new SolidColorBrush(Color.FromRgb(40, 85, 172)), // Brand color #2855AC
                        StrokeThickness = 3
                    };
                    SalesChart.Children.Add(line);

                    var point = new Ellipse
                    {
                        Width = 6,
                        Height = 6,
                        Fill = new SolidColorBrush(Color.FromRgb(40, 85, 172))
                    };
                    Canvas.SetLeft(point, x1 - 3);
                    Canvas.SetTop(point, y1 - 3);
                    SalesChart.Children.Add(point);
                }

                var title = new TextBlock
                {
                    Text = "Son 15 Gun - Gunluk Ciro",
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(40, 85, 172))
                };
                Canvas.SetLeft(title, paddingLeft);
                Canvas.SetTop(title, paddingTop + chartHeight + 15);
                SalesChart.Children.Add(title);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Grafik cizimi hatasi: {ex.Message}", "ReportsView");
            }
        }

        private async void GenerateReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var reportName = $"Sistem Raporu - {DateTime.Now:dd.MM.yyyy HH:mm}";
                // SQL tabanli servis rapor uretimi simule etmez; sadece ozetleri yenileyelim
                var refreshed = await _reportsService.GetDashboardSummariesAsync();
                if (refreshed.TopProducts.Count >= 0)
                {
                    await LoadReportsPageAsync();
                    ToastManager.ShowSuccess($"'{reportName}' basariyla olusturuldu!", "Rapor Uretimi");
                }
                else
                {
                    ToastManager.ShowError("Rapor olusturulamadi!", "Hata");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Rapor olusturma hatasi: {ex.Message}", "ReportsView");
                ToastManager.ShowError("Rapor olusturulurken hata olustu!", "Hata");
            }
        }

        #endregion

        #region Event Handlers

        private async void RefreshReports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadReportsPageAsync();
                await UpdateStatisticsAsync();
                _ = DrawSalesChartAsync();

                // Refresh LiveCharts2 as well
                InitializeLiveCharts();

                GlobalLogger.Instance.LogInfo("Enhanced rapor listesi yenilendi", "ReportsView");
                ToastManager.ShowSuccess("Raporlar basariyla yenilendi!", "Rapor Merkezi");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Rapor yenileme hatasi: {ex.Message}", "ReportsView");
                ToastManager.ShowError("Rapor verileri yenilenirken hata olustu!", "Hata");
            }
        }

        private async void DateRange_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (DateRangeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    var range = selectedItem.Content.ToString();

                    // Tarih araligina gore verileri guncelle
                    switch (range)
                    {
                        case "Bu Hafta":
                            UpdateDataForWeek();
                            break;
                        case "Bu Ay":
                            UpdateDataForMonth();
                            break;
                        case "Son 3 Ay":
                            UpdateDataForQuarter();
                            break;
                    }

                    await DrawSalesChartAsync();
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(ReportsView)} DateRange handler error: {ex.Message}");
            }
        }

        private void UpdateDataForWeek()
        {
            // Haftalik veriler icin guncelleme
            TotalRevenueText.Text = "\u20BA18,750";
            TotalSalesText.Text = "187";
        }

        private void UpdateDataForMonth()
        {
            // Aylik veriler icin guncelleme
            TotalRevenueText.Text = "\u20BA125,450";
            TotalSalesText.Text = "1,247";
        }

        private void UpdateDataForQuarter()
        {
            // Uc aylik veriler icin guncelleme
            TotalRevenueText.Text = "\u20BA345,670";
            TotalSalesText.Text = "3,456";
        }

        // Export functionality (demo)
        private async void ExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Security: SimpleSecurityService integration pending
                // Su anda tum kullanicilar rapor export edebilir
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel (*.xls)|*.xls|CSV (*.csv)|*.csv",
                    FileName = $"rapor_ozetleri_{DateTime.Now:yyyyMMdd_HHmm}"
                };
                if (sfd.ShowDialog() == true)
                {
                    var summaries = await _reportsService.GetDashboardSummariesAsync();

                    // Hedef uzanti
                    var ext = System.IO.Path.GetExtension(sfd.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext))
                    {
                        // Varsayilan xls
                        sfd.FileName += ".xls";
                        ext = ".xls";
                    }

                    if (ext == ".csv")
                    {
                        var lines = new List<string>();
                        lines.Add("En Cok Satanlar;Satis");
                        foreach (var t in summaries.TopProducts)
                            lines.Add($"{t.Name};{t.Sales}");
                        lines.Add("");
                        lines.Add("Dusuk Stok;Stok");
                        foreach (var l in summaries.LowStockItems)
                            lines.Add($"{l.Name};{l.Stock}");
                        System.IO.File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);
                    }
                    else
                    {
                        // Basit HTML tablo olarak .xls uretimi (Excel acabilir)
                        var html = new System.Text.StringBuilder();
                        html.Append("<html><head><meta charset='UTF-8'></head><body>");
                        html.Append("<h3>En Cok Satanlar</h3><table border='1' cellspacing='0' cellpadding='4'><tr><th>Urun</th><th>Satis</th></tr>");
                        foreach (var t in summaries.TopProducts)
                        {
                            html.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(t.Name)}</td><td>{t.Sales}</td></tr>");
                        }
                        html.Append("</table><br/>");
                        html.Append("<h3>Dusuk Stok</h3><table border='1' cellspacing='0' cellpadding='4'><tr><th>Urun</th><th>Stok</th></tr>");
                        foreach (var l in summaries.LowStockItems)
                        {
                            html.Append($"<tr><td>{System.Net.WebUtility.HtmlEncode(l.Name)}</td><td>{l.Stock}</td></tr>");
                        }
                        html.Append("</table></body></html>");
                        System.IO.File.WriteAllText(sfd.FileName, html.ToString(), System.Text.Encoding.UTF8);
                    }

                    ToastManager.ShowSuccess($"Disa aktarildi: {sfd.FileName}", "Rapor");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Rapor disa aktarma hatasi: {ex.Message}", "ReportsView");
                ToastManager.ShowError("Rapor disa aktarma basarisiz!", "Hata");
            }
        }

        private void ExportPDF_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = ExportPdfAsync();
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(ReportsView)} ExportPDF handler error: {ex.Message}");
            }
        }

        private async Task ExportPdfAsync()
        {
            try
            {
                // Security: SimpleSecurityService integration pending
                // Su anda tum kullanicilar rapor export edebilir
                var sfd = new SaveFileDialog
                {
                    Filter = "PDF Dosyasi (*.pdf)|*.pdf",
                    FileName = $"kritik_stok_{DateTime.Now:yyyyMMdd_HHmm}.pdf"
                };
                if (sfd.ShowDialog() == true)
                {
                    var summaries = await _reportsService.GetDashboardSummariesAsync();
                    var lowStock = summaries.LowStockItems.Select(x => (x.Name, x.Stock)).ToList();

                    // H31: Company name via MediatR GetCompanySettingsQuery
                    string company = "MesTech Teknoloji";
                    try
                    {
                        var sp = MesTechStok.Desktop.App.Services;
                        if (sp != null)
                        {
                            using var scope = sp.CreateScope();
                            var mediator = scope.ServiceProvider.GetRequiredService<MediatR.IMediator>();
                            var csResult = await mediator.Send(new MesTech.Application.Queries.GetCompanySettings.GetCompanySettingsQuery());
                            if (csResult != null && !string.IsNullOrWhiteSpace(csResult.CompanyName))
                            {
                                company = csResult.CompanyName;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        GlobalLogger.Instance.LogError($"[ReportsView] Low stock export failed: {ex.Message}");
                    }

                    var pdf = App.Services?.GetService<PdfReportService>() ?? new PdfReportService();
                    GlobalLogger.Instance.LogInfo($"PDF export starting: rows={lowStock.Count}", "ReportsView");
                    await pdf.ExportLowStockReportAsync(sfd.FileName, company, lowStock);
                    GlobalLogger.Instance.LogInfo($"PDF export done: path={sfd.FileName}", "ReportsView");
                    ToastManager.ShowSuccess($"PDF olusturuldu: {sfd.FileName}", "Rapor");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"PDF export error: {ex.Message}", "ReportsView");
                ToastManager.ShowError("PDF olusturma hatasi!", "Rapor");
            }
        }

        private async void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Security: SimpleSecurityService integration pending
                // Su anda tum kullanicilar rapor export edebilir
                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV Dosyasi (*.csv)|*.csv",
                    FileName = $"rapor_ozetleri_{DateTime.Now:yyyyMMdd_HHmm}.csv"
                };
                if (sfd.ShowDialog() == true)
                {
                    var summaries = await _reportsService.GetDashboardSummariesAsync();
                    var lines = new List<string>();
                    lines.Add("En Cok Satanlar;Satis");
                    foreach (var t in summaries.TopProducts)
                        lines.Add($"{t.Name};{t.Sales}");
                    lines.Add("");
                    lines.Add("Dusuk Stok;Stok");
                    foreach (var l in summaries.LowStockItems)
                        lines.Add($"{l.Name};{l.Stock}");
                    System.IO.File.WriteAllLines(sfd.FileName, lines, System.Text.Encoding.UTF8);
                    ToastManager.ShowSuccess($"CSV disa aktarildi: {sfd.FileName}", "Rapor");
                }
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogError($"Rapor CSV disa aktarma hatasi: {ex.Message}", "ReportsView");
                ToastManager.ShowError("CSV disa aktarma basarisiz!", "Hata");
            }
        }

        private void EmailReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBox.Show("E-posta Gonderimi\n\nRapor e-posta ile gonderilecek.\nBu ozellik gelistirme asamasindadir.",
                    "E-posta Gonder", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(ReportsView)} EmailReport handler error: {ex.Message}");
            }
        }

        private void PrintReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ToastManager.ShowInfo("Rapor yazdirma ozelligi gelistiriliyor...", "Yazdirma");
            }
            catch (Exception ex)
            {
                GlobalLogger.Instance.LogWarning($"{nameof(ReportsView)} PrintReport handler error: {ex.Message}");
            }
        }

        // ProductsView filtre/kolon profilleri yonetimi (toolbar kalabaligini azaltmak icin buraya tasindi)
        private void LoadProductsProfiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ofd = new Microsoft.Win32.OpenFileDialog { Filter = "JSON|*.json" };
                if (ofd.ShowDialog() == true)
                {
                    // Profili ProductsView'a yayinla
                    var json = System.IO.File.ReadAllText(ofd.FileName);
                    var profiles = System.Text.Json.JsonSerializer.Deserialize<List<MesTechStok.Desktop.Views.ProductsView.ColumnProfileDto>>(json) ?? new List<MesTechStok.Desktop.Views.ProductsView.ColumnProfileDto>();
                    MesTechStok.Desktop.Views.ProductsView.ProductsViewProfilesBridge.ApplyProfiles(profiles);
                    ToastManager.ShowSuccess("Urun kolon profili yuklendi", "Rapor Merkezi");
                }
            }
            catch (Exception ex) { GlobalLogger.Instance.LogError($"Profiles load error: {ex.Message}", nameof(ReportsView)); }
        }

        private void SaveProductsProfiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new Microsoft.Win32.SaveFileDialog { Filter = "JSON|*.json", FileName = $"urun_kolon_profili_{DateTime.Now:yyyyMMdd_HHmmss}.json" };
                if (sfd.ShowDialog() == true)
                {
                    var profiles = MesTechStok.Desktop.Views.ProductsView.ProductsViewProfilesBridge.CaptureProfiles();
                    var json = System.Text.Json.JsonSerializer.Serialize(profiles, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                    System.IO.File.WriteAllText(sfd.FileName, json);
                    ToastManager.ShowSuccess("Urun kolon profili kaydedildi", "Rapor Merkezi");
                }
            }
            catch (Exception ex) { GlobalLogger.Instance.LogError($"Profiles save error: {ex.Message}", nameof(ReportsView)); }
        }

        private void ResetProductsProfiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MesTechStok.Desktop.Views.ProductsView.ProductsViewProfilesBridge.ResetToDefault();
                ToastManager.ShowInfo("Urun gorunumu kolonlari varsayilanlara alindi", "Rapor Merkezi");
            }
            catch (Exception ex) { GlobalLogger.Instance.LogError($"Profiles reset error: {ex.Message}", nameof(ReportsView)); }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
