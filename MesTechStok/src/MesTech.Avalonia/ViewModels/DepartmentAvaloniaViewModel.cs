using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Hr.Queries.GetDepartments;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class DepartmentAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ITenantProvider _tenantProvider;

    [ObservableProperty] private int totalCount;

    public ObservableCollection<DepartmentItemVm> Departments { get; } = [];

    public DepartmentAvaloniaViewModel(IMediator mediator, ITenantProvider tenantProvider)
    {
        _mediator = mediator;
        _tenantProvider = tenantProvider;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            var tenantId = _tenantProvider.GetCurrentTenantId();
            var result = await _mediator.Send(new GetDepartmentsQuery(tenantId), CancellationToken);

            Departments.Clear();
            foreach (var d in result)
            {
                Departments.Add(new DepartmentItemVm
                {
                    Id = d.Id,
                    Name = d.Name,
                    EmployeeCount = d.EmployeeCount,
                    Status = d.IsActive ? "Aktif" : "Pasif"
                });
            }
            TotalCount = Departments.Count;
            IsEmpty = Departments.Count == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Departmanlar yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private void Add()
    {
        // NAV: Navigate to department create form or show dialog
    }
}

public class DepartmentItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Manager { get; set; }
    public int EmployeeCount { get; set; }
    public string Status { get; set; } = "Aktif";
}
