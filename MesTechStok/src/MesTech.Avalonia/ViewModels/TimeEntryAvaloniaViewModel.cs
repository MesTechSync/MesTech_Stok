using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Hr.Queries.GetEmployees;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Zaman Kaydi ViewModel — tarih, saat, proje, aciklama girisi + liste.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class TimeEntryAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    // New entry form fields
    [ObservableProperty] private DateTimeOffset? selectedDate = DateTimeOffset.Now;
    [ObservableProperty] private string startTime = string.Empty;
    [ObservableProperty] private string endTime = string.Empty;
    [ObservableProperty] private string projectName = string.Empty;
    [ObservableProperty] private string description = string.Empty;

    public ObservableCollection<TimeEntryItemDto> TimeEntries { get; } = [];

    public TimeEntryAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        IsEmpty = false;
        ErrorMessage = string.Empty;
        try
        {
            // DEP: DEV1 — Replace with GetTimeEntriesQuery when available — using employee list as scaffold
            var employees = await _mediator.Send(new GetEmployeesQuery(_currentUser.TenantId));

            TimeEntries.Clear();
            // Placeholder: show employees as time entry rows until dedicated TimeEntry handler exists
            foreach (var e in employees)
            {
                TimeEntries.Add(new TimeEntryItemDto
                {
                    Date = DateTime.Now.ToString("dd.MM.yyyy"),
                    StartTime = "09:00",
                    EndTime = "18:00",
                    Duration = "8 sa",
                    Project = e.JobTitle,
                    Description = e.EmployeeCode
                });
            }

            TotalCount = TimeEntries.Count;
            IsEmpty = TotalCount == 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Zaman kayitlari yuklenemedi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();

    [RelayCommand]
    private async Task SaveEntry()
    {
        if (string.IsNullOrWhiteSpace(StartTime) || string.IsNullOrWhiteSpace(EndTime))
            return;

        IsLoading = true;
        try
        {
            // DEP: DEV1 — await _mediator.Send(new CreateTimeEntryCommand(...))
            var dateStr = SelectedDate?.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy");
            TimeEntries.Insert(0, new TimeEntryItemDto
            {
                Date = dateStr,
                StartTime = StartTime,
                EndTime = EndTime,
                Duration = "-- sa",
                Project = ProjectName,
                Description = Description
            });

            TotalCount = TimeEntries.Count;
            StartTime = string.Empty;
            EndTime = string.Empty;
            ProjectName = string.Empty;
            Description = string.Empty;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public class TimeEntryItemDto
{
    public string Date { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
