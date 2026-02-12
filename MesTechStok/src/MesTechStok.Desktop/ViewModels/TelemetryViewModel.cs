using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTechStok.Core.Data.Models;
using MesTechStok.Desktop.Services;

namespace MesTechStok.Desktop.ViewModels;

public partial class TelemetryViewModel : ViewModelBase
{
    private readonly ITelemetryQueryService _service;
    public TelemetryViewModel(ITelemetryQueryService service) { _service = service; }

    [ObservableProperty] private string? endpointFilter;
    [ObservableProperty] private string? categoryFilter;
    [ObservableProperty] private bool? successFilter = null; // null = all
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private ObservableCollection<ApiCallLog> logs = new();
    [ObservableProperty] private ObservableCollection<CircuitStateLog> circuitStateLogs = new();

    public override async Task InitializeAsync() { await RefreshTelemetryAsync(); }

    [RelayCommand]
    private async Task RefreshTelemetryAsync()
    {
        try
        {
            IsLoading = true;
            var apiData = await _service.GetRecentAsync(200, EndpointFilter, SuccessFilter, CategoryFilter);
            var circuitData = await _service.GetCircuitStateHistoryAsync(100);

            Logs = new ObservableCollection<ApiCallLog>(apiData);
            CircuitStateLogs = new ObservableCollection<CircuitStateLog>(circuitData);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        EndpointFilter = null;
        CategoryFilter = null;
        SuccessFilter = null;
        await RefreshTelemetryAsync();
    }
}
