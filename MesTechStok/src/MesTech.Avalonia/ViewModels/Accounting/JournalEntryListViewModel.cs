using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetJournalEntries;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels.Accounting;

/// <summary>
/// Yevmiye Defteri ViewModel — Chain 3 GL kayıtları.
/// Wired to GetJournalEntriesQuery via MediatR.
/// </summary>
public partial class JournalEntryListViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    private List<JournalEntryItem> _allEntries = [];

    [ObservableProperty] private DateTimeOffset? fromDate = DateTimeOffset.Now.AddMonths(-1);
    [ObservableProperty] private DateTimeOffset? toDate = DateTimeOffset.Now;
    [ObservableProperty] private string selectedSourceType = "Tümü";

    // Search
    [ObservableProperty] private string searchText = string.Empty;

    // Sort
    [ObservableProperty] private string sortColumn = "date";
    [ObservableProperty] private bool sortAscending = false;

    // Date range quick filter
    [ObservableProperty] private string selectedDateRange = "Bu Ay";
    public string[] DateRangeOptions { get; } = ["Tumu", "Bugun", "Bu Hafta", "Bu Ay", "Son 3 Ay"];

    public ObservableCollection<string> SourceTypes { get; } =
        ["Tümü", "Satış", "Alış", "İade", "Komisyon", "Kargo", "Manuel"];

    public ObservableCollection<JournalEntryItem> JournalEntries { get; } = [];

    public JournalEntryListViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async () =>
        {
            var from = FromDate?.DateTime ?? DateTime.Now.AddMonths(-1);
            var to = ToDate?.DateTime ?? DateTime.Now;
            var results = await _mediator.Send(
                new GetJournalEntriesQuery(_tenantProvider.GetCurrentTenantId(), from, to), CancellationToken);

            _allEntries = results.Select(dto => new JournalEntryItem(
                dto.ReferenceNumber ?? $"YEV-{dto.Id:N}".Substring(0, 14),
                dto.EntryDate,
                dto.Description,
                dto.Lines.FirstOrDefault()?.AccountName ?? "Manuel",
                dto.TotalDebit,
                dto.TotalCredit)).ToList();

            ApplyFilters();
        }, "Yevmiye defteri");
    }

    private void ApplyFilters()
    {
        var filtered = _allEntries.AsEnumerable();

        // Source type filter
        if (SelectedSourceType != "Tümü")
            filtered = filtered.Where(e => e.SourceType == SelectedSourceType);

        // Search filter (description or account name)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim().ToLowerInvariant();
            filtered = filtered.Where(e =>
                e.Description.Contains(term, StringComparison.InvariantCultureIgnoreCase) ||
                e.SourceType.Contains(term, StringComparison.InvariantCultureIgnoreCase) ||
                e.EntryNumber.Contains(term, StringComparison.InvariantCultureIgnoreCase));
        }

        // Sort
        filtered = SortColumn switch
        {
            "date"   => SortAscending ? filtered.OrderBy(e => e.EntryDate)  : filtered.OrderByDescending(e => e.EntryDate),
            "amount" => SortAscending ? filtered.OrderBy(e => e.TotalDebit) : filtered.OrderByDescending(e => e.TotalDebit),
            "source" => SortAscending ? filtered.OrderBy(e => e.SourceType) : filtered.OrderByDescending(e => e.SourceType),
            _        => filtered.OrderByDescending(e => e.EntryDate)
        };

        JournalEntries.Clear();
        foreach (var item in filtered)
            JournalEntries.Add(item);

        IsEmpty = JournalEntries.Count == 0;
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnSelectedSourceTypeChanged(string value) => ApplyFilters();

    partial void OnSelectedDateRangeChanged(string value)
    {
        var now = DateTimeOffset.Now;
        (FromDate, ToDate) = value switch
        {
            "Bugun"    => (now.Date, now),
            "Bu Hafta" => (now.AddDays(-(int)now.DayOfWeek), now),
            "Bu Ay"    => (new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, now.Offset), now),
            "Son 3 Ay" => (now.AddMonths(-3), now),
            _          => ((DateTimeOffset?)null, (DateTimeOffset?)null)
        };
        _ = LoadAsync();
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
        ApplyFilters();
    }

    [RelayCommand]
    private async Task FilterAsync() => await LoadAsync();
}

public class JournalEntryItem
{
    public JournalEntryItem(string entryNumber, DateTime date, string description,
        string sourceType, decimal debit, decimal credit)
    {
        EntryNumber = entryNumber;
        EntryDate = date;
        Description = description;
        SourceType = sourceType;
        TotalDebit = debit;
        TotalCredit = credit;
    }

    public string EntryNumber { get; }
    public DateTime EntryDate { get; }
    public string Description { get; }
    public string SourceType { get; }
    public decimal TotalDebit { get; }
    public decimal TotalCredit { get; }

    public string EntryDateText => EntryDate.ToString("dd.MM.yyyy");
    public string SourceTypeText => SourceType;
    public string TotalDebitText => TotalDebit.ToString("N2");
    public string TotalCreditText => TotalCredit.ToString("N2");
    public string BalanceIcon => Math.Abs(TotalDebit - TotalCredit) < 0.01m ? "✓" : "✗";
}
