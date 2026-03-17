using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Category management ViewModel — DataGrid with hierarchical categories.
/// 12 demo categories with parent-child hierarchy (indented names).
/// </summary>
public partial class CategoryAvaloniaViewModel : ObservableObject
{
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<CategoryItemDto> Categories { get; } = [];

    private List<CategoryItemDto> _allCategories = [];

    public async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(300); // Simulate async load

            _allCategories =
            [
                new() { Name = "Elektronik", ParentCategory = "—", Platform = "Trendyol", ProductCount = 245 },
                new() { Name = "  Telefon", ParentCategory = "Elektronik", Platform = "Trendyol", ProductCount = 89 },
                new() { Name = "    Akilli Telefon", ParentCategory = "Telefon", Platform = "Trendyol", ProductCount = 62 },
                new() { Name = "  Bilgisayar", ParentCategory = "Elektronik", Platform = "Hepsiburada", ProductCount = 67 },
                new() { Name = "    Dizustu", ParentCategory = "Bilgisayar", Platform = "Hepsiburada", ProductCount = 34 },
                new() { Name = "Ev & Yasam", ParentCategory = "—", Platform = "N11", ProductCount = 312 },
                new() { Name = "  Mutfak Gerecleri", ParentCategory = "Ev & Yasam", Platform = "Ciceksepeti", ProductCount = 134 },
                new() { Name = "  Mobilya", ParentCategory = "Ev & Yasam", Platform = "Amazon", ProductCount = 56 },
                new() { Name = "Giyim", ParentCategory = "—", Platform = "Trendyol", ProductCount = 478 },
                new() { Name = "  Kadin Giyim", ParentCategory = "Giyim", Platform = "Trendyol", ProductCount = 256 },
                new() { Name = "  Erkek Giyim", ParentCategory = "Giyim", Platform = "N11", ProductCount = 189 },
                new() { Name = "Kozmetik", ParentCategory = "—", Platform = "Ciceksepeti", ProductCount = 201 },
            ];

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Kategoriler yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allCategories.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(c =>
                c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.ParentCategory.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                c.Platform.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        Categories.Clear();
        foreach (var item in filtered)
            Categories.Add(item);

        TotalCount = Categories.Count;
        IsEmpty = Categories.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (_allCategories.Count > 0)
            ApplyFilters();
    }
}

public class CategoryItemDto
{
    public string Name { get; set; } = string.Empty;
    public string ParentCategory { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public int ProductCount { get; set; }
}
