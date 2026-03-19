using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class DepartmentAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private int totalCount;

    public ObservableCollection<DepartmentItemVm> Departments { get; } = [];

    public DepartmentAvaloniaViewModel(IMediator mediator)
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
            await Task.Delay(50);
            Departments.Clear();
            Departments.Add(new DepartmentItemVm { Id = Guid.NewGuid(), Name = "Yazilim Gelistirme", Manager = "Fatih Ilhan", EmployeeCount = 12, Status = "Aktif" });
            Departments.Add(new DepartmentItemVm { Id = Guid.NewGuid(), Name = "Satis ve Pazarlama", Manager = "Mehmet Can", EmployeeCount = 8, Status = "Aktif" });
            Departments.Add(new DepartmentItemVm { Id = Guid.NewGuid(), Name = "Insan Kaynaklari", Manager = "Ayse Kara", EmployeeCount = 4, Status = "Aktif" });
            Departments.Add(new DepartmentItemVm { Id = Guid.NewGuid(), Name = "Finans", Manager = "Ali Kaya", EmployeeCount = 6, Status = "Aktif" });
            Departments.Add(new DepartmentItemVm { Id = Guid.NewGuid(), Name = "Destek", Manager = "Zeynep Arslan", EmployeeCount = 10, Status = "Aktif" });
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
}

public class DepartmentItemVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Manager { get; set; }
    public int EmployeeCount { get; set; }
    public string Status { get; set; } = "Aktif";
}
