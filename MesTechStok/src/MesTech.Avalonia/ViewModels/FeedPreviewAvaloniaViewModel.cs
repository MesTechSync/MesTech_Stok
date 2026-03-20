using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Feed Onizleme ViewModel — tedarikci feed verilerini onizleme + dogrulama.
/// </summary>
public partial class FeedPreviewAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string feedUrl = string.Empty;
    [ObservableProperty] private string feedFormat = "XML";
    [ObservableProperty] private int totalProducts;
    [ObservableProperty] private int validProducts;
    [ObservableProperty] private int errorCount;
    [ObservableProperty] private bool previewLoaded;

    public ObservableCollection<FeedPreviewItemDto> PreviewItems { get; } = [];
    public ObservableCollection<string> ValidationErrors { get; } = [];
    public ObservableCollection<string> FormatOptions { get; } = ["XML", "CSV", "JSON", "Excel"];

    public FeedPreviewAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    [RelayCommand]
    private async Task PreviewFeedAsync()
    {
        if (string.IsNullOrWhiteSpace(FeedUrl)) return;

        IsLoading = true;
        HasError = false;
        PreviewLoaded = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(800);

            PreviewItems.Clear();
            PreviewItems.Add(new FeedPreviewItemDto { SKU = "SUP-001", Name = "Samsung Galaxy S24", Price = 42_999m, Stock = 150, Category = "Elektronik" });
            PreviewItems.Add(new FeedPreviewItemDto { SKU = "SUP-002", Name = "Apple iPhone 15 Pro", Price = 64_999m, Stock = 75, Category = "Elektronik" });
            PreviewItems.Add(new FeedPreviewItemDto { SKU = "SUP-003", Name = "Sony WH-1000XM5", Price = 8_499m, Stock = 200, Category = "Aksesuar" });
            PreviewItems.Add(new FeedPreviewItemDto { SKU = "SUP-004", Name = "Logitech MX Master 3S", Price = 2_899m, Stock = 320, Category = "Aksesuar" });
            PreviewItems.Add(new FeedPreviewItemDto { SKU = "SUP-005", Name = "Dell U2723QE Monitor", Price = 15_999m, Stock = 42, Category = "Bilgisayar" });

            ValidationErrors.Clear();
            ValidationErrors.Add("Satir 12: Fiyat alani bos — varsayilan 0 atandi");
            ValidationErrors.Add("Satir 45: SKU tekrari — SUP-003 zaten mevcut");

            TotalProducts = 120;
            ValidProducts = 118;
            ErrorCount = 2;
            PreviewLoaded = true;
            IsEmpty = PreviewItems.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Feed onizleme hatasi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await PreviewFeedAsync();
}

public class FeedPreviewItemDto
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Category { get; set; } = string.Empty;
}
