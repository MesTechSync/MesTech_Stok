using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Platform Listesi ViewModel — 13 platform karti ile pazaryeri yonetimi.
/// TODO: Replace demo data with MediatR.Send(new GetPlatformListQuery()) when A1 CQRS is ready.
/// </summary>
public partial class PlatformListAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<PlatformCardDto> Platforms { get; } = [];

    public PlatformListAvaloniaViewModel(IMediator mediator)
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
            // TODO: Replace with MediatR.Send(new GetPlatformListQuery()) when A1 CQRS is ready
            await Task.Delay(200);

            Platforms.Clear();
            Platforms.Add(new PlatformCardDto { Name = "Trendyol", Color = "#FF6F00", StoreCount = 2, IsActive = true });
            Platforms.Add(new PlatformCardDto { Name = "Hepsiburada", Color = "#FF6000", StoreCount = 1, IsActive = true });
            Platforms.Add(new PlatformCardDto { Name = "N11", Color = "#0B2441", StoreCount = 1, IsActive = true });
            Platforms.Add(new PlatformCardDto { Name = "Ciceksepeti", Color = "#F27A1A", StoreCount = 1, IsActive = true });
            Platforms.Add(new PlatformCardDto { Name = "Amazon", Color = "#FF9900", StoreCount = 1, IsActive = true });
            Platforms.Add(new PlatformCardDto { Name = "eBay", Color = "#E53238", StoreCount = 0, IsActive = false });
            Platforms.Add(new PlatformCardDto { Name = "Shopify", Color = "#96BF48", StoreCount = 1, IsActive = true });
            Platforms.Add(new PlatformCardDto { Name = "WooCommerce", Color = "#96588A", StoreCount = 1, IsActive = true });
            Platforms.Add(new PlatformCardDto { Name = "Pazarama", Color = "#00B8D4", StoreCount = 0, IsActive = false });
            Platforms.Add(new PlatformCardDto { Name = "PttAVM", Color = "#FFD600", StoreCount = 1, IsActive = true });
            Platforms.Add(new PlatformCardDto { Name = "OpenCart", Color = "#23A8E0", StoreCount = 1, IsActive = true });
            Platforms.Add(new PlatformCardDto { Name = "Ozon", Color = "#005BFF", StoreCount = 0, IsActive = false });
            Platforms.Add(new PlatformCardDto { Name = "Etsy", Color = "#F1641E", StoreCount = 0, IsActive = false });

            TotalCount = Platforms.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Platform listesi yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class PlatformCardDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#0078D4";
    public int StoreCount { get; set; }
    public bool IsActive { get; set; }
    public string StatusText => IsActive ? "Aktif" : "Pasif";
}
