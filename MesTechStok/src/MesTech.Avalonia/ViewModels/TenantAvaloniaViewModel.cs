using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Billing.Queries.GetTenantSubscription;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Tenant Yonetimi ViewModel — aktif tenant bilgisi + ayarlar.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class TenantAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string tenantName = "MesTech Ana";
    [ObservableProperty] private string tenantCode = "MESTECH-001";
    [ObservableProperty] private string tenantPlan = "Enterprise";
    [ObservableProperty] private string databaseName = "mestech_main";
    [ObservableProperty] private int maxUsers = 50;
    [ObservableProperty] private double storageUsed = 12.4;

    public TenantAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var subscription = await _mediator.Send(new GetTenantSubscriptionQuery(_currentUser.TenantId), ct);
            if (subscription is not null)
            {
                TenantPlan = subscription.PlanName;
                TenantCode = subscription.Id.ToString("N")[..8].ToUpperInvariant();
            }
        }, "Tenant yuklenirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
