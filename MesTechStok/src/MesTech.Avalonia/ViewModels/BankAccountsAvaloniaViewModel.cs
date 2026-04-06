using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Finance.Queries.GetBankAccounts;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Bank Accounts ViewModel — wired to GetBankAccountsQuery via MediatR.
/// </summary>
public partial class BankAccountsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;
    private List<BankAccountDto> _allAccounts = [];

    [ObservableProperty] private ObservableCollection<BankAccountDto> accounts = [];
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private decimal totalBalance;

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    public BankAccountsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
        Title = "Banka Hesaplari";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetBankAccountsQuery(_currentUser.TenantId), ct);

            _allAccounts = result.ToList();
            ApplyFilter();
        }, "Banka hesaplari yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allAccounts
            : _allAccounts.Where(a =>
                a.BankName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                a.AccountNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (a.IBAN ?? "").Contains(SearchText, StringComparison.OrdinalIgnoreCase))
              .ToList();

        // Sort
        var sortedList = SortColumn switch
        {
            "BankName"      => SortAscending ? filtered.OrderBy(x => x.BankName).ToList()      : filtered.OrderByDescending(x => x.BankName).ToList(),
            "AccountNumber" => SortAscending ? filtered.OrderBy(x => x.AccountNumber).ToList() : filtered.OrderByDescending(x => x.AccountNumber).ToList(),
            "Balance"       => SortAscending ? filtered.OrderBy(x => x.Balance).ToList()       : filtered.OrderByDescending(x => x.Balance).ToList(),
            "CurrencyCode"  => SortAscending ? filtered.OrderBy(x => x.CurrencyCode).ToList()  : filtered.OrderByDescending(x => x.CurrencyCode).ToList(),
            _               => SortAscending ? filtered.OrderBy(x => x.BankName).ToList()      : filtered.OrderByDescending(x => x.BankName).ToList(),
        };

        Accounts = new ObservableCollection<BankAccountDto>(sortedList);
        TotalCount = sortedList.Count;
        TotalBalance = sortedList.Where(a => a.IsActive).Sum(a => a.Balance);
        Summary = $"{TotalCount} hesap — {TotalBalance:N2} ₺";
        IsEmpty = TotalCount == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilter();
    }

    [RelayCommand]
    private async Task Add()
    {
        var newAccount = new BankAccountDto
        {
            Id = Guid.NewGuid(),
            BankName = "Yeni Banka",
            AccountNumber = string.Empty,
            CurrencyCode = "TRY",
            Balance = 0,
            IsActive = true
        };
        _allAccounts.Insert(0, newAccount);
        ApplyFilter();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
