using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEvents;
using MesTech.Domain.Interfaces;

namespace MesTech.Avalonia.ViewModels;

public partial class CalendarAvaloniaViewModel : ViewModelBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;


    [ObservableProperty] private string currentMonth = DateTime.Now.ToString("MMMM yyyy");
    [ObservableProperty] private int totalCount;

    public ObservableCollection<CalendarEventVm> Events { get; } = [];

    public CalendarAvaloniaViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    public override async Task LoadAsync()
    {
        await SafeExecuteAsync(async ct =>
        {
            var result = await _mediator.Send(new GetCalendarEventsQuery(_currentUser.TenantId), ct);

            Events.Clear();
            foreach (var e in result)
            {
                Events.Add(new CalendarEventVm
                {
                    Id = e.Id,
                    Title = e.Title,
                    StartDate = e.StartAt,
                    EndDate = e.EndAt,
                    Type = e.Type.ToString(),
                    Color = e.Color ?? "#3B82F6"
                });
            }
            TotalCount = Events.Count;
            IsEmpty = Events.Count == 0;
        }, "Takvim yuklenirken hata");
    }

    [RelayCommand]
    private async Task Refresh() => await LoadAsync();
}

public class CalendarEventVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Color { get; set; } = "#3B82F6";

    public string DateDisplay => StartDate.ToString("dd MMM yyyy — HH:mm");
    public string TimeRange => $"{StartDate:HH:mm} - {EndDate:HH:mm}";
}
