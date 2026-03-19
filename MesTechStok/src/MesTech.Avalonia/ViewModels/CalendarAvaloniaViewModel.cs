using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;

namespace MesTech.Avalonia.ViewModels;

public partial class CalendarAvaloniaViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool isEmpty;

    [ObservableProperty] private string currentMonth = DateTime.Now.ToString("MMMM yyyy");
    [ObservableProperty] private int totalCount;

    public ObservableCollection<CalendarEventVm> Events { get; } = [];

    public CalendarAvaloniaViewModel(IMediator mediator)
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
            Events.Clear();
            Events.Add(new CalendarEventVm
            {
                Id = Guid.NewGuid(), Title = "ABC Ltd Demo",
                StartDate = DateTime.Now.AddDays(1).Date.AddHours(10),
                EndDate = DateTime.Now.AddDays(1).Date.AddHours(11),
                Type = "Toplanti", Color = "#3B82F6"
            });
            Events.Add(new CalendarEventVm
            {
                Id = Guid.NewGuid(), Title = "Sprint Planlama",
                StartDate = DateTime.Now.AddDays(2).Date.AddHours(14),
                EndDate = DateTime.Now.AddDays(2).Date.AddHours(15),
                Type = "Toplanti", Color = "#8B5CF6"
            });
            Events.Add(new CalendarEventVm
            {
                Id = Guid.NewGuid(), Title = "Teklif son gun — XYZ AS",
                StartDate = DateTime.Now.AddDays(3).Date.AddHours(17),
                EndDate = DateTime.Now.AddDays(3).Date.AddHours(17),
                Type = "Hatirlatma", Color = "#EF4444"
            });
            Events.Add(new CalendarEventVm
            {
                Id = Guid.NewGuid(), Title = "Fatma Demir takip araması",
                StartDate = DateTime.Now.AddDays(4).Date.AddHours(9),
                EndDate = DateTime.Now.AddDays(4).Date.AddHours(9).AddMinutes(30),
                Type = "Arama", Color = "#10B981"
            });
            TotalCount = Events.Count;
            IsEmpty = Events.Count == 0;
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
