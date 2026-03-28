using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class BillingAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string currentPlan = "Pro";
    [ObservableProperty] private string monthlyFee = "0,00 TL";
    [ObservableProperty] private string nextBillingDate = "-";

    // Filters
    [ObservableProperty] private string searchText = string.Empty;

    public ObservableCollection<BillingInvoiceItemDto> Items { get; } = [];
    private List<BillingInvoiceItemDto> _allItems = [];

    public BillingAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR queries

            CurrentPlan = "Pro";
            MonthlyFee = "2.990,00 TL";
            NextBillingDate = "2026-04-01";

            _allItems =
            [
                new() { InvoiceNumber = "BILL-2026-003", Period = "Mart 2026", Amount = 2990m, AmountFormatted = "2.990,00 TL", Status = "Beklemede", DueDate = "2026-04-01", Plan = "Pro" },
                new() { InvoiceNumber = "BILL-2026-002", Period = "Subat 2026", Amount = 2990m, AmountFormatted = "2.990,00 TL", Status = "Odendi", DueDate = "2026-03-01", Plan = "Pro" },
                new() { InvoiceNumber = "BILL-2026-001", Period = "Ocak 2026", Amount = 2990m, AmountFormatted = "2.990,00 TL", Status = "Odendi", DueDate = "2026-02-01", Plan = "Pro" },
                new() { InvoiceNumber = "BILL-2025-012", Period = "Aralik 2025", Amount = 1990m, AmountFormatted = "1.990,00 TL", Status = "Odendi", DueDate = "2026-01-01", Plan = "Starter" },
            ];

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
