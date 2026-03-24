using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Calisma Takvimi ViewModel — haftalik plan + vardiya listesi.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class WorkScheduleAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;


    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    public ObservableCollection<ScheduleItemDto> Schedules { get; } = [];

    public WorkScheduleAvaloniaViewModel(IMediator mediator)
    {
        _mediator = mediator;
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

            Schedules.Clear();
            Schedules.Add(new ScheduleItemDto { EmployeeName = "Ali Veli", DayOfWeek = "Pazartesi", StartTime = "09:00", EndTime = "18:00", ShiftType = "Sabah" });
            Schedules.Add(new ScheduleItemDto { EmployeeName = "Ali Veli", DayOfWeek = "Sali", StartTime = "09:00", EndTime = "18:00", ShiftType = "Sabah" });
            Schedules.Add(new ScheduleItemDto { EmployeeName = "Mehmet Demir", DayOfWeek = "Pazartesi", StartTime = "14:00", EndTime = "22:00", ShiftType = "Aksam" });
            Schedules.Add(new ScheduleItemDto { EmployeeName = "Mehmet Demir", DayOfWeek = "Carsamba", StartTime = "14:00", EndTime = "22:00", ShiftType = "Aksam" });
            Schedules.Add(new ScheduleItemDto { EmployeeName = "Fatma Ozturk", DayOfWeek = "Persembe", StartTime = "09:00", EndTime = "18:00", ShiftType = "Sabah" });

            TotalCount = Schedules.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Takvim yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class ScheduleItemDto
{
    public string EmployeeName { get; set; } = string.Empty;
    public string DayOfWeek { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string ShiftType { get; set; } = string.Empty;
}
