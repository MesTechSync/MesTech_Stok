using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.AI.Commands.GenerateProductDescription;
using MesTech.Application.Features.Dashboard.Queries.GetTopProducts;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// G102: AI Urun Aciklama UI — 3 aciklama (A/B/C), SEO skoru, karakter sayaci.
/// </summary>
public partial class ProductDescriptionAIViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // Product Selection
    [ObservableProperty] private ObservableCollection<ProductSelectItem> _products = [];
    [ObservableProperty] private ProductSelectItem? _selectedProduct;
    [ObservableProperty] private string _productName = string.Empty;
    [ObservableProperty] private string _category = string.Empty;
    [ObservableProperty] private string _brand = string.Empty;
    [ObservableProperty] private string _features = string.Empty;

    // AI Results
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string _shortDescription = string.Empty;
    [ObservableProperty] private string _mediumDescription = string.Empty;
    [ObservableProperty] private string _longDescription = string.Empty;
    [ObservableProperty] private int _shortCharCount;
    [ObservableProperty] private int _mediumCharCount;
    [ObservableProperty] private int _longCharCount;
    [ObservableProperty] private string _selectedDescription = string.Empty;
    [ObservableProperty] private int _selectedIndex; // 0=short, 1=medium, 2=long

    // SEO
    [ObservableProperty] private string _seoTitle = string.Empty;
    [ObservableProperty] private ObservableCollection<string> _seoKeywords = [];
    [ObservableProperty] private int _seoScore;
    [ObservableProperty] private string _seoScoreColor = "#94A3B8";

    // Status
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _hasResults;

    public ProductDescriptionAIViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        Title = "AI Urun Aciklama";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var topProducts = await _mediator.Send(new GetTopProductsQuery(_currentUser.TenantId, 50), ct);
            Products.Clear();
            foreach (var p in topProducts)
                Products.Add(new ProductSelectItem(p.ProductId, p.Name, p.SKU));
            IsEmpty = Products.Count == 0;
        }, "Urunler yuklenirken hata");
    }

    partial void OnSelectedProductChanged(ProductSelectItem? value)
    {
        if (value is not null)
        {
            ProductName = value.Name;
            // Category and Brand would come from product detail query
        }
    }

    partial void OnShortDescriptionChanged(string value) => ShortCharCount = value.Length;
    partial void OnMediumDescriptionChanged(string value) => MediumCharCount = value.Length;
    partial void OnLongDescriptionChanged(string value) => LongCharCount = value.Length;

    [RelayCommand]
    private async Task GenerateAsync()
    {
        if (string.IsNullOrWhiteSpace(ProductName))
        {
            StatusMessage = "Lutfen bir urun secin veya urun adi girin.";
            return;
        }

        IsGenerating = true;
        HasResults = false;
        StatusMessage = "AI aciklama uretiyor...";
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var featureList = string.IsNullOrWhiteSpace(Features)
                ? null
                : (IReadOnlyList<string>)Features.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            var result = await _mediator.Send(new GenerateProductDescriptionCommand(
                ProductId: SelectedProduct?.Id ?? Guid.Empty,
                TenantId: _currentUser.TenantId,
                ProductName: ProductName,
                Category: string.IsNullOrWhiteSpace(Category) ? null : Category,
                Brand: string.IsNullOrWhiteSpace(Brand) ? null : Brand,
                Features: featureList));

            if (result.IsSuccess)
            {
                ShortDescription = result.ShortDescription;
                MediumDescription = result.MediumDescription;
                LongDescription = result.LongDescription;
                SeoTitle = result.SeoTitle ?? string.Empty;
                SeoKeywords = new ObservableCollection<string>(result.SeoKeywords);
                CalculateSeoScore();
                HasResults = true;
                SelectedIndex = 1; // Default: medium
                SelectedDescription = MediumDescription;
                StatusMessage = "Aciklama basariyla uretildi!";
            }
            else
            {
                HasError = true;
                ErrorMessage = result.ErrorMessage ?? "AI aciklama uretilemedi.";
                StatusMessage = string.Empty;
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"AI hatasi: {ex.Message}";
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private void SelectShort() { SelectedIndex = 0; SelectedDescription = ShortDescription; }

    [RelayCommand]
    private void SelectMedium() { SelectedIndex = 1; SelectedDescription = MediumDescription; }

    [RelayCommand]
    private void SelectLong() { SelectedIndex = 2; SelectedDescription = LongDescription; }

    [RelayCommand]
    private async Task RegenerateAsync() => await GenerateAsync();

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    private void CalculateSeoScore()
    {
        int score = 0;
        if (!string.IsNullOrEmpty(SeoTitle)) score += 20;
        if (SeoKeywords.Count >= 3) score += 20;
        if (MediumDescription.Length >= 150) score += 20;
        if (MediumDescription.Length >= 300) score += 10;
        if (LongDescription.Length >= 500) score += 15;
        if (ShortDescription.Length >= 50) score += 15;
        SeoScore = Math.Min(score, 100);
        SeoScoreColor = SeoScore >= 80 ? "#22C55E" : SeoScore >= 50 ? "#F59E0B" : "#EF4444";
    }
}

public record ProductSelectItem(Guid Id, string Name, string SKU);
