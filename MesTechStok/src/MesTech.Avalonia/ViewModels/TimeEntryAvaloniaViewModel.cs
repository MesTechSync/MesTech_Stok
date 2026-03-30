using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Hr.Commands.CreateTimeEntry;
using MesTech.Application.Features.Hr.Queries.GetTimeEntries;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

/// <summary>
/// Zaman Kaydi ViewModel — tarih, saat, proje, aciklama girisi + liste.
/// EMR-12: Wired to GetTimeEntriesQuery + CreateTimeEntryCommand via MediatR.
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
            var from = SelectedDate?.Date ?? DateTime.Now.Date.AddDays(-7);
            var to = DateTime.Now.Date.AddDays(1);
            var entries = await _mediator.Send(new GetTimeEntriesQuery(
                _currentUser.TenantId, from, to));

            TimeEntries.Clear();
            foreach (var e in entries)
            {
                var dur = e.EndedAt.HasValue
                    ? $"{(int)(e.EndedAt.Value - e.StartedAt).TotalHours} sa {(e.EndedAt.Value - e.StartedAt).Minutes} dk"
                    : $"{e.Minutes} dk";
                TimeEntries.Add(new TimeEntryItemDto
                {
                    Date = e.StartedAt.ToString("dd.MM.yyyy"),
                    StartTime = e.StartedAt.ToString("HH:mm"),
                    EndTime = e.EndedAt?.ToString("HH:mm") ?? "--",
                    Duration = dur,
                    Project = e.IsBillable ? "Faturalanabilir" : "Dahili",
                    Description = e.Description ?? string.Empty
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
            await _mediator.Send(new CreateTimeEntryCommand(
                TenantId: _currentUser.TenantId,
                WorkTaskId: Guid.Empty, // NAV: task selection needed
                UserId: _currentUser.UserId ?? Guid.Empty,
                Description: $"{ProjectName}: {Description}".Trim(':').Trim(),
                IsBillable: false));

            // Reload to show the new entry from DB
            StartTime = string.Empty;
            EndTime = string.Empty;
            ProjectName = string.Empty;
            Description = string.Empty;
            await LoadAsync();
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
