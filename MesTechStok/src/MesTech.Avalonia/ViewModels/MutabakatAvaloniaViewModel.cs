using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class MutabakatAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    // KPI
    [ObservableProperty] private string totalRecords = "0";
    [ObservableProperty] private string matchedCount = "0";
    [ObservableProperty] private string unmatchedCount = "0";
    [ObservableProperty] private string matchScoreText = "%0";

    // Filters
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedSource = "Tumu";
    [ObservableProperty] private string selectedStatusFilter = "Tumu";

    public ObservableCollection<MutabakatItemDto> Items { get; } = [];
    private List<MutabakatItemDto> _allItems = [];

    public ObservableCollection<string> Sources { get; } =
        ["Tumu", "Banka - Garanti", "Banka - Isbank", "Cari - Trendyol", "Cari - Hepsiburada", "Cari - N11"];

    public ObservableCollection<string> StatusFilters { get; } =
        ["Tumu", "Eslesti", "Eslesmedi", "Beklemede"];

    public MutabakatAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR query

            _allItems =
            [
                new() { Date = "19.03.2026", Reference = "TR-2026-1842", Source = "Cari - Trendyol", Description = "Trendyol hakedis odemesi", AmountFormatted = "4.520,00 TL", Status = "Eslesti" },
                new() { Date = "18.03.2026", Reference = "BNK-00341", Source = "Banka - Garanti", Description = "EFT gelen - Trendyol", AmountFormatted = "4.520,00 TL", Status = "Eslesti" },
                new() { Date = "18.03.2026", Reference = "HB-2026-0923", Source = "Cari - Hepsiburada", Description = "Hepsiburada hakedis odemesi", AmountFormatted = "2.180,00 TL", Status = "Eslesti" },
                new() { Date = "17.03.2026", Reference = "N11-2026-0412", Source = "Cari - N11", Description = "N11 hakedis odemesi", AmountFormatted = "1.240,00 TL", Status = "Eslesmedi" },
                new() { Date = "17.03.2026", Reference = "BNK-00338", Source = "Banka - Isbank", Description = "EFT gelen - bilinmiyor", AmountFormatted = "1.180,00 TL", Status = "Eslesmedi" },
                new() { Date = "16.03.2026", Reference = "TR-2026-1790", Source = "Cari - Trendyol", Description = "Trendyol iade iadesi", AmountFormatted = "-320,00 TL", Status = "Beklemede" },
            ];

            ApplyFilters();
            UpdateKpis();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Mutabakat verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateKpis()
    {
        var total = _allItems.Count;
        var matched = _allItems.Count(x => x.Status == "Eslesti");
        var unmatched = _allItems.Count(x => x.Status == "Eslesmedi");
        var score = total > 0 ? (double)matched / total * 100 : 0;

        TotalRecords = total.ToString();
        MatchedCount = matched.ToString();
        UnmatchedCount = unmatched.ToString();
        MatchScoreText = $"%{score:N0}";
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedSourceChanged(string value) => ApplyFilters();
    partial void OnSelectedStatusFilterChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x =>
                x.Description.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                x.Reference.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedSource != "Tumu")
            filtered = filtered.Where(x => x.Source == SelectedSource);

        if (SelectedStatusFilter != "Tumu")
            filtered = filtered.Where(x => x.Status == SelectedStatusFilter);

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task AutoMatch()
    {
        IsLoading = true;
        try
        {
            await Task.Delay(500); // Will be replaced with MediatR command
            // Auto-match logic placeholder
            await LoadAsync();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Otomatik eslestirme hatasi: {ex.Message}";
            IsLoading = false;
        }
    }
}

public class MutabakatItemDto
{
    public string Date { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AmountFormatted { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
