using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Billing.Queries.GetBillingInvoices;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionPlans;
using MesTech.Application.Features.Billing.Queries.GetSubscriptionUsage;
using MesTech.Application.Features.Billing.Queries.GetUserFeatures;
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
    [ObservableProperty] private int availablePlanCount;

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
            // KÖK-1 FIX: Sequential query — DbContext concurrent access önleme
            var subscription = await _mediator.Send(new GetTenantSubscriptionQuery(_currentUser.TenantId));
            var invoices = await _mediator.Send(new GetBillingInvoicesQuery(_currentUser.TenantId));

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

            // Subscription plans (G540 orphan wire)
            try
            {
                var plans = await _mediator.Send(new GetSubscriptionPlansQuery());
                AvailablePlanCount = plans.Count;
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] GetSubscriptionPlans failed: {ex.Message}"); AvailablePlanCount = 0; }
            try { _ = await _mediator.Send(new GetSubscriptionUsageQuery(_currentUser.TenantId)); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] GetSubscriptionUsage failed: {ex.Message}"); }
            try { _ = await _mediator.Send(new GetUserFeaturesQuery(_currentUser.TenantId)); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[WARNING] GetUserFeatures failed: {ex.Message}"); }

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
