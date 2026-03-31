#pragma warning disable CS1998
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MesTech.Application.Features.Product.Queries.FetchProductFromPlatform;
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

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            // Set initial clean state — user will enter URL and click Fetch
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
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

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
            var result = await _mediator.Send(
                new FetchProductFromPlatformQuery(ProductUrl));

            if (result is null)
                throw new InvalidOperationException("Platform'dan urun bilgisi alinamadi");

            FetchedName = result.Title;
            FetchedPrice = result.Price;
            FetchedDescription = result.Description ?? string.Empty;
            FetchedCategory = result.CategoryPath ?? string.Empty;
            FetchedSKU = result.Barcode ?? string.Empty;
            FetchedStock = 0; // Platform API stok bilgisini ayrı endpoint'ten çeker
            FetchedImageUrl = result.ImageUrl ?? string.Empty;
            FetchedPlatform = result.Platform;
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
            var result = await _mediator.Send(new Application.Commands.CreateProduct.CreateProductCommand(
                Name: FetchedName,
                SKU: FetchedSKU,
                Barcode: null,
                PurchasePrice: 0,
                SalePrice: FetchedPrice,
                CategoryId: Guid.Empty, // NAV: category selection needed
                Description: FetchedDescription,
                ImageUrl: FetchedImageUrl));

            if (!result.IsSuccess)
                throw new InvalidOperationException(result.ErrorMessage ?? "Urun olusturulamadi");

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
