using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Queries.GetCategories;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Category management ViewModel — wired to GetCategoriesQuery via MediatR.
/// Displays hierarchical categories with parent-child indentation.
/// </summary>
public partial class CategoryAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private CategoryItemDto? selectedCategory;

    // HH-FIX-026: CRUD form
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string editCategoryName = string.Empty;
    [ObservableProperty] private string editParentCategory = string.Empty;

    public ObservableCollection<CategoryItemDto> Categories { get; } = [];

    private List<CategoryItemDto> _allCategories = [];

    public CategoryAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetCategoriesQuery(ActiveOnly: true), ct) ?? [];

            // Build parent name lookup for hierarchy display
            var lookup = result.ToDictionary(c => c.Id, c => c.Name);

            _allCategories = result.Select(c => new CategoryItemDto
            {
                Name = c.ParentCategoryId.HasValue ? $"  {c.Name}" : c.Name,
                ParentCategory = c.ParentCategoryId.HasValue && lookup.TryGetValue(c.ParentCategoryId.Value, out var pn)
                    ? pn : "—",
                Platform = "-",
                ProductCount = 0
            }).ToList();

            ApplyFilters();
        }, "Kategoriler yuklenirken hata");
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

    // HH-FIX-026: Add category
    [RelayCommand]
    private void AddCategory()
    {
        EditCategoryName = string.Empty;
        EditParentCategory = string.Empty;
        SelectedCategory = null;
        IsEditing = true;
    }

    // HH-FIX-026: Edit category
    [RelayCommand]
    private void EditCategory()
    {
        if (SelectedCategory is null) return;
        EditCategoryName = SelectedCategory.Name.TrimStart();
        EditParentCategory = SelectedCategory.ParentCategory;
        IsEditing = true;
    }

    // HH-FIX-026: Save
    [RelayCommand]
    private async Task SaveCategory()
    {
        if (string.IsNullOrWhiteSpace(EditCategoryName)) return;
        // TODO: CreateCategoryCommand / UpdateCategoryCommand — DEV1 handler gerekli
        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void CancelEdit() => IsEditing = false;

    // HH-FIX-026: Delete
    [RelayCommand]
    private void DeleteCategory()
    {
        if (SelectedCategory is null) return;
        _allCategories.Remove(SelectedCategory);
        SelectedCategory = null;
        ApplyFilters();
        // TODO: DeleteCategoryCommand — DEV1 handler gerekli
    }

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
