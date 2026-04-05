using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class KomisyonAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;

    // KPI — Platform averages
    [ObservableProperty] private string trendyolAvgRate = "%0.0";
    [ObservableProperty] private string hepsiburadaAvgRate = "%0.0";
    [ObservableProperty] private string ciceksepetiAvgRate = "%0.0";
    [ObservableProperty] private string n11AvgRate = "%0.0";

    // Filters
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private string selectedCategory = "Tumu";
    [ObservableProperty] private string searchText = string.Empty;

    public ObservableCollection<CommissionItemDto> Items { get; } = [];
    private List<CommissionItemDto> _allItems = [];

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    public ObservableCollection<string> Platforms { get; } =
        ["Tumu", "Trendyol", "Hepsiburada", "Ciceksepeti", "N11", "Amazon", "Pazarama"];

    public ObservableCollection<string> Categories { get; } =
        ["Tumu", "Giyim", "Elektronik", "Ev & Yasam", "Kozmetik", "Gida", "Diger"];

    public KomisyonAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetCommissionSummaryQuery(
                _currentUser.TenantId, DateTime.UtcNow.AddMonths(-3), DateTime.UtcNow), ct);

            _allItems = result.ByPlatform.Select(p => new CommissionItemDto
            {
                Platform = p.Platform,
                Category = "-",
                RateFormatted = $"%{p.AverageRate:F1}",
                FixedFeeFormatted = $"{p.TotalCommission:N2} TL",
                ValidFrom = DateTime.UtcNow.ToString("dd.MM.yyyy")
            }).ToList();

            var lookup = result.ByPlatform.ToDictionary(p => p.Platform.ToLowerInvariant(), p => p.AverageRate);
            TrendyolAvgRate = lookup.TryGetValue("trendyol", out var tr) ? $"%{tr:F1}" : "%0.0";
            HepsiburadaAvgRate = lookup.TryGetValue("hepsiburada", out var hb) ? $"%{hb:F1}" : "%0.0";
            CiceksepetiAvgRate = lookup.TryGetValue("ciceksepeti", out var cs) ? $"%{cs:F1}" : "%0.0";
            N11AvgRate = lookup.TryGetValue("n11", out var n11) ? $"%{n11:F1}" : "%0.0";

            ApplyFilters();
        }, "Komisyon verileri yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedPlatformChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x =>
                x.Platform.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                x.Category.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedPlatform != "Tumu")
            filtered = filtered.Where(x => x.Platform == SelectedPlatform);

        if (SelectedCategory != "Tumu")
            filtered = filtered.Where(x => x.Category == SelectedCategory);

        // Sort
        var sortedList = SortColumn switch
        {
            "Platform"  => SortAscending ? filtered.OrderBy(x => x.Platform).ToList()         : filtered.OrderByDescending(x => x.Platform).ToList(),
            "Category"  => SortAscending ? filtered.OrderBy(x => x.Category).ToList()         : filtered.OrderByDescending(x => x.Category).ToList(),
            "ValidFrom" => SortAscending ? filtered.OrderBy(x => x.ValidFrom).ToList()        : filtered.OrderByDescending(x => x.ValidFrom).ToList(),
            _           => SortAscending ? filtered.OrderBy(x => x.Platform).ToList()         : filtered.OrderByDescending(x => x.Platform).ToList(),
        };

        Items.Clear();
        foreach (var item in sortedList)
            Items.Add(item);

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilters();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(Guid.Empty, "commissions", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Komisyon verileri disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void AddCommission()
    {
        // Will open add commission dialog
    }
}

public class CommissionItemDto
{
    public string Platform { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string RateFormatted { get; set; } = string.Empty;
    public string FixedFeeFormatted { get; set; } = string.Empty;
    public string ValidFrom { get; set; } = string.Empty;
}
