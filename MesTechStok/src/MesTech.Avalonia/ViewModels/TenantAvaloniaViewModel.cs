using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Tenant Yonetimi ViewModel — aktif tenant bilgisi + ayarlar.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class TenantAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string tenantName = "MesTech Ana";
    [ObservableProperty] private string tenantCode = "MESTECH-001";
    [ObservableProperty] private string tenantPlan = "Enterprise";
    [ObservableProperty] private string databaseName = "mestech_main";
    [ObservableProperty] private int maxUsers = 50;
    [ObservableProperty] private double storageUsed = 12.4;

    public TenantAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR query

            TenantName = "MesTech Ana";
            TenantCode = "MESTECH-001";
            TenantPlan = "Enterprise";
            DatabaseName = "mestech_main";
            MaxUsers = 50;
            StorageUsed = 12.4;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Tenant yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
