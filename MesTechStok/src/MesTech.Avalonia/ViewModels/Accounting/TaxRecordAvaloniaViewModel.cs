using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels.Accounting;

public partial class TaxRecordAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string totalTaxAmount = "0,00 TL";
    [ObservableProperty] private string paidTaxAmount = "0,00 TL";
    [ObservableProperty] private string pendingTaxAmount = "0,00 TL";

    // Filters
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string? selectedTaxType;

    public ObservableCollection<TaxRecordItemDto> Items { get; } = [];
    private List<TaxRecordItemDto> _allItems = [];

    public ObservableCollection<string> TaxTypes { get; } =
        ["Tumu", "KDV", "Gelir Vergisi", "Kurumlar Vergisi", "Damga Vergisi", "SGK Primi", "Stopaj", "Diger"];

    public TaxRecordAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
        SelectedTaxType = "Tumu";
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            await Task.Delay(200); // Will be replaced with MediatR query

            _allItems =
            [
                new() { TaxType = "KDV", Period = "2026/03", Description = "KDV Beyannamesi — Mart 2026", Amount = 24500m, AmountFormatted = "24.500,00 TL", DueDate = "2026-04-26", Status = "Beklemede", ReferenceNumber = "KDV-2026-03" },
                new() { TaxType = "SGK Primi", Period = "2026/03", Description = "SGK Prim Bildirimi — Mart 2026", Amount = 18200m, AmountFormatted = "18.200,00 TL", DueDate = "2026-04-30", Status = "Beklemede", ReferenceNumber = "SGK-2026-03" },
                new() { TaxType = "Stopaj", Period = "2026/03", Description = "Muhtasar Beyanname — Mart 2026", Amount = 8750m, AmountFormatted = "8.750,00 TL", DueDate = "2026-04-26", Status = "Beklemede", ReferenceNumber = "MUH-2026-03" },
                new() { TaxType = "KDV", Period = "2026/02", Description = "KDV Beyannamesi — Subat 2026", Amount = 21300m, AmountFormatted = "21.300,00 TL", DueDate = "2026-03-26", Status = "Odendi", ReferenceNumber = "KDV-2026-02" },
                new() { TaxType = "Damga Vergisi", Period = "2026/Q1", Description = "Damga Vergisi — 1. Ceyrek", Amount = 3200m, AmountFormatted = "3.200,00 TL", DueDate = "2026-04-30", Status = "Beklemede", ReferenceNumber = "DV-2026-Q1" },
            ];

            var total = _allItems.Sum(x => x.Amount);
            var paid = _allItems.Where(x => x.Status == "Odendi").Sum(x => x.Amount);
            var pending = total - paid;

            TotalTaxAmount = $"{total:N2} TL";
            PaidTaxAmount = $"{paid:N2} TL";
            PendingTaxAmount = $"{pending:N2} TL";

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Vergi kayitlari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedTaxTypeChanged(string? value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x =>
                x.Description.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                x.ReferenceNumber.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(SelectedTaxType) && SelectedTaxType != "Tumu")
        {
            filtered = filtered.Where(x => x.TaxType == SelectedTaxType);
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

public class TaxRecordItemDto
{
    public string TaxType { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string AmountFormatted { get; set; } = string.Empty;
    public string DueDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
}
