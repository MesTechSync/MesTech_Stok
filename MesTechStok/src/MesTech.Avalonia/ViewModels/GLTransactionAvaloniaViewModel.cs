using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetChartOfAccounts;
using MesTech.Application.Features.Accounting.Queries.GetJournalEntries;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class GLTransactionAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;


    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedAccount = "Tum Hesaplar";
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;

    public ObservableCollection<GLTransactionItemDto> Transactions { get; } = [];
    private List<GLTransactionItemDto> _allItems = [];

    public ObservableCollection<string> Accounts { get; } = ["Tum Hesaplar"];

    public GLTransactionAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            // Load chart of accounts for filter dropdown
            var accounts = await _mediator.Send(new GetChartOfAccountsQuery(_currentUser.TenantId), ct);
            if (Accounts.Count <= 1)
            {
                foreach (var a in accounts)
                    Accounts.Add($"{a.Code} - {a.Name}");
            }

            var from = StartDate?.DateTime ?? DateTime.Today.AddMonths(-1);
            var to = EndDate?.DateTime ?? DateTime.Today;
            var entries = await _mediator.Send(new GetJournalEntriesQuery(_currentUser.TenantId, from, to), ct);

            _allItems = entries.SelectMany(e => e.Lines.Count > 0
                ? e.Lines.Select(l => new GLTransactionItemDto
                {
                    Date = e.EntryDate.ToString("dd.MM.yyyy"),
                    VoucherNo = e.ReferenceNumber ?? string.Empty,
                    Account = $"{l.AccountCode} - {l.AccountName}",
                    Description = l.Description ?? e.Description,
                    DebitFormatted = l.Debit > 0 ? l.Debit.ToString("N2") : string.Empty,
                    CreditFormatted = l.Credit > 0 ? l.Credit.ToString("N2") : string.Empty,
                })
                : [new GLTransactionItemDto
                {
                    Date = e.EntryDate.ToString("dd.MM.yyyy"),
                    VoucherNo = e.ReferenceNumber ?? string.Empty,
                    Description = e.Description,
                    DebitFormatted = e.TotalDebit > 0 ? e.TotalDebit.ToString("N2") : string.Empty,
                    CreditFormatted = e.TotalCredit > 0 ? e.TotalCredit.ToString("N2") : string.Empty,
                }]).ToList();

            ApplyFilters();
        }, "Muhasebe hareketleri yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedAccountChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x =>
                x.Description.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                x.VoucherNo.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedAccount != "Tum Hesaplar")
            filtered = filtered.Where(x => x.Account == SelectedAccount);

        Transactions.Clear();
        foreach (var item in filtered)
            Transactions.Add(item);

        TotalCount = Transactions.Count;
        IsEmpty = Transactions.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void AddTransaction()
    {
        // Will open add transaction dialog
    }
}

public class GLTransactionItemDto
{
    public string Date { get; set; } = string.Empty;
    public string VoucherNo { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DebitFormatted { get; set; } = string.Empty;
    public string CreditFormatted { get; set; } = string.Empty;
    public string BalanceFormatted { get; set; } = string.Empty;
}
