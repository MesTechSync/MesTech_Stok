using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Hr.Queries.GetEmployees;
using MesTech.Application.Features.Reporting.Commands.ExportReport;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// HR Employees ViewModel — wired to GetEmployeesQuery via MediatR.
/// HH-FIX-employees: sort + Excel export added.
/// </summary>
public partial class EmployeesAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private readonly IDialogService _dialog;
    private List<EmployeeDto> _allEmployees = [];

    [ObservableProperty] private ObservableCollection<EmployeeDto> employees = [];
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string sortColumn = "default";
    [ObservableProperty] private bool sortAscending = true;

    public EmployeesAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser, IDialogService dialog)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        _dialog = dialog;
        Title = "Calisanlar";
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(
                new GetEmployeesQuery(_currentUser.TenantId), ct);

            _allEmployees = result.ToList();
            ApplyFilter();
        }, "Calisanlar yuklenirken hata");
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allEmployees
            : _allEmployees.Where(e =>
                e.EmployeeCode.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                e.JobTitle.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                e.WorkEmail.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
              .ToList();

        var sorted = SortColumn switch
        {
            "EmployeeCode" => SortAscending ? filtered.OrderBy(e => e.EmployeeCode) : filtered.OrderByDescending(e => e.EmployeeCode),
            "JobTitle"     => SortAscending ? filtered.OrderBy(e => e.JobTitle)     : filtered.OrderByDescending(e => e.JobTitle),
            "WorkEmail"    => SortAscending ? filtered.OrderBy(e => e.WorkEmail)    : filtered.OrderByDescending(e => e.WorkEmail),
            "Status"       => SortAscending ? filtered.OrderBy(e => e.Status)       : filtered.OrderByDescending(e => e.Status),
            "HireDate"     => SortAscending ? filtered.OrderBy(e => e.HireDate)     : filtered.OrderByDescending(e => e.HireDate),
            _              => filtered.OrderBy(e => e.EmployeeCode)
        };
        var sortedList = sorted.ToList();

        Employees = new ObservableCollection<EmployeeDto>(sortedList);
        TotalCount = sortedList.Count;
        Summary = $"Toplam {TotalCount} calisan";
        IsEmpty = TotalCount == 0;
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
        ApplyFilter();
    }

    [RelayCommand]
    private async Task ExportExcel()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new ExportReportCommand(_currentUser.TenantId, "employees", "xlsx"), ct);
            if (result.FileData.Length > 0)
            {
                var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MesTech_Exports");
                System.IO.Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllBytesAsync(System.IO.Path.Combine(dir, result.FileName), result.FileData);
            }
        }, "Calisanlar disa aktarilirken hata");
    }

    [RelayCommand]
    private async Task Add()
    {
        var newEmployee = new EmployeeDto
        {
            Id = Guid.NewGuid(),
            EmployeeCode = $"EMP-{DateTime.Now:yyyyMMddHHmmss}",
            JobTitle = "Yeni Calisan",
            WorkEmail = string.Empty,
            Status = "Taslak",
            HireDate = DateTime.Now
        };
        _allEmployees.Insert(0, newEmployee);
        ApplyFilter();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
