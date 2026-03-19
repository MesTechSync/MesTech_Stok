using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Zaman Kaydi ViewModel — tarih, saat, proje, aciklama girisi + liste.
/// EMR-12: Enhanced from placeholder to functional view.
/// </summary>
public partial class TimeEntryAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private int totalCount;

    // New entry form fields
    [ObservableProperty] private DateTimeOffset? selectedDate = DateTimeOffset.Now;
    [ObservableProperty] private string startTime = string.Empty;
    [ObservableProperty] private string endTime = string.Empty;
    [ObservableProperty] private string projectName = string.Empty;
    [ObservableProperty] private string description = string.Empty;

    public ObservableCollection<TimeEntryItemDto> TimeEntries { get; } = [];

    public TimeEntryAvaloniaViewModel(IMediator mediator)
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

            TimeEntries.Clear();
            TimeEntries.Add(new TimeEntryItemDto { Date = "19.03.2026", StartTime = "09:00", EndTime = "12:30", Duration = "3.5 sa", Project = "MesTech Stok", Description = "Avalonia view gelistirme" });
            TimeEntries.Add(new TimeEntryItemDto { Date = "19.03.2026", StartTime = "13:30", EndTime = "18:00", Duration = "4.5 sa", Project = "MesTech Stok", Description = "ViewModel entegrasyon" });
            TimeEntries.Add(new TimeEntryItemDto { Date = "18.03.2026", StartTime = "09:00", EndTime = "17:00", Duration = "8 sa", Project = "MesTech Trendyol", Description = "API adapter duzenleme" });

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
            await Task.Delay(200); // Will be replaced with MediatR command

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
