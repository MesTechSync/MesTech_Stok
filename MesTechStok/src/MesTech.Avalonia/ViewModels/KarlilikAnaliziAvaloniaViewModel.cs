using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetProfitReport;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class KarlilikAnaliziAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private int totalCount;

    // KPI
    [ObservableProperty] private string totalRevenue = "0,00 TL";
    [ObservableProperty] private string totalProfit = "0,00 TL";
    [ObservableProperty] private string averageMargin = "%0.0";

    // Filters
    [ObservableProperty] private string selectedPlatform = "Tumu";
    [ObservableProperty] private string selectedCategory = "Tumu";
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private DateTimeOffset? startDate;
    [ObservableProperty] private DateTimeOffset? endDate;

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    public ObservableCollection<ProfitabilityItemDto> Items { get; } = [];
    private List<ProfitabilityItemDto> _allItems = [];

    public ObservableCollection<string> Platforms { get; } =
        ["Tumu", "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon"];

    public ObservableCollection<string> Categories { get; } =
        ["Tumu", "Giyim", "Elektronik", "Ev & Yasam", "Kozmetik", "Gida"];

    public KarlilikAnaliziAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var platform = SelectedPlatform == "Tumu" ? null : SelectedPlatform;
            var report = await _mediator.Send(new GetProfitReportQuery(_currentUser.TenantId, "Aylik", platform), ct);

            if (report is null)
            {
                IsEmpty = true;
                return;
            }

            TotalRevenue = $"{report.TotalRevenue:N2} TL";
            TotalProfit = $"{report.NetProfit:N2} TL";
            AverageMargin = $"%{report.ProfitMargin:N1}";

            _allItems =
            [
                new()
                {
                    ProductName = report.Platform ?? "Genel",
                    SalesFormatted = $"{report.TotalRevenue:N2} TL",
                    CostFormatted = $"{report.TotalCost:N2} TL",
                    CommissionFormatted = $"{report.TotalCommission:N2} TL",
                    ShippingFormatted = $"{report.TotalCargo:N2} TL",
                    NetProfitFormatted = $"{report.NetProfit:N2} TL",
                    MarginFormatted = $"%{report.ProfitMargin:N1}",
                    NetProfit = report.NetProfit
                }
            ];

            ApplyFilters();
        }, "Karlilik analizi yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnSelectedPlatformChanged(string value) => ApplyFilters();
    partial void OnSelectedCategoryChanged(string value) => ApplyFilters();

    private void ApplyFilters()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText) && SearchText.Length >= 2)
        {
            var s = SearchText;
            filtered = filtered.Where(x => x.ProductName.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        // Sort
        filtered = SortColumn switch
        {
            "ProductName"        => SortAscending ? filtered.OrderBy(x => x.ProductName)        : filtered.OrderByDescending(x => x.ProductName),
            "SalesFormatted"     => SortAscending ? filtered.OrderBy(x => x.SalesFormatted)     : filtered.OrderByDescending(x => x.SalesFormatted),
            "CostFormatted"      => SortAscending ? filtered.OrderBy(x => x.CostFormatted)      : filtered.OrderByDescending(x => x.CostFormatted),
            "CommissionFormatted"=> SortAscending ? filtered.OrderBy(x => x.CommissionFormatted): filtered.OrderByDescending(x => x.CommissionFormatted),
            "ShippingFormatted"  => SortAscending ? filtered.OrderBy(x => x.ShippingFormatted)  : filtered.OrderByDescending(x => x.ShippingFormatted),
            "NetProfit"          => SortAscending ? filtered.OrderBy(x => x.NetProfit)          : filtered.OrderByDescending(x => x.NetProfit),
            "MarginFormatted"    => SortAscending ? filtered.OrderBy(x => x.MarginFormatted)    : filtered.OrderByDescending(x => x.MarginFormatted),
            _                    => filtered
        };

        Items.Clear();
        foreach (var item in filtered)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
        ApplyFilters();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new ExportReportCommand(_currentUser.TenantId, "profitability", "xlsx"), ct);

            if (result?.FileData.Length > 0)
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "MesTech_Exports");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, result.FileName);
                await File.WriteAllBytesAsync(path, result.FileData.ToArray(), ct);
            }
        }, "Excel export sirasinda hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class ProfitabilityItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public string SalesFormatted { get; set; } = string.Empty;
    public string CostFormatted { get; set; } = string.Empty;
    public string CommissionFormatted { get; set; } = string.Empty;
    public string ShippingFormatted { get; set; } = string.Empty;
    public string NetProfitFormatted { get; set; } = string.Empty;
    public string MarginFormatted { get; set; } = string.Empty;
    public decimal NetProfit { get; set; }
}
