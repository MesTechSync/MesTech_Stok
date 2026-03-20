using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class NakitAkisAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    // KPI
    [ObservableProperty] private string totalInflow = "0,00 TL";
    [ObservableProperty] private string totalOutflow = "0,00 TL";
    [ObservableProperty] private string netCashFlow = "0,00 TL";
    [ObservableProperty] private int totalCount;

    // Filters
    [ObservableProperty] private string selectedPeriodType = "Aylik";
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;
    [ObservableProperty] private string searchText = string.Empty;

    public ObservableCollection<CashFlowItemDto> Items { get; } = [];
    private List<CashFlowItemDto> _allItems = [];

    public ObservableCollection<string> PeriodTypes { get; } =
        ["Gunluk", "Haftalik", "Aylik", "Yillik"];

    public NakitAkisAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task LoadAsync()
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
                new() { Date = "20.03.2026", Description = "Trendyol hakedis odemesi", InflowFormatted = "+12.480,00 TL", OutflowFormatted = "", BalanceFormatted = "52.480,00 TL", Inflow = 12480, Outflow = 0 },
                new() { Date = "19.03.2026", Description = "Aras Kargo fatura odemesi", InflowFormatted = "", OutflowFormatted = "-1.240,00 TL", BalanceFormatted = "40.000,00 TL", Inflow = 0, Outflow = 1240 },
                new() { Date = "19.03.2026", Description = "Hepsiburada hakedis odemesi", InflowFormatted = "+8.920,00 TL", OutflowFormatted = "", BalanceFormatted = "41.240,00 TL", Inflow = 8920, Outflow = 0 },
                new() { Date = "18.03.2026", Description = "Ofis kirasi odemesi", InflowFormatted = "", OutflowFormatted = "-6.500,00 TL", BalanceFormatted = "32.320,00 TL", Inflow = 0, Outflow = 6500 },
                new() { Date = "18.03.2026", Description = "N11 hakedis odemesi", InflowFormatted = "+3.150,00 TL", OutflowFormatted = "", BalanceFormatted = "38.820,00 TL", Inflow = 3150, Outflow = 0 },
                new() { Date = "17.03.2026", Description = "AWS sunucu odemesi", InflowFormatted = "", OutflowFormatted = "-2.100,00 TL", BalanceFormatted = "35.670,00 TL", Inflow = 0, Outflow = 2100 },
            ];

            ApplyFilters();

            var inflow = _allItems.Sum(i => i.Inflow);
            var outflow = _allItems.Sum(i => i.Outflow);
            TotalInflow = $"{inflow:N2} TL";
            TotalOutflow = $"{outflow:N2} TL";
            NetCashFlow = $"{inflow - outflow:N2} TL";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Nakit akis verileri yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedPeriodTypeChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x => x.Description.Contains(s, StringComparison.OrdinalIgnoreCase));
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

public class CashFlowItemDto
{
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string InflowFormatted { get; set; } = string.Empty;
    public string OutflowFormatted { get; set; } = string.Empty;
    public string BalanceFormatted { get; set; } = string.Empty;
    public decimal Inflow { get; set; }
    public decimal Outflow { get; set; }
}
