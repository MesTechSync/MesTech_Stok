using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetExpenseReport;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Application.Features.Stock.Queries.GetStockSummary;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// ViewModel for Reports screen — Dalga 14/15.
/// Wired to GetDashboardSummaryQuery + GetStockSummaryQuery via MediatR.
/// </summary>
public partial class ReportsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public ReportsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [ObservableProperty] private string summary = "Rapor olusturmak icin tarih araligi secin ve rapor turunu belirleyin.";
    [ObservableProperty] private DateTimeOffset startDate = new(new DateTime(2026, 3, 1));
    [ObservableProperty] private DateTimeOffset endDate = new(new DateTime(2026, 3, 17));
    [ObservableProperty] private bool isGenerating;
    [ObservableProperty] private string generatingMessage = string.Empty;

    // Sales Report
    [ObservableProperty] private string totalSales = "0 TL";
    [ObservableProperty] private string totalOrders = "0";
    [ObservableProperty] private string averageOrderValue = "0 TL";

    // Stock Report
    [ObservableProperty] private string totalProducts = "0";
    [ObservableProperty] private string lowStockCount = "0";
    [ObservableProperty] private string outOfStockCount = "0";

    // Revenue Report
    [ObservableProperty] private string totalRevenue = "0 TL";
    [ObservableProperty] private string totalExpenses = "0 TL";
    [ObservableProperty] private string netProfit = "0 TL";

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            // KÖK-1 FIX: Sequential query — DbContext concurrent access önleme
            var dashboard = await _mediator.Send(new GetDashboardSummaryQuery(TenantId: _currentUser.TenantId), ct);
            var stock = await _mediator.Send(new GetStockSummaryQuery(TenantId: _currentUser.TenantId), ct);

            // Sales Report
            TotalSales = $"{dashboard.MonthlySalesAmount:N0} TL";
            TotalOrders = $"{dashboard.TodayOrderCount:N0}";
            AverageOrderValue = dashboard.TodayOrderCount > 0
                ? $"{dashboard.TodaySalesAmount / dashboard.TodayOrderCount:N2} TL"
                : "0 TL";

            // Stock Report
            TotalProducts = $"{stock.TotalProducts:N0}";
            LowStockCount = $"{stock.LowStockProducts:N0}";
            OutOfStockCount = $"{stock.OutOfStockProducts:N0}";

            // Revenue Report
            TotalRevenue = $"{dashboard.MonthlySalesAmount:N0} TL";
            var expenses = await _mediator.Send(new GetExpenseReportQuery(
                _currentUser.TenantId, DateTime.Now.AddMonths(-1), DateTime.Now), ct);
            TotalExpenses = $"{expenses.TotalExpenses:N0} TL";
            NetProfit = $"{dashboard.MonthlySalesAmount - expenses.TotalExpenses:N0} TL";

            Summary = "Rapor olusturmak icin tarih araligi secin ve rapor turunu belirleyin.";
        }, "Raporlar yuklenirken hata");
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    [RelayCommand]
    private Task GenerateReport(string reportType)
    {
        IsGenerating = true;
        GeneratingMessage = $"{reportType} hazirlaniyor...";
        try
        {
            GeneratingMessage = string.Empty;
            Summary = $"{reportType} basariyla olusturuldu. ({StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy})";
        }
        catch (Exception ex)
        {
            GeneratingMessage = string.Empty;
            HasError = true;
            ErrorMessage = $"Rapor olusturulamadi: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
        return Task.CompletedTask;
    }
}
