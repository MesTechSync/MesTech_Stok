using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using MediatR;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Application.Features.Reporting.Queries.GetSavedReports;
using MesTech.Application.Features.Reports.PlatformSalesReport;
using MesTech.Domain.Interfaces;
using SkiaSharp;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// WPF019: Report Dashboard ViewModel — LiveCharts2 grafik + TreeView rapor tipleri + filtreler.
/// ColumnSeries (satış) + LineSeries (trend) + summary table + Excel aktarma.
/// </summary>
public partial class ReportDashboardAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ReportDashboardAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        BuildReportTypeTree();
        BuildChartData();
    }

    // ─── Generating overlay ──────────────────────────────────────
    [ObservableProperty] private bool isGenerating;
    [ObservableProperty] private string generatingMessage = string.Empty;
    [ObservableProperty] private int progressPercent;

    // ─── Left panel: Report type selection ───────────────────────
    [ObservableProperty] private ReportCategoryItem? selectedReportCategory;
    [ObservableProperty] private string selectedReportType = "Günlük Satış";

    public ObservableCollection<ReportCategoryItem> ReportCategories { get; } = new();

    // ─── Right panel: Filter bar ──────────────────────────────────
    [ObservableProperty] private DateTimeOffset dateFrom = new(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
    [ObservableProperty] private DateTimeOffset dateTo = new(DateTime.Today);
    [ObservableProperty] private string selectedPlatform = "Tümü";

    public ObservableCollection<string> Platforms { get; } = new()
    {
        "Tümü", "Trendyol", "Hepsiburada", "N11", "Amazon", "Çiçeksepeti", "Pazarama"
    };

    // ─── LiveCharts2: ColumnSeries (satış) + LineSeries (trend) ──
    [ObservableProperty] private ISeries[] chartSeries = Array.Empty<ISeries>();
    [ObservableProperty] private Axis[] xAxes = Array.Empty<Axis>();
    [ObservableProperty] private Axis[] yAxes = Array.Empty<Axis>();

    // ─── Summary table ────────────────────────────────────────────
    public ObservableCollection<ReportRowItem> ReportRows { get; } = new();

    // ─── Summary KPI cards ────────────────────────────────────────
    [ObservableProperty] private string totalSales = "0,00 TL";
    [ObservableProperty] private int totalOrders;
    [ObservableProperty] private string netProfit = "0,00 TL";
    [ObservableProperty] private int totalProducts;
    [ObservableProperty] private int lowStockCount;
    [ObservableProperty] private string totalExpenses = "0,00 TL";

    // ─── Legacy compat (Scheduled + Recent reports) ───────────────
    public ObservableCollection<ScheduledReportItem> ScheduledReports { get; } = new();
    public ObservableCollection<RecentReportItem> RecentReports { get; } = new();

    // ─── Export format ────────────────────────────────────────────
    [ObservableProperty] private string selectedExportFormat = "Excel";
    public ObservableCollection<string> ExportFormats { get; } = new() { "Excel", "PDF", "CSV" };

    // ─── Load ─────────────────────────────────────────────────────
    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var savedReports = await _mediator.Send(new GetSavedReportsQuery(_currentUser.TenantId), ct);

            RecentReports.Clear();
            foreach (var report in savedReports)
            {
                RecentReports.Add(new RecentReportItem(
                    report.CreatedAt.ToString("dd.MM.yyyy"),
                    report.Name,
                    report.ReportType,
                    "—"));
            }
        }, "Rapor verileri yuklenirken hata");
    }

    // ─── Commands ─────────────────────────────────────────────────

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        IsGenerating = true;
        ProgressPercent = 0;
        GeneratingMessage = $"{SelectedReportType} hazırlanıyor...";
        try
        {
            ProgressPercent = 30;
            await BuildReportDataAsync();
            ProgressPercent = 100;
            GeneratingMessage = $"{SelectedReportType} basariyla olusturuldu!";
        }
        catch (Exception ex)
        {
            GeneratingMessage = string.Empty;
            HasError = true;
            ErrorMessage = $"Rapor oluşturulamadı: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private async Task ExportExcelAsync()
    {
        IsGenerating = true;
        GeneratingMessage = "Excel dosyası hazırlanıyor...";
        try
        {
            var result = await _mediator.Send(new ExportReportCommand(
                _currentUser.TenantId, "dashboard", "xlsx"));
            if (result.FileData.Length > 0)
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                Directory.CreateDirectory(dir);
                await File.WriteAllBytesAsync(Path.Combine(dir, result.FileName), result.FileData);
            }
            GeneratingMessage = $"Excel aktarimi tamamlandi ({result.ExportedCount} kayit)!";
        }
        catch (Exception ex)
        {
            GeneratingMessage = string.Empty;
            HasError = true;
            ErrorMessage = $"Excel aktarımı başarısız: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private async Task GenerateQuickReportAsync(string reportName)
    {
        SelectedReportType = reportName;
        await GenerateReportAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    // ─── Helpers ──────────────────────────────────────────────────

    private void BuildReportTypeTree()
    {
        ReportCategories.Clear();
        ReportCategories.Add(new ReportCategoryItem("📊 Satış Raporları", new[]
        {
            "Günlük Satış", "Haftalık Satış", "Aylık Satış"
        }));
        ReportCategories.Add(new ReportCategoryItem("📦 Stok Raporları", new[]
        {
            "Stok Hareket", "Stok Değer"
        }));
        ReportCategories.Add(new ReportCategoryItem("💰 Karlılık Raporları", new[]
        {
            "Ürün Karlılığı", "Platform Karlılığı"
        }));
    }

    private void BuildChartData()
    {
        var labels = Enumerable.Range(0, 7)
            .Select(i => DateTime.Today.AddDays(-6 + i).ToString("dd MMM"))
            .ToArray();

        // ColumnSeries — satış tutarı
        var salesValues = new double[] { 12500, 18200, 15800, 21400, 19700, 24300, 27100 };

        // LineSeries — trend (sipariş adet)
        var orderValues = new double[] { 42, 61, 53, 74, 68, 85, 97 };

        ChartSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Satış (TL)",
                Values = salesValues,
                Fill = new SolidColorPaint(SKColor.Parse("#2563EB")),
                Stroke = null,
                MaxBarWidth = 32
            },
            new LineSeries<double>
            {
                Name = "Sipariş Adedi",
                Values = orderValues,
                Stroke = new SolidColorPaint(SKColor.Parse("#16A34A")) { StrokeThickness = 3 },
                Fill = null,
                GeometrySize = 8,
                GeometryStroke = new SolidColorPaint(SKColor.Parse("#16A34A")) { StrokeThickness = 2 },
                ScalesYAt = 1
            }
        };

        XAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                LabelsRotation = 0,
                TextSize = 12
            }
        };

        YAxes = new Axis[]
        {
            new Axis
            {
                Name = "Satış (TL)",
                Labeler = v => $"{v:N0} TL",
                TextSize = 11
            },
            new Axis
            {
                Name = "Sipariş",
                Position = LiveChartsCore.Measure.AxisPosition.End,
                TextSize = 11
            }
        };
    }

    private async Task BuildReportDataAsync()
    {
        ReportRows.Clear();

        var platformFilter = SelectedPlatform == "Tümü" ? null : SelectedPlatform;
        var results = await _mediator.Send(new PlatformSalesReportQuery(
            _currentUser.TenantId,
            DateFrom.DateTime,
            DateTo.DateTime,
            platformFilter));

        foreach (var r in results)
        {
            ReportRows.Add(new ReportRowItem(
                DateFrom.DateTime.ToString("dd.MM.yyyy"),
                r.Platform,
                SelectedReportType,
                r.TotalOrders,
                r.TotalRevenue,
                r.NetRevenue));
        }

        var totalSalesVal = results.Sum(r => r.TotalRevenue);
        var totalProfitVal = results.Sum(r => r.NetRevenue);
        var totalOrdersVal = results.Sum(r => r.TotalOrders);

        TotalSales = $"{totalSalesVal:N2} TL";
        TotalOrders = totalOrdersVal;
        NetProfit = $"{totalProfitVal:N2} TL";
        TotalExpenses = $"{results.Sum(r => r.Commissions):N2} TL";

        IsEmpty = ReportRows.Count == 0;
    }
}

