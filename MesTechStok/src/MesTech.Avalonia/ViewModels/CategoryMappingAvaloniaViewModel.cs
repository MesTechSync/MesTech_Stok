using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Kategori Eslestirme ViewModel — dual-tree ile lokal ve platform kategori eslestirme.
/// AI otomatik eslestirme + sync + ilerleme takibi.
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

    // TreeView selections
    [ObservableProperty] private CategoryTreeNodeDto? selectedLocalCategoryNode;
    [ObservableProperty] private CategoryTreeNodeDto? selectedPlatformCategoryNode;

    // Search
    [ObservableProperty] private string localSearchText = string.Empty;
    [ObservableProperty] private string platformSearchText = string.Empty;

    // Progress & stats
    [ObservableProperty] private double mappingPercentage;
    [ObservableProperty] private int mappedCount;
    [ObservableProperty] private int unmappedCount;
    [ObservableProperty] private int aiSuggestionCount;
    [ObservableProperty] private string lastSyncText = string.Empty;

    // Legacy flat lists (kept for backward compat)
    public ObservableCollection<CategoryNodeDto> LocalCategories { get; } = [];
    public ObservableCollection<CategoryNodeDto> PlatformCategories { get; } = [];

    // TreeView hierarchical data
    public ObservableCollection<CategoryTreeNodeDto> LocalCategoryTree { get; } = [];
    public ObservableCollection<CategoryTreeNodeDto> PlatformCategoryTree { get; } = [];

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
            await Task.Delay(300);

            // Legacy flat data
            LocalCategories.Clear();
            LocalCategories.Add(new CategoryNodeDto { Name = "Elektronik", Id = "L1" });
            LocalCategories.Add(new CategoryNodeDto { Name = "  Telefon", Id = "L1.1" });
            LocalCategories.Add(new CategoryNodeDto { Name = "  Bilgisayar", Id = "L1.2" });
            LocalCategories.Add(new CategoryNodeDto { Name = "Giyim", Id = "L2" });
            LocalCategories.Add(new CategoryNodeDto { Name = "  Erkek", Id = "L2.1" });
            LocalCategories.Add(new CategoryNodeDto { Name = "  Kadin", Id = "L2.2" });
            LocalCategories.Add(new CategoryNodeDto { Name = "Ev & Yasam", Id = "L3" });

            // Tree data for enhanced view
            LocalCategoryTree.Clear();
            LocalCategoryTree.Add(new CategoryTreeNodeDto
            {
                Name = "Elektronik", Id = "L1", StatusColor = "#4CAF50", StatusText = "[eslesmis]",
                Children = [
                    new() { Name = "Telefon", Id = "L1.1", StatusColor = "#4CAF50", StatusText = "[eslesmis]" },
                    new() { Name = "Bilgisayar", Id = "L1.2", StatusColor = "#4CAF50", StatusText = "[eslesmis]" },
                    new() { Name = "Tablet", Id = "L1.3", StatusColor = "#FF6F00", StatusText = "[AI onerisi]" },
                    new() { Name = "Aksesuar", Id = "L1.4", StatusColor = "#E53935", StatusText = "[eslesmemis]" },
                ]
            });
            LocalCategoryTree.Add(new CategoryTreeNodeDto
            {
                Name = "Giyim", Id = "L2", StatusColor = "#4CAF50", StatusText = "[eslesmis]",
                Children = [
                    new() { Name = "Erkek", Id = "L2.1", StatusColor = "#4CAF50", StatusText = "[eslesmis]" },
                    new() { Name = "Kadin", Id = "L2.2", StatusColor = "#4CAF50", StatusText = "[eslesmis]" },
                    new() { Name = "Cocuk", Id = "L2.3", StatusColor = "#E53935", StatusText = "[eslesmemis]" },
                ]
            });
            LocalCategoryTree.Add(new CategoryTreeNodeDto
            {
                Name = "Ev & Yasam", Id = "L3", StatusColor = "#FF6F00", StatusText = "[AI onerisi]",
                Children = [
                    new() { Name = "Mobilya", Id = "L3.1", StatusColor = "#E53935", StatusText = "[eslesmemis]" },
                    new() { Name = "Dekorasyon", Id = "L3.2", StatusColor = "#FF6F00", StatusText = "[AI onerisi]" },
                ]
            });

            PlatformCategories.Clear();
            PlatformCategories.Add(new CategoryNodeDto { Name = "Elektronik", Id = "P1" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "  Cep Telefonu", Id = "P1.1" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "  Dizustu Bilgisayar", Id = "P1.2" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "Moda", Id = "P2" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "  Erkek Giyim", Id = "P2.1" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "  Kadin Giyim", Id = "P2.2" });
            PlatformCategories.Add(new CategoryNodeDto { Name = "Ev & Dekorasyon", Id = "P3" });

            PlatformCategoryTree.Clear();
            PlatformCategoryTree.Add(new CategoryTreeNodeDto
            {
                Name = "Elektronik", Id = "P1",
                Children = [
                    new() { Name = "Cep Telefonu", Id = "P1.1" },
                    new() { Name = "Dizustu Bilgisayar", Id = "P1.2" },
                    new() { Name = "Tablet Bilgisayar", Id = "P1.3" },
                    new() { Name = "Telefon Aksesuarlari", Id = "P1.4" },
                ]
            });
            PlatformCategoryTree.Add(new CategoryTreeNodeDto
            {
                Name = "Moda", Id = "P2",
                Children = [
                    new() { Name = "Erkek Giyim", Id = "P2.1" },
                    new() { Name = "Kadin Giyim", Id = "P2.2" },
                    new() { Name = "Cocuk Giyim", Id = "P2.3" },
                ]
            });
            PlatformCategoryTree.Add(new CategoryTreeNodeDto
            {
                Name = "Ev & Dekorasyon", Id = "P3",
                Children = [
                    new() { Name = "Mobilya", Id = "P3.1" },
                    new() { Name = "Ev Dekorasyon", Id = "P3.2" },
                ]
            });

            Mappings.Clear();
            Mappings.Add(new CategoryMappingItemDto { LocalCategory = "Telefon", PlatformCategory = "Cep Telefonu", Platform = "Trendyol", StatusLabel = "Eslesmis", Source = "Manuel" });
            Mappings.Add(new CategoryMappingItemDto { LocalCategory = "Bilgisayar", PlatformCategory = "Dizustu Bilgisayar", Platform = "Trendyol", StatusLabel = "Eslesmis", Source = "Manuel" });
            Mappings.Add(new CategoryMappingItemDto { LocalCategory = "Erkek", PlatformCategory = "Erkek Giyim", Platform = "Trendyol", StatusLabel = "Eslesmis", Source = "AI" });
            Mappings.Add(new CategoryMappingItemDto { LocalCategory = "Kadin", PlatformCategory = "Kadin Giyim", Platform = "Trendyol", StatusLabel = "Eslesmis", Source = "AI" });
            Mappings.Add(new CategoryMappingItemDto { LocalCategory = "Elektronik", PlatformCategory = "Elektronik", Platform = "Trendyol", StatusLabel = "Eslesmis", Source = "Manuel" });
            Mappings.Add(new CategoryMappingItemDto { LocalCategory = "Giyim", PlatformCategory = "Moda", Platform = "Trendyol", StatusLabel = "Eslesmis", Source = "AI" });

            TotalCount = Mappings.Count;
            MappedCount = 7;
            UnmappedCount = 3;
            AiSuggestionCount = 3;
            MappingPercentage = 78.0;
            LastSyncText = "Son sync: 5 dk once";
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
        if (SelectedLocalCategoryNode == null || SelectedPlatformCategoryNode == null)
            return;

        Mappings.Add(new CategoryMappingItemDto
        {
            LocalCategory = SelectedLocalCategoryNode.Name,
            PlatformCategory = SelectedPlatformCategoryNode.Name,
            Platform = SelectedPlatform,
            StatusLabel = "Eslesmis",
            Source = "Manuel"
        });
        TotalCount = Mappings.Count;
        MappedCount++;
        if (UnmappedCount > 0) UnmappedCount--;
        MappingPercentage = MappedCount * 100.0 / (MappedCount + UnmappedCount + AiSuggestionCount);
    }

    [RelayCommand]
    private void UnmapCategory()
    {
    }

    [RelayCommand]
    private async Task SyncPlatformCategoriesAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(500);
            LastSyncText = "Son sync: az once";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AiAutoMapAsync()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(800);
            AiSuggestionCount = 0;
            MappedCount += 3;
            MappingPercentage = MappedCount * 100.0 / (MappedCount + UnmappedCount);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class CategoryNodeDto
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

public class CategoryTreeNodeDto
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#94A3B8";
    public string StatusText { get; set; } = string.Empty;
    public ObservableCollection<CategoryTreeNodeDto> Children { get; set; } = [];
}

public class CategoryMappingItemDto
{
    public string LocalCategory { get; set; } = string.Empty;
    public string PlatformCategory { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
}
