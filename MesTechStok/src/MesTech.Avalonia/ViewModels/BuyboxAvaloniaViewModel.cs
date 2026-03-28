using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetTopProducts;
using MesTech.Application.Features.Product.Queries.GetBuyboxStatus;
using MesTech.Domain.Interfaces;
using System.Collections.ObjectModel;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Buybox analiz ekrani — urun bazli buybox durumu, rakip fiyat karsilastirmasi.
/// GetTopProductsQuery ile urun listesi cekilir, her biri icin GetBuyboxStatusQuery cagirilir.
/// </summary>
public partial class BuyboxAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string _pageTitle = "Buybox Analizi";
    [ObservableProperty] private ObservableCollection<BuyboxItemViewModel> _items = new();
    [ObservableProperty] private int _winCount;
    [ObservableProperty] private int _loseCount;

    public BuyboxAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async () =>
        {
            var products = await _mediator.Send(new GetTopProductsQuery(_currentUser.TenantId, 20));
            var items = new List<BuyboxItemViewModel>();

            foreach (var p in products.Take(10))
            {
                try
                {
                    var buybox = await _mediator.Send(new GetBuyboxStatusQuery(_currentUser.TenantId, p.ProductId));
                    items.Add(new BuyboxItemViewModel
                    {
                        ProductName = p.Name,
                        OurPrice = buybox.OurPrice,
                        CompetitorPrice = buybox.LowestPrice ?? buybox.BuyboxPrice ?? 0m,
                        Status = buybox.IsWinner ? "Kazaniyor" : "Kaybediyor"
                    });
                }
                catch
                {
                    items.Add(new BuyboxItemViewModel
                    {
                        ProductName = p.Name,
                        OurPrice = p.Revenue > 0 && p.SoldQuantity > 0 ? Math.Round(p.Revenue / p.SoldQuantity, 2) : 0m,
                        CompetitorPrice = 0m,
                        Status = "Bilinmiyor"
                    });
                }
            }

            Items = new ObservableCollection<BuyboxItemViewModel>(items);
            WinCount = items.Count(i => i.Status == "Kazaniyor");
            LoseCount = items.Count(i => i.Status == "Kaybediyor");
            IsEmpty = items.Count == 0;
        }, "Buybox verisi yukleniyor");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public partial class BuyboxItemViewModel : ObservableObject
{
    [ObservableProperty] private string _productName = "";
    [ObservableProperty] private decimal _ourPrice;
    [ObservableProperty] private decimal _competitorPrice;
    [ObservableProperty] private string _status = "";
    public string StatusColor => Status == "Kazanıyor" ? "#4CAF50" : "#F44336";
}
