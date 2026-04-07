using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetDashboardSummary;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Karsilama Ekrani ViewModel — logo + hosgeldin mesaji + gercek KPI'lar.
/// Wired to GetDashboardSummaryQuery via MediatR.
/// </summary>
public partial class WelcomeAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string welcomeText = "Entegrator Stok Yonetim Sistemi";
    [ObservableProperty] private string totalProducts = "-";
    [ObservableProperty] private string totalOrders = "-";
    [ObservableProperty] private string activePlatforms = "-";

    public ObservableCollection<RecentActivityDto> RecentActivities { get; } = [];

    public WelcomeAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var summary = await _mediator.Send(
                new GetDashboardSummaryQuery(_currentUser.TenantId), ct);

            TotalProducts = summary.ActiveProductCount.ToString("N0");
            TotalOrders = summary.TodayOrderCount.ToString("N0");
            ActivePlatforms = summary.ActivePlatformCount.ToString();

            RecentActivities.Clear();
            foreach (var order in summary.RecentOrders.Take(5))
            {
                RecentActivities.Add(new RecentActivityDto
                {
                    Description = $"Siparis {order.OrderNumber} — {order.CustomerName} ({order.TotalAmount:N2} TL)",
                    TimeAgo = FormatTimeAgo(order.CreatedAt)
                });
            }

            IsEmpty = summary.ActiveProductCount == 0 && summary.TodayOrderCount == 0;
        }, "Karsilama ekrani yuklenirken hata");
    }

    private static string FormatTimeAgo(DateTime dt)
    {
        var diff = DateTime.Now - dt;
        if (diff.TotalMinutes < 1) return "simdi";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} dk once";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} saat once";
        return $"{(int)diff.TotalDays} gun once";
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();
}

public class RecentActivityDto
{
    public string Description { get; set; } = string.Empty;
    public string TimeAgo { get; set; } = string.Empty;
}
