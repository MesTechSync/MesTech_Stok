using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MesTechStok.Desktop.ViewModels;

/// <summary>
/// EMR-03-v2 ALAN-C: Yeni unified KPI'lar için ViewModel.
/// Mevcut DashboardView.xaml.cs logic'ine DOKUNMAZ — ek KPI'lar sağlar.
/// GetDashboardSummaryQuery ile: AktifPlatform, BekleyenKargo, İadeOranı, AylıkCiro.
/// </summary>
public partial class DashboardKpiViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<DashboardKpiViewModel> _logger;

    // ── Ek KPI'lar (DashboardView'da eksik olanlar) ────────────────────────
    [ObservableProperty] private string activePlatformCount = "—";
    [ObservableProperty] private string pendingShipmentCount = "—";
    [ObservableProperty] private string returnRate = "—";
    [ObservableProperty] private string monthlySalesAmount = "—";

    // ── Bugünkü satış (mevcut widgetları tamamlayıcı) ─────────────────────
    [ObservableProperty] private string todaySalesAmount = "—";
    [ObservableProperty] private string todayOrderCount = "—";

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string lastUpdated = "--:--";
    [ObservableProperty] private bool hasError;

    public DashboardKpiViewModel(
        IMediator mediator,
        ITenantProvider tenantProvider,
        ILogger<DashboardKpiViewModel> logger)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var summary = await _mediator.Send(
                new GetDashboardSummaryQuery(tenantId), CancellationToken.None);

            ActivePlatformCount = summary.ActivePlatformCount.ToString();
            PendingShipmentCount = summary.PendingShipmentCount.ToString();
            ReturnRate = $"%{summary.ReturnRate:F1}";
            MonthlySalesAmount = summary.MonthlySalesAmount.ToString("N2") + " TL";
            TodaySalesAmount = summary.TodaySalesAmount.ToString("N2") + " TL";
            TodayOrderCount = summary.TodayOrderCount.ToString();
            LastUpdated = DateTime.Now.ToString("HH:mm:ss");
        }
        catch (Exception ex)
        {
            HasError = true;
            _logger.LogWarning(ex, "DashboardKpiViewModel load failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
