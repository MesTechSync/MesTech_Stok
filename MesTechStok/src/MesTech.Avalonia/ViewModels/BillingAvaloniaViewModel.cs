using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Billing.Queries.GetBillingInvoices;
using MesTech.Application.Features.Billing.Queries.GetTenantSubscription;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class BillingAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string currentPlan = "Pro";
    [ObservableProperty] private string monthlyFee = "0,00 TL";
    [ObservableProperty] private string nextBillingDate = "-";

    // Filters
    [ObservableProperty] private string searchText = string.Empty;

    public ObservableCollection<BillingInvoiceItemDto> Items { get; } = [];
    private List<BillingInvoiceItemDto> _allItems = [];

    public BillingAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var subTask = _mediator.Send(new GetTenantSubscriptionQuery(_currentUser.TenantId));
            var invTask = _mediator.Send(new GetBillingInvoicesQuery(_currentUser.TenantId));
            await Task.WhenAll(subTask, invTask);

            var subscription = await subTask;
            var invoices = await invTask;

            CurrentPlan = subscription?.PlanName ?? "—";
            NextBillingDate = subscription?.NextBillingDate?.ToString("yyyy-MM-dd") ?? "—";

            _allItems = invoices.Select(inv => new BillingInvoiceItemDto
            {
                InvoiceNumber = inv.InvoiceNumber,
                Period = inv.IssueDate.ToString("MMMM yyyy"),
                Amount = inv.TotalAmount,
                AmountFormatted = $"{inv.TotalAmount:N2} {inv.CurrencyCode}",
                Status = inv.Status.ToString(),
                DueDate = inv.DueDate.ToString("yyyy-MM-dd"),
                Plan = CurrentPlan
            }).ToList();

            if (_allItems.Count > 0)
                MonthlyFee = _allItems[0].AmountFormatted;

            IsEmpty = _allItems.Count == 0;
            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Fatura verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x =>
                x.InvoiceNumber.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                x.Period.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class BillingInvoiceItemDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountFormatted { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
}