// ─── Model Classes ────────────────────────────────────────────────────────────

public class ReportCategoryItem
{
    public string CategoryName { get; }
    public ObservableCollection<string> Children { get; }

    public ReportCategoryItem(string categoryName, IEnumerable<string> children)
    {
        CategoryName = categoryName;
        Children = new ObservableCollection<string>(children);
    }
}

public class ReportRowItem
{
    public string Tarih { get; }
    public string Platform { get; }
    public string DonemTipi { get; }
    public int SiparisSayisi { get; }
    public decimal SatisAmount { get; }
    public decimal KarAmount { get; }

    public string SatisTL => $"{SatisAmount:N2} TL";
    public string KarTL => $"{KarAmount:N2} TL";
    public string MarjinYuzde => SatisAmount > 0 ? $"{KarAmount / SatisAmount * 100:N1}%" : "—";

    public ReportRowItem(string tarih, string platform, string donemTipi, int siparisSayisi, decimal satisAmount, decimal karAmount)
    {
        Tarih = tarih;
        Platform = platform;
        DonemTipi = donemTipi;
        SiparisSayisi = siparisSayisi;
        SatisAmount = satisAmount;
        KarAmount = karAmount;
    }
}

public class ScheduledReportItem
{
    public string Name { get; }
    public string Period { get; }
    public string Format { get; }
    public string LastRun { get; }
    public string Status { get; }

    public ScheduledReportItem(string name, string period, string format, string lastRun, string status)
    {
        Name = name;
        Period = period;
        Format = format;
        LastRun = lastRun;
        Status = status;
    }
}

public class RecentReportItem
{
    public string Date { get; }
    public string ReportName { get; }
    public string Format { get; }
    public string Size { get; }

    public RecentReportItem(string date, string reportName, string format, string size)
    {
        Date = date;
        ReportName = reportName;
        Format = format;
        Size = size;
    }
}
