using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Stock.Queries.GetStockSummary;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Stock Management ViewModel — wired to GetStockSummaryQuery via MediatR.
/// </summary>
public partial class StockAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalProducts;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int inStockProducts;
    [ObservableProperty] private int outOfStockProducts;
    [ObservableProperty] private int lowStockProducts;
    [ObservableProperty] private decimal totalStockValue;
    [ObservableProperty] private int totalUnits;
    [ObservableProperty] private string summary = string.Empty;

    public StockAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        Title = "Stok Yonetimi";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetStockSummaryQuery(_currentUser.TenantId), ct);

            TotalProducts = result.TotalProducts;
            InStockProducts = result.InStockProducts;
            OutOfStockProducts = result.OutOfStockProducts;
            LowStockProducts = result.LowStockProducts;
            TotalStockValue = result.TotalStockValue;
            TotalUnits = result.TotalUnits;
            TotalCount = TotalProducts;
            Summary = $"{TotalProducts} urun — {TotalStockValue:N2} ₺ deger";
            IsEmpty = TotalProducts == 0;
        }, "Stok ozeti yuklenirken hata");
    }

    [RelayCommand]
    private async Task AddMovement()
    {
        // TODO: Open stock movement dialog
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
