using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Kategori Eslestirme ViewModel — dual-tree ile lokal ve platform kategori eslestirme.
/// TODO: Replace demo data with MediatR.Send(new GetCategoryMappingsQuery()) when A1 CQRS is ready.
/// </summary>
public partial class CategoryMappingAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;

    [ObservableProperty] private string selectedLocalCategory = string.Empty;
    [ObservableProperty] private string selectedPlatformCategory = string.Empty;
    [ObservableProperty] private string selectedPlatform = "Trendyol";

    public ObservableCollection<CategoryNodeDto> LocalCategories { get; } = [];
    public ObservableCollection<CategoryNodeDto> PlatformCategories { get; } = [];
    public ObservableCollection<CategoryMappingItemDto> Mappings { get; } = [];
    public ObservableCollection<string> PlatformList { get; } = ["Trendyol", "Hepsiburada", "N11", "Amazon", "Ciceksepeti"];

    public CategoryMappingAvaloniaViewModel(IMediator mediator)
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
            // TODO: Replace with MediatR.Send(new GetCategoryMappingsQuery()) when A1 CQRS is ready
            await Task.Delay(300);

            LocalCategories.Clear();
            LocalCategories.Add(new CategoryNodeDto { Name = "Elektronik", Id = "L1" });
            LocalCategories.Add(new CategoryNodeDto { Name = "  Telefon", Id = "L1.1" });
            LocalCategories.Add(new CategoryNodeDto { Name = "  Bilgisayar", Id = "L1.2" });
            LocalCategories.Add(new CategoryNodeDto { Name = "Giyim", Id = "L2" });
            LocalCategories.Add(new CategoryNodeDto { Name = "  Erkek", Id = "L2.1" });
            LocalCategories.Add(new CategoryNodeDto { Name = "  Kadin", Id = "L2.2" });
            LocalCategories.Add(new CategoryNodeDto { Name = "Ev & Yasam", Id = "L3" });

            PlatformCategories.Clear();
            PlatformCategories.Add(new CategoryNodeDto { Name = "Elektronik", Id = "P1" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "  Cep Telefonu", Id = "P1.1" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "  Dizustu Bilgisayar", Id = "P1.2" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "Moda", Id = "P2" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "  Erkek Giyim", Id = "P2.1" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "  Kadin Giyim", Id = "P2.2" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "Ev & Dekorasyon", Id = "P3" });

            Mappings.Clear();
            Mappings.Add(new CategoryMappingItemDto { LocalCategory = "Telefon", PlatformCategory = "Cep Telefonu", Platform = "Trendyol" });
            Mappings.Add(new CategoryMappingItemDto { LocalCategory = "Bilgisayar", PlatformCategory = "Dizustu Bilgisayar", Platform = "Trendyol" });

            TotalCount = Mappings.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kategori eslestirmeleri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task MapCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(SelectedLocalCategory) || string.IsNullOrWhiteSpace(SelectedPlatformCategory))
            return;

        // TODO: Replace with MediatR.Send(new MapCategoryCommand()) when A1 CQRS is ready
        Mappings.Add(new CategoryMappingItemDto
        {
            LocalCategory = SelectedLocalCategory,
            PlatformCategory = SelectedPlatformCategory,
            Platform = SelectedPlatform
        });
        TotalCount = Mappings.Count;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class CategoryNodeDto
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

public class CategoryMappingItemDto
{
    public string LocalCategory { get; set; } = string.Empty;
    public string PlatformCategory { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
}
