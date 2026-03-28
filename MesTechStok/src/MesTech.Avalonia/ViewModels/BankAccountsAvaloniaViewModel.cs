using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Finance.Queries.GetBankAccounts;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Bank Accounts ViewModel — wired to GetBankAccountsQuery via MediatR.
/// </summary>
public partial class BankAccountsAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private List<BankAccountDto> _allAccounts = [];

    [ObservableProperty] private ObservableCollection<BankAccountDto> accounts = [];
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private decimal totalBalance;

    public BankAccountsAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
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

        Accounts = new ObservableCollection<BankAccountDto>(filtered);
        TotalCount = filtered.Count;
        TotalBalance = filtered.Where(a => a.IsActive).Sum(a => a.Balance);
        Summary = $"{TotalCount} hesap — {TotalBalance:N2} ₺";
        IsEmpty = TotalCount == 0;
    }

    [RelayCommand]
    private async Task Add()
    {
        // TODO: Open bank account create dialog
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
