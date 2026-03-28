using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Hr.Queries.GetEmployees;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// HR Employees ViewModel — wired to GetEmployeesQuery via MediatR.
/// </summary>
public partial class EmployeesAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private List<EmployeeDto> _allEmployees = [];

    [ObservableProperty] private ObservableCollection<EmployeeDto> employees = [];
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string searchText = string.Empty;

    public EmployeesAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
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

        Employees = new ObservableCollection<EmployeeDto>(filtered);
        TotalCount = filtered.Count;
        Summary = $"Toplam {TotalCount} calisan";
        IsEmpty = TotalCount == 0;
    }

    [RelayCommand]
    private async Task Add()
    {
        // TODO: Open employee create dialog
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}
