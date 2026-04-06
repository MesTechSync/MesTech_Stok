using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Finance.Queries.GetBudgetSummary;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class BudgetAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;


    // KPI
    [ObservableProperty] private string totalBudget = "0,00 TL";
    [ObservableProperty] private string totalActual = "0,00 TL";
    [ObservableProperty] private string totalRemaining = "0,00 TL";
    [ObservableProperty] private string usageRate = "%0";

    // Filters
    [ObservableProperty] private string selectedMonth = "Mart";
    [ObservableProperty] private string selectedYear = "2026";

    public ObservableCollection<BudgetLineItemDto> Items { get; } = [];
    private List<BudgetLineItemDto> _allItems = [];

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    public ObservableCollection<string> Months { get; } =
        ["Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran", "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik"];

    public ObservableCollection<string> Years { get; } =
        ["2024", "2025", "2026", "2027"];

    public BudgetAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var monthIndex = Months.IndexOf(SelectedMonth) + 1;
            var year = int.TryParse(SelectedYear, out var y) ? y : DateTime.Now.Year;
            var month = monthIndex > 0 ? monthIndex : DateTime.Now.Month;

            var result = await _mediator.Send(new GetBudgetSummaryQuery(_currentUser.TenantId, year, month), ct);

            _allItems = result.Categories.Select(cat =>
            {
                var remaining = cat.Budget - cat.Spent;
                return new BudgetLineItemDto
                {
                    CategoryName = cat.Category,
                    Budget = cat.Budget,
                    Actual = cat.Spent,
                    BudgetFormatted = $"{cat.Budget:N2} TL",
                    ActualFormatted = $"{cat.Spent:N2} TL",
                    RemainingFormatted = $"{remaining:N2} TL",
                    StatusText = remaining < 0 ? "ASIM" : cat.Spent >= cat.Budget ? "Tamamlandi" : "Normal"
                };
            }).ToList();

            TotalBudget = $"{result.TotalBudget:N2} TL";
            TotalActual = $"{result.TotalSpent:N2} TL";
            TotalRemaining = $"{result.Remaining:N2} TL";
            UsageRate = $"%{result.UtilizationPercent:N0}";

            ApplySort();
        }, "Butce verileri yuklenirken hata");
    }

    private void ApplySort()
    {
        var sorted = SortColumn switch
        {
            "CategoryName" => SortAscending ? _allItems.OrderBy(x => x.CategoryName).ToList() : _allItems.OrderByDescending(x => x.CategoryName).ToList(),
            "Budget"       => SortAscending ? _allItems.OrderBy(x => x.Budget).ToList()       : _allItems.OrderByDescending(x => x.Budget).ToList(),
            "Actual"       => SortAscending ? _allItems.OrderBy(x => x.Actual).ToList()       : _allItems.OrderByDescending(x => x.Actual).ToList(),
            "StatusText"   => SortAscending ? _allItems.OrderBy(x => x.StatusText).ToList()   : _allItems.OrderByDescending(x => x.StatusText).ToList(),
            _              => SortAscending ? _allItems.OrderBy(x => x.CategoryName).ToList() : _allItems.OrderByDescending(x => x.CategoryName).ToList(),
        };
        Items.Clear();
        foreach (var item in sorted) Items.Add(item);
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplySort();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(_currentUser.TenantId, "budget", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Butce verileri disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class BudgetLineItemDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string BudgetFormatted { get; set; } = string.Empty;
    public string ActualFormatted { get; set; } = string.Empty;
    public string RemainingFormatted { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public decimal Budget { get; set; }
    public decimal Actual { get; set; }
}
