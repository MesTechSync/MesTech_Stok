using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Accounting.Queries.GetSalaryRecords;
using MesTech.Application.Features.Hr.Queries.GetEmployees;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class BordroAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

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

    // Sort
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    public ObservableCollection<string> Months { get; } =
        ["Ocak", "Subat", "Mart", "Nisan", "Mayis", "Haziran", "Temmuz", "Agustos", "Eylul", "Ekim", "Kasim", "Aralik"];

    public ObservableCollection<string> Years { get; } =
        ["2024", "2025", "2026", "2027"];

    public BordroAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var monthIndex = Months.IndexOf(SelectedMonth) + 1;
            int.TryParse(SelectedYear, out var year);
            var salaries = await _mediator.Send(new GetSalaryRecordsQuery(
                _currentUser.TenantId, year > 0 ? year : null, monthIndex > 0 ? monthIndex : null), ct);

            _allItems = salaries.Select(s => new PayrollItemDto
            {
                EmployeeName = s.EmployeeName,
                Gross = s.GrossSalary,
                Net = s.GrossSalary - s.SGKEmployee - s.IncomeTax - s.StampTax,
                GrossFormatted = $"{s.GrossSalary:N2} TL",
                SgkEmployeeFormatted = $"{s.SGKEmployee:N2} TL",
                SgkEmployerFormatted = $"{s.SGKEmployer:N2} TL",
                IncomeTaxFormatted = $"{s.IncomeTax:N2} TL",
                StampTaxFormatted = $"{s.StampTax:N2} TL",
                NetFormatted = $"{s.GrossSalary - s.SGKEmployee - s.IncomeTax - s.StampTax:N2} TL"
            }).ToList();

            var gross = _allItems.Sum(x => x.Gross);
            var net = _allItems.Sum(x => x.Net);
            var employerCost = gross * 1.2225m;

            TotalGross = $"{gross:N2} TL";
            TotalNet = $"{net:N2} TL";
            TotalEmployerCost = $"{employerCost:N2} TL";

            IsEmpty = _allItems.Count == 0;
            ApplyFilters();
        }, "Bordro verileri yuklenirken hata");
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

        // Sort
        var sortedList = SortColumn switch
        {
            "EmployeeName" => SortAscending ? filtered.OrderBy(x => x.EmployeeName).ToList() : filtered.OrderByDescending(x => x.EmployeeName).ToList(),
            "Gross"        => SortAscending ? filtered.OrderBy(x => x.Gross).ToList()        : filtered.OrderByDescending(x => x.Gross).ToList(),
            "Net"          => SortAscending ? filtered.OrderBy(x => x.Net).ToList()          : filtered.OrderByDescending(x => x.Net).ToList(),
            _              => SortAscending ? filtered.OrderBy(x => x.EmployeeName).ToList() : filtered.OrderByDescending(x => x.EmployeeName).ToList(),
        };

        Items.Clear();
        foreach (var item in sortedList)
            Items.Add(item);

        TotalCount = Items.Count;
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private void SortBy(string column)
    {
        if (SortColumn == column) SortAscending = !SortAscending;
        else { SortColumn = column; SortAscending = true; }
        ApplyFilters();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(_currentUser.TenantId, "payroll", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Bordro verileri disa aktarilirken hata");
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
