using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetPlatformHealth;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Sistem Sagligi ViewModel — CPU, RAM, Disk kullanimi ve servis durum tablosu.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class HealthAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    // KPI metrics
    [ObservableProperty] private string lastUpdated = "--:--";
    [ObservableProperty] private int cpuUsage;
    [ObservableProperty] private int ramUsage;
    [ObservableProperty] private int diskUsage;

    public ObservableCollection<ServiceStatusDto> ServiceStatuses { get; } = [];

    public HealthAvaloniaViewModel(IMediator mediator)
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
            var result = await _mediator.Send(new GetPlatformHealthQuery(Guid.Empty));

            LastUpdated = DateTime.Now.ToString("HH:mm:ss");

            ServiceStatuses.Clear();
            foreach (var p in result)
            {
                ServiceStatuses.Add(new ServiceStatusDto
                {
                    ServiceName = p.Platform,
                    Status = p.Status,
                    ResponseTime = $"{p.ErrorCount24h} hata/24h",
                    LastCheck = p.LastSyncAt.HasValue
                        ? p.LastSyncAt.Value.ToString("dd.MM.yyyy HH:mm")
                        : "--"
                });
            }

            IsEmpty = ServiceStatuses.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Sistem durumu yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class ServiceStatusDto
{
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ResponseTime { get; set; } = string.Empty;
    public string LastCheck { get; set; } = string.Empty;
}
