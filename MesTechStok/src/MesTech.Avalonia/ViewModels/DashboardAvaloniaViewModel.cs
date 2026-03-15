using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Avalonia PoC dashboard — summary KPI cards.
/// Mirrors DashboardView.xaml.cs logic but as a proper ViewModel (no code-behind).
/// In production, the WPF DashboardView code-behind should be refactored to this pattern.
/// </summary>
public partial class DashboardAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string totalProducts = "0";
    [ObservableProperty] private string totalStockValue = "0 TL";
    [ObservableProperty] private string lowStockCount = "0";
    [ObservableProperty] private string activeCategories = "0";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string lastUpdated = "--:--";

    public DashboardAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            // Uses same MediatR pipeline as WPF — queries go through Application layer
            // TODO: Wire GetDashboardDataQuery when full migration starts
            await Task.Delay(100); // Simulate async load

            // Placeholder data for PoC demonstration
            TotalProducts = "1,247";
            TotalStockValue = "2,456,890 TL";
            LowStockCount = "23";
            ActiveCategories = "18";
            LastUpdated = DateTime.Now.ToString("HH:mm:ss");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
