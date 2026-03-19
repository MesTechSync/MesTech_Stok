using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dropshipping Karlilik ViewModel — urun bazli kar analizi + tarih filtre + ozet satir.
/// TODO: Replace demo data with MediatR queries when A1 CQRS is ready.
/// </summary>
public partial class DropshipProfitAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;

    // Date range filter
    [ObservableProperty] private DateTimeOffset startDate = DateTimeOffset.Now.AddDays(-30);
    [ObservableProperty] private DateTimeOffset endDate = DateTimeOffset.Now;

    // Summary
    [ObservableProperty] private decimal totalRevenue;
    [ObservableProperty] private decimal totalCost;
    [ObservableProperty] private decimal totalCommission;
    [ObservableProperty] private decimal totalNetProfit;
    [ObservableProperty] private decimal overallMargin;

    public ObservableCollection<DropshipProfitItemDto> Items { get; } = [];

    public DropshipProfitAvaloniaViewModel(IMediator mediator)
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
            // TODO: Replace with MediatR.Send(new GetDropshipProfitQuery()) when A1 CQRS is ready
            await Task.Delay(300);

            Items.Clear();
            Items.Add(new DropshipProfitItemDto { ProductName = "Samsung Galaxy S24 Ultra", QuantitySold = 23, CustomerPrice = 64_999m, SupplierPrice = 52_000m, Commission = 6_500m, NetProfit = 6_499m, MarginPercent = 10.0m });
            Items.Add(new DropshipProfitItemDto { ProductName = "Sony WH-1000XM5", QuantitySold = 45, CustomerPrice = 11_499m, SupplierPrice = 8_500m, Commission = 1_150m, NetProfit = 1_849m, MarginPercent = 16.1m });
            Items.Add(new DropshipProfitItemDto { ProductName = "Logitech MX Master 3S", QuantitySold = 67, CustomerPrice = 3_299m, SupplierPrice = 2_400m, Commission = 330m, NetProfit = 569m, MarginPercent = 17.2m });
            Items.Add(new DropshipProfitItemDto { ProductName = "Dell U2723QE Monitor", QuantitySold = 12, CustomerPrice = 18_799m, SupplierPrice = 14_200m, Commission = 1_880m, NetProfit = 2_719m, MarginPercent = 14.5m });
            Items.Add(new DropshipProfitItemDto { ProductName = "Dyson V15 Supurge", QuantitySold = 18, CustomerPrice = 28_990m, SupplierPrice = 22_500m, Commission = 2_899m, NetProfit = 3_591m, MarginPercent = 12.4m });
            Items.Add(new DropshipProfitItemDto { ProductName = "Philips Airfryer XXL", QuantitySold = 34, CustomerPrice = 7_499m, SupplierPrice = 5_800m, Commission = 750m, NetProfit = 949m, MarginPercent = 12.7m });

            TotalRevenue = Items.Sum(i => i.CustomerPrice * i.QuantitySold);
            TotalCost = Items.Sum(i => i.SupplierPrice * i.QuantitySold);
            TotalCommission = Items.Sum(i => i.Commission * i.QuantitySold);
            TotalNetProfit = Items.Sum(i => i.NetProfit * i.QuantitySold);
            OverallMargin = TotalRevenue > 0 ? Math.Round(TotalNetProfit / TotalRevenue * 100, 1) : 0;

            TotalCount = Items.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Karlilik verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class DropshipProfitItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal CustomerPrice { get; set; }
    public decimal SupplierPrice { get; set; }
    public decimal Commission { get; set; }
    public decimal NetProfit { get; set; }
    public decimal MarginPercent { get; set; }
}
