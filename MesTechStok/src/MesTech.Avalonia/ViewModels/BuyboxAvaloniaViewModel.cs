using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using System.Collections.ObjectModel;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Buybox analiz ekrani — FIX-18 Gorev #13.
/// Urun bazli buybox durumu, rakip fiyat karsilastirmasi.
/// TODO(v2): Wire to IBuyboxService query via MediatR.
/// </summary>
public partial class BuyboxAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string _pageTitle = "Buybox Analizi";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private ObservableCollection<BuyboxItemViewModel> _items = new();

    public BuyboxAvaloniaViewModel(IMediator mediator) => _mediator = mediator;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            // TODO(v2): Wire to IBuyboxService query
            await Task.Delay(50); // Simulate async load
            Items = new ObservableCollection<BuyboxItemViewModel>(new[]
            {
                new BuyboxItemViewModel { ProductName = "Örnek Ürün 1", OurPrice = 149.90m, CompetitorPrice = 159.90m, Status = "Kazanıyor" },
                new BuyboxItemViewModel { ProductName = "Örnek Ürün 2", OurPrice = 299.90m, CompetitorPrice = 279.90m, Status = "Kaybediyor" },
            });
        }
        finally { IsLoading = false; }
    }
}

public partial class BuyboxItemViewModel : ObservableObject
{
    [ObservableProperty] private string _productName = "";
    [ObservableProperty] private decimal _ourPrice;
    [ObservableProperty] private decimal _competitorPrice;
    [ObservableProperty] private string _status = "";
    public string StatusColor => Status == "Kazanıyor" ? "#4CAF50" : "#F44336";
}
