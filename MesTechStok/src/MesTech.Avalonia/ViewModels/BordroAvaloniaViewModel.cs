using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class BordroAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;
    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string totalGross = "0,00 TL";
    [ObservableProperty] private string totalNet = "0,00 TL";
    [ObservableProperty] private string totalEmployerCost = "0,00 TL";

    // Filters
    [ObservableProperty] private string selectedMonth = "Mart";
    [ObservableProperty] private string selectedYear = "2026";
    [ObservableProperty] private string searchText = string.Empty;

    public ObservableCollection<PayrollItemDto> Items { get; } = [];
    private List<PayrollItemDto> _allItems = [];

    public ObservableCollection<string> Months { get; } =
        ["Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran", "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik"];

    public ObservableCollection<string> Years { get; } =
        ["2024", "2025", "2026", "2027"];

    public BordroAvaloniaViewModel(IMediator mediator)
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
                new() { EmployeeName = "Ahmet Yilmaz", GrossFormatted = "25.000,00 TL", SgkEmployeeFormatted = "3.500,00 TL", SgkEmployerFormatted = "5.563,00 TL", IncomeTaxFormatted = "3.750,00 TL", StampTaxFormatted = "189,75 TL", NetFormatted = "17.560,25 TL", Gross = 25000, Net = 17560.25m },
                new() { EmployeeName = "Fatma Demir", GrossFormatted = "20.000,00 TL", SgkEmployeeFormatted = "2.800,00 TL", SgkEmployerFormatted = "4.450,00 TL", IncomeTaxFormatted = "2.580,00 TL", StampTaxFormatted = "151,80 TL", NetFormatted = "14.468,20 TL", Gross = 20000, Net = 14468.20m },
                new() { EmployeeName = "Mehmet Kaya", GrossFormatted = "18.000,00 TL", SgkEmployeeFormatted = "2.520,00 TL", SgkEmployerFormatted = "4.005,00 TL", IncomeTaxFormatted = "2.130,00 TL", StampTaxFormatted = "136,62 TL", NetFormatted = "13.213,38 TL", Gross = 18000, Net = 13213.38m },
                new() { EmployeeName = "Ayse Ozturk", GrossFormatted = "15.000,00 TL", SgkEmployeeFormatted = "2.100,00 TL", SgkEmployerFormatted = "3.338,00 TL", IncomeTaxFormatted = "1.575,00 TL", StampTaxFormatted = "113,85 TL", NetFormatted = "11.211,15 TL", Gross = 15000, Net = 11211.15m },
            ];

            var gross = _allItems.Sum(x => x.Gross);
            var net = _allItems.Sum(x => x.Net);
            var employerCost = gross * 1.2225m; // approximate employer cost ratio

            TotalGross = $"{gross:N2} TL";
            TotalNet = $"{net:N2} TL";
            TotalEmployerCost = $"{employerCost:N2} TL";

            ApplyFilters();
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Bordro verileri yuklenemedi: {ex.Message}";
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
            filtered = filtered.Where(x => x.EmployeeName.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class PayrollItemDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string GrossFormatted { get; set; } = string.Empty;
    public string SgkEmployeeFormatted { get; set; } = string.Empty;
    public string SgkEmployerFormatted { get; set; } = string.Empty;
    public string IncomeTaxFormatted { get; set; } = string.Empty;
    public string StampTaxFormatted { get; set; } = string.Empty;
    public string NetFormatted { get; set; } = string.Empty;
    public decimal Gross { get; set; }
    public decimal Net { get; set; }
}
