using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetCounterparties;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Cari Hesap (Account) ViewModel — wired to GetCounterpartiesQuery via MediatR.
/// G033: Task.Delay mock replaced with real mediator.Send call.
/// DataGrid: Ad, Tip, Borc, Alacak, Bakiye with Musteri/Tedarikci filter.
/// </summary>
public partial class CariAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedType = "Tumu";
    [ObservableProperty] private int totalCount;

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    public ObservableCollection<CariItemDto> Accounts { get; } = [];

    public ObservableCollection<string> AccountTypes { get; } =
    [
        "Tumu", "Musteri", "Tedarikci"
    ];

    private List<CariItemDto> _allAccounts = [];

    public CariAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var results = await _mediator.Send(
                new GetCounterpartiesQuery(_currentUser.TenantId),
                ct);

            _allAccounts = results.Select(c => new CariItemDto
            {
                Name = c.Name,
                Type = c.CounterpartyType switch
                {
                    "Customer" => "Musteri",
                    "Supplier" => "Tedarikci",
                    _ => c.CounterpartyType
                },
                Debt = 0m,
                Credit = 0m,
                Balance = 0m
            }).ToList();

            ApplyFilters();
        }, "Cari hesaplar yuklenirken hata");
    }

    private void ApplyFilters()
    {
        var filtered = _allAccounts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var search = SearchText.ToLowerInvariant();
            filtered = filtered.Where(a =>
                a.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedType != "Tumu")
        {
            filtered = filtered.Where(a => a.Type == SelectedType);
        }

        // Sort
        var sortedList = SortColumn switch
        {
            "Name"    => SortAscending ? filtered.OrderBy(x => x.Name).ToList()    : filtered.OrderByDescending(x => x.Name).ToList(),
            "Type"    => SortAscending ? filtered.OrderBy(x => x.Type).ToList()    : filtered.OrderByDescending(x => x.Type).ToList(),
            "Debt"    => SortAscending ? filtered.OrderBy(x => x.Debt).ToList()    : filtered.OrderByDescending(x => x.Debt).ToList(),
            "Credit"  => SortAscending ? filtered.OrderBy(x => x.Credit).ToList()  : filtered.OrderByDescending(x => x.Credit).ToList(),
            "Balance" => SortAscending ? filtered.OrderBy(x => x.Balance).ToList() : filtered.OrderByDescending(x => x.Balance).ToList(),
            _         => SortAscending ? filtered.OrderBy(x => x.Name).ToList()    : filtered.OrderByDescending(x => x.Name).ToList(),
        };

        Accounts.Clear();
        foreach (var item in sortedList)
            Accounts.Add(item);

        TotalCount = Accounts.Count;
        IsEmpty = Accounts.Count == 0;
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
            var result = await _mediator.Send(new ExportReportCommand(Guid.Empty, "accounts", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Cari hesaplar disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    partial void OnSearchTextChanged(string value)
    {
        if (_allAccounts.Count > 0)
            ApplyFilters();
    }

    partial void OnSelectedTypeChanged(string value)
    {
        if (_allAccounts.Count > 0)
            ApplyFilters();
    }
}

public class CariItemDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Debt { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}
