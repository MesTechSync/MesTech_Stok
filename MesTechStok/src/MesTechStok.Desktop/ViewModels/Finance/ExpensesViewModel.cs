using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Finance.Commands.ApproveExpense;
using MesTech.Domain.Interfaces;

namespace MesTechStok.Desktop.ViewModels.Finance;

/// <summary>
/// H29 — Gider listesi + onay workflow ViewModel.
/// MediatR uzerinden ApproveExpenseCommand gondererek gider onay sureci baslatir.
/// </summary>
public partial class ExpensesViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private decimal totalThisMonth;
    [ObservableProperty] private int pendingCount;
    [ObservableProperty] private int approvedCount;
    [ObservableProperty] private bool isLoading;

    public ObservableCollection<ExpenseItemVm> Expenses { get; } = [];

    public ExpensesViewModel(IMediator mediator, ICurrentUserService currentUser)
        => (_mediator, _currentUser) = (mediator, currentUser);

    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var now = DateTime.Today;
            // Placeholder: await _mediator.Send(new GetExpenseSummaryQuery(...))
            // Placeholder — will be wired when DEV 1 delivers GetExpenseSummaryQuery
            await Task.Delay(10);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task Approve(Guid expenseId)
    {
        var userId = _currentUser.UserId ?? Guid.Empty;
        await _mediator.Send(new ApproveExpenseCommand(expenseId, userId));
        await LoadAsync();
    }

    [RelayCommand]
    private void Reject(Guid expenseId)
        => System.Windows.MessageBox.Show($"Red #{expenseId} — H29 sonunda implement edilecek");

    [RelayCommand]
    private void CreateExpense()
        => System.Windows.MessageBox.Show("Yeni Gider formu — H29 sonunda implement edilecek");
}

public partial class ExpenseItemVm : ObservableObject
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#64748B";
    public bool CanApprove { get; set; }
}
