using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dropshipping Dashboard ViewModel — KPI kartlari + tedarikci performans tablosu.
/// TODO: Replace demo data with MediatR.Send(new GetDropshipDashboardQuery()) when A1 CQRS is ready.
/// </summary>
public partial class DropshipDashboardAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    // KPI values
    [ObservableProperty] private int totalOrders;
    [ObservableProperty] private decimal totalRevenue;
    [ObservableProperty] private decimal totalProfit;
    [ObservableProperty] private decimal averageMargin;

    public ObservableCollection<DropshipSupplierPerformanceDto> Suppliers { get; } = [];

    public DropshipDashboardAvaloniaViewModel(IMediator mediator)
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
            // TODO: Replace with MediatR.Send(new GetDropshipDashboardQuery()) when A1 CQRS is ready
            await Task.Delay(300);

            TotalOrders = 347;
            TotalRevenue = 284_500.00m;
            TotalProfit = 42_675.00m;
            AverageMargin = 15.0m;

            Suppliers.Clear();
            Suppliers.Add(new DropshipSupplierPerformanceDto { SupplierName = "ABC Elektronik", OrderCount = 145, Revenue = 128_400m, FulfillRate = 97.2, AvgDeliveryDays = 2.1 });
            Suppliers.Add(new DropshipSupplierPerformanceDto { SupplierName = "XYZ Bilisim", OrderCount = 98, Revenue = 87_300m, FulfillRate = 94.8, AvgDeliveryDays = 2.8 });
            Suppliers.Add(new DropshipSupplierPerformanceDto { SupplierName = "Guney Aksesuar", OrderCount = 67, Revenue = 42_100m, FulfillRate = 91.5, AvgDeliveryDays = 3.2 });
            Suppliers.Add(new DropshipSupplierPerformanceDto { SupplierName = "Delta Depo", OrderCount = 37, Revenue = 26_700m, FulfillRate = 88.9, AvgDeliveryDays = 3.5 });

            IsEmpty = Suppliers.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Dropshipping verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class DropshipSupplierPerformanceDto
{
    public string SupplierName { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal Revenue { get; set; }
    public double FulfillRate { get; set; }
    public double AvgDeliveryDays { get; set; }
}
