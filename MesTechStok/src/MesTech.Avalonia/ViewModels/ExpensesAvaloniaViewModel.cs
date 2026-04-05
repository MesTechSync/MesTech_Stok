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

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

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
        var filtered = (string.IsNullOrWhiteSpace(SearchText)
            ? _allExpenses
            : _allExpenses.Where(e =>
                e.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
              .ToList()).AsEnumerable();

        // Sort
        filtered = SortColumn switch
        {
            "Description" => SortAscending ? filtered.OrderBy(x => x.Description)   : filtered.OrderByDescending(x => x.Description),
            "Amount"      => SortAscending ? filtered.OrderBy(x => x.Amount)        : filtered.OrderByDescending(x => x.Amount),
            "ExpenseType" => SortAscending ? filtered.OrderBy(x => x.ExpenseType)   : filtered.OrderByDescending(x => x.ExpenseType),
            "Date"        => SortAscending ? filtered.OrderBy(x => x.Date)          : filtered.OrderByDescending(x => x.Date),
            _             => SortAscending ? filtered.OrderByDescending(x => x.Date) : filtered.OrderBy(x => x.Date),
        };

        var sortedList = filtered.ToList();
        Expenses = new ObservableCollection<ExpenseDto>(sortedList);
        TotalCount = sortedList.Count;
        TotalAmount = sortedList.Sum(e => e.Amount);
        Summary = $"Toplam {TotalCount} gider — {TotalAmount:N2} ₺";
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
        var newExpense = new ExpenseDto
        {
            Id = Guid.NewGuid(),
            TenantId = _currentUser.TenantId,
            Description = "Yeni Gider",
            Amount = 0,
            ExpenseType = Domain.Enums.ExpenseType.Diger,
            Date = DateTime.Now
        };
        _allExpenses.Insert(0, newExpense);
        ApplyFilter();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
