using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Dropshipping.Commands.PreviewFeed;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Feed Onizleme ViewModel — tedarikci feed verilerini onizleme + dogrulama.
/// </summary>
public partial class FeedPreviewAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string feedUrl = string.Empty;
    [ObservableProperty] private string feedFormat = "XML";
    [ObservableProperty] private Guid feedSourceId;
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

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            // Set initial state — user will enter URL and click Preview
            FeedUrl = string.Empty;
            FeedFormat = "XML";
            PreviewLoaded = false;
            TotalProducts = 0;
            ValidProducts = 0;
            ErrorCount = 0;
            PreviewItems.Clear();
            ValidationErrors.Clear();
            IsEmpty = true;
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
    private async Task PreviewFeedAsync()
    {
        if (string.IsNullOrWhiteSpace(FeedUrl)) return;

        IsLoading = true;
        HasError = false;
        PreviewLoaded = false;
        ErrorMessage = string.Empty;
        try
        {
            if (FeedSourceId == Guid.Empty) return;

            var preview = await _mediator.Send(new PreviewFeedCommand(FeedSourceId));

            PreviewItems.Clear();
            foreach (var p in preview.Products)
            {
                PreviewItems.Add(new FeedPreviewItemDto
                {
                    SKU = p.SKU ?? "",
                    Name = p.Name,
                    Price = p.SupplierPrice,
                    Stock = p.Stock,
                    Category = p.AlreadyExists ? "Mevcut" : "Yeni"
                });
            }

            ValidationErrors.Clear();
            foreach (var w in preview.Warnings)
                ValidationErrors.Add(w);

            TotalProducts = preview.TotalProductCount;
            ValidProducts = preview.Products.Count;
            ErrorCount = preview.Warnings.Count;
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
