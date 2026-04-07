using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetServiceHealth;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Performans Dashboard ViewModel — S2-DEV2-01 (Menü 76).
/// Altyapı servisleri sağlık durumu: PostgreSQL, Redis, RabbitMQ response time.
/// GetServiceHealthQuery ile veri çeker.
/// </summary>
public partial class PerformanceDashboardAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int healthyCount;
    [ObservableProperty] private int unhealthyCount;
    [ObservableProperty] private int totalServices;

    public ObservableCollection<ServiceHealthItemDto> Services { get; } = [];

    public PerformanceDashboardAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetServiceHealthQuery(), ct);

            Services.Clear();
            foreach (var s in result)
            {
                Services.Add(new ServiceHealthItemDto
                {
                    ServiceName = s.ServiceName,
                    IsHealthy = s.IsHealthy,
                    ResponseTime = s.ResponseTime,
                    StatusText = s.IsHealthy ? "Saglikli" : "Sorunlu",
                    StatusColor = s.IsHealthy ? "#10B981" : "#EF4444"
                });
            }

            TotalServices = result.Count;
            HealthyCount = result.Count(s => s.IsHealthy);
            UnhealthyCount = result.Count(s => !s.IsHealthy);
            IsEmpty = result.Count == 0;
        }, "Servis durumlari yuklenirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class ServiceHealthItemDto
{
    public string ServiceName { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
    public string ResponseTime { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
}
