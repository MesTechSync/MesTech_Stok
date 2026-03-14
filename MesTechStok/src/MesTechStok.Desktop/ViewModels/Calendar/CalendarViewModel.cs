using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using MesTech.Domain.Interfaces;

namespace MesTechStok.Desktop.ViewModels.Calendar;

public partial class CalendarViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;
    private DateTime _currentMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    [ObservableProperty] private string currentMonthLabel = string.Empty;

    public ObservableCollection<CalendarDayVm> CalendarDays { get; } = [];

    public CalendarViewModel(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
        BuildCalendar();
    }

    [RelayCommand]
    private void PrevMonth() { _currentMonth = _currentMonth.AddMonths(-1); BuildCalendar(); }

    [RelayCommand]
    private void NextMonth() { _currentMonth = _currentMonth.AddMonths(1); BuildCalendar(); }

    [RelayCommand]
    private void CreateEvent()
        => System.Windows.MessageBox.Show("Yeni Etkinlik formu yakında.", "MesTech Takvim");

    private void BuildCalendar()
    {
        CurrentMonthLabel = _currentMonth.ToString("MMMM yyyy", new CultureInfo("tr-TR"));
        CalendarDays.Clear();

        var firstDay = _currentMonth;
        // Haftanın başlangıcı Pazartesi — DayOfWeek offset
        int offset = ((int)firstDay.DayOfWeek + 6) % 7;
        var startDate = firstDay.AddDays(-offset);

        for (int i = 0; i < 42; i++)
        {
            var date = startDate.AddDays(i);
            bool isCurrentMonth = date.Month == _currentMonth.Month;
            bool isToday = date.Date == DateTime.Today;

            CalendarDays.Add(new CalendarDayVm
            {
                Date = date,
                DayNumber = date.Day.ToString(),
                Background = isToday ? "#EFF6FF" : isCurrentMonth ? "White" : "#F8FAFC",
                DayColor = isToday ? "#2855AC" : isCurrentMonth ? "#1E293B" : "#CBD5E1",
                DayFontWeight = isToday ? "Bold" : "Normal"
            });
        }

        // TODO: Gerçek events yükle GetCalendarEventsQuery ile
        _ = LoadEventsAsync();
    }

    private async Task LoadEventsAsync()
    {
        // H28 DEV3 CalendarEventRepository tamamlanınca aktif olacak
        await Task.CompletedTask;
    }
}

public partial class CalendarDayVm : ObservableObject
{
    public DateTime Date { get; set; }
    public string DayNumber { get; set; } = string.Empty;
    public string Background { get; set; } = "White";
    public string DayColor { get; set; } = "#1E293B";
    public string DayFontWeight { get; set; } = "Normal";
    public ObservableCollection<CalendarEventDotVm> Events { get; } = [];
}

public class CalendarEventDotVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Color { get; set; } = "#2855AC";
}
