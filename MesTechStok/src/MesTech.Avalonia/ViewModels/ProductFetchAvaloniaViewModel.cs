using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Urun Cekme ViewModel — URL'den urun bilgisi cekme ve kaydetme.
/// </summary>
public partial class ProductFetchAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string productUrl = string.Empty;
    [ObservableProperty] private bool productLoaded;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private bool saveCompleted;

    // Fetched product info
    [ObservableProperty] private string fetchedName = string.Empty;
    [ObservableProperty] private decimal fetchedPrice;
    [ObservableProperty] private string fetchedDescription = string.Empty;
    [ObservableProperty] private string fetchedCategory = string.Empty;
    [ObservableProperty] private string fetchedSKU = string.Empty;
    [ObservableProperty] private int fetchedStock;
    [ObservableProperty] private string fetchedImageUrl = string.Empty;
    [ObservableProperty] private string fetchedPlatform = string.Empty;

    public ProductFetchAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RelayCommand]
    private async Task FetchProductAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductUrl)) return;

        IsLoading = true;
        HasError = false;
        ProductLoaded = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(1000);

            FetchedName = "Samsung Galaxy S24 Ultra 256GB";
            FetchedPrice = 64_999.00m;
            FetchedDescription = "Samsung Galaxy S24 Ultra, 256GB, Titanium Siyah, 200MP Kamera";
            FetchedCategory = "Elektronik > Telefon > Akilli Telefon";
            FetchedSKU = "SM-S928B-256-BK";
            FetchedStock = 45;
            FetchedImageUrl = "https://cdn.example.com/samsung-s24-ultra.jpg";
            FetchedPlatform = "Trendyol";
            ProductLoaded = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Urun cekilemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveProductAsync()
    {
        IsSaving = true;
        HasError = false;
        try
        {
            await Task.Delay(500);
            SaveCompleted = true;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Urun kaydedilemedi: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private void Reset()
    {
        ProductUrl = string.Empty;
        ProductLoaded = false;
        SaveCompleted = false;
        FetchedName = string.Empty;
        FetchedPrice = 0;
        FetchedDescription = string.Empty;
        FetchedCategory = string.Empty;
        FetchedSKU = string.Empty;
        FetchedStock = 0;
        FetchedImageUrl = string.Empty;
        FetchedPlatform = string.Empty;
    }
}
