using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Queries.GetExpenses;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Expenses ViewModel — wired to GetExpensesQuery via MediatR.
/// </summary>
public partial class ExpensesAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;
    private List<ExpenseDto> _allExpenses = [];

    [ObservableProperty] private ObservableCollection<ExpenseDto> expenses = [];
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private decimal totalAmount;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string searchText = string.Empty;

    public ExpensesAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
        Title = "Giderler";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetExpensesQuery(TenantId: _currentUser.TenantId), ct);

            _allExpenses = result.ToList();
            ApplyFilter();
        }, "Giderler yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allExpenses
            : _allExpenses.Where(e =>
                e.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
              .ToList();

        Expenses = new ObservableCollection<ExpenseDto>(filtered);
        TotalCount = filtered.Count;
        TotalAmount = filtered.Sum(e => e.Amount);
        Summary = $"Toplam {TotalCount} gider — {TotalAmount:N2} ₺";
        IsEmpty = TotalCount == 0;
    }

    [RelayCommand]
    private async Task Add()
    {
        await _dialog.ShowInfoAsync("Bu özellik yakinda aktif olacak.", "MesTech");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
