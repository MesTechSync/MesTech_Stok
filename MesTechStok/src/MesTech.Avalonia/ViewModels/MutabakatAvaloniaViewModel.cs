using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationDashboard;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationMatches;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class MutabakatAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    // KPI
    [ObservableProperty] private string totalRecords = "0";
    [ObservableProperty] private string matchedCount = "0";
    [ObservableProperty] private string unmatchedCount = "0";
    [ObservableProperty] private string matchScoreText = "%0";
    [ObservableProperty] private int pendingMatchCount;

    // Filters
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedSource = "Tumu";
    [ObservableProperty] private string selectedStatusFilter = "Tumu";

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    public ObservableCollection<MutabakatItemDto> Items { get; } = [];
    private List<MutabakatItemDto> _allItems = [];

    public ObservableCollection<string> Sources { get; } =
        ["Tumu", "Banka - Garanti", "Banka - Isbank", "Cari - Trendyol", "Cari - Hepsiburada", "Cari - N11"];

    public ObservableCollection<string> StatusFilters { get; } =
        ["Tumu", "Eslesti", "Eslesmedi", "Beklemede"];

    public MutabakatAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var dashboard = await _mediator.Send(new GetReconciliationDashboardQuery(_currentUser.TenantId), ct);

            var total = dashboard.AutoMatchedCount + dashboard.NeedsReviewCount + dashboard.UnmatchedCount;
            var score = total > 0 ? (double)dashboard.AutoMatchedCount / total * 100 : 0;

            TotalRecords = total.ToString();
            MatchedCount = dashboard.AutoMatchedCount.ToString();
            UnmatchedCount = dashboard.UnmatchedCount.ToString();
            MatchScoreText = $"%{score:N0}";

            _allItems = [];
            if (dashboard.AutoMatchedCount > 0)
                _allItems.Add(new() { Date = DateTime.Now.ToString("dd.MM.yyyy"), Reference = "ESLESTIRME", Source = "Otomatik", Description = $"{dashboard.AutoMatchedCount} kayit eslesti", AmountFormatted = $"{dashboard.AutoMatchedTotal:N2} TL", Status = "Eslesti" });
            if (dashboard.NeedsReviewCount > 0)
                _allItems.Add(new() { Date = DateTime.Now.ToString("dd.MM.yyyy"), Reference = "INCELEME", Source = "Manuel", Description = $"{dashboard.NeedsReviewCount} kayit inceleme bekliyor", AmountFormatted = $"{dashboard.NeedsReviewTotal:N2} TL", Status = "Beklemede" });
            if (dashboard.UnmatchedCount > 0)
                _allItems.Add(new() { Date = DateTime.Now.ToString("dd.MM.yyyy"), Reference = "ESLESMEDI", Source = "Sistem", Description = $"{dashboard.UnmatchedCount} kayit eslesmedi", AmountFormatted = $"{dashboard.UnmatchedTotal:N2} TL", Status = "Eslesmedi" });

            IsEmpty = total == 0;

            // Reconciliation matches (G540 orphan wire)
            try
            {
                var matches = await _mediator.Send(new GetReconciliationMatchesQuery(_currentUser.TenantId), ct);
                PendingMatchCount = matches.Count;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] GetReconciliationMatches failed: {ex.Message}"); PendingMatchCount = 0; }

            ApplyFilters();
        }, "Mutabakat verileri yuklenirken hata");
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

        // Sort
        var sortedList = SortColumn switch
        {
            "Date"        => SortAscending ? filtered.OrderBy(x => x.Date).ToList()        : filtered.OrderByDescending(x => x.Date).ToList(),
            "Reference"   => SortAscending ? filtered.OrderBy(x => x.Reference).ToList()   : filtered.OrderByDescending(x => x.Reference).ToList(),
            "Source"      => SortAscending ? filtered.OrderBy(x => x.Source).ToList()      : filtered.OrderByDescending(x => x.Source).ToList(),
            "Description" => SortAscending ? filtered.OrderBy(x => x.Description).ToList() : filtered.OrderByDescending(x => x.Description).ToList(),
            "Status"      => SortAscending ? filtered.OrderBy(x => x.Status).ToList()      : filtered.OrderByDescending(x => x.Status).ToList(),
            _             => SortAscending ? filtered.OrderBy(x => x.Date).ToList()        : filtered.OrderByDescending(x => x.Date).ToList(),
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
            var result = await _mediator.Send(new ExportReportCommand(Guid.Empty, "reconciliation", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Mutabakat verileri disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task AutoMatch()
    {
        // LoadAsync already handles IsLoading + try/catch/finally
        await LoadAsync();
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
