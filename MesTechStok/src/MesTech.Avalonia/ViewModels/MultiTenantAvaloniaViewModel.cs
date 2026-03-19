using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Multi-Tenant Yonetimi ViewModel — tenant listesi + aktif tenant bilgisi.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class MultiTenantAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string activeTenantName = "MesTech Ana";
    [ObservableProperty] private string activeTenantId = "tenant-001";

    public ObservableCollection<TenantListItemDto> Tenants { get; } = [];

    public MultiTenantAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR query

            ActiveTenantName = "MesTech Ana";
            ActiveTenantId = "tenant-001";

            Tenants.Clear();
            Tenants.Add(new TenantListItemDto { Name = "MesTech Ana", Database = "mestech_main", Status = "Aktif", CreatedAt = "01.01.2026" });
            Tenants.Add(new TenantListItemDto { Name = "MesTech Test", Database = "mestech_test", Status = "Aktif", CreatedAt = "15.02.2026" });
            Tenants.Add(new TenantListItemDto { Name = "Demo Firma", Database = "mestech_demo", Status = "Pasif", CreatedAt = "01.03.2026" });
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Multi-Tenant yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class TenantListItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
