using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProfitability;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Dropshipping Karlilik ViewModel — urun bazli kar analizi + tarih filtre + ozet satir.
/// </summary>
public partial class DropshipProfitAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

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

    public DropshipProfitAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var result = await _mediator.Send(new GetDropshipProfitabilityQuery(_currentUser.TenantId));

            Items.Clear();
            foreach (var dto in result)
            {
                Items.Add(new DropshipProfitItemDto
                {
                    ProductName = dto.ProductName,
                    QuantitySold = dto.QuantitySold,
                    CustomerPrice = dto.CustomerPrice,
                    SupplierPrice = dto.SupplierPrice,
                    Commission = dto.CommissionAmount,
                    NetProfit = dto.NetProfit,
                    MarginPercent = dto.ProfitMargin
                });
            }

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
