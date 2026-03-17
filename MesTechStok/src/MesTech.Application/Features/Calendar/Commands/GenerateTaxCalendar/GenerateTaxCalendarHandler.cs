#pragma warning disable MA0051 // Method is too long — tax calendar generation is a single cohesive operation
using MediatR;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;

namespace MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;

/// <summary>
/// Yillik vergi takvimi olusturma handler.
/// Turk vergi mevzuatina uygun tarihlerle CalendarEvent kayitlari olusturur.
///
/// Takvim icerigi:
///   - 12x KDV beyanname (takip eden ayin 26'si)
///   - 3x Gecici vergi (Mayis 17, Agustos 17, Kasim 17)
///   - 12x SGK bildirge (takip eden ayin 26'si)
///   - 12x Ba-Bs formlari (takip eden ayin son gunu)
///   - 1x Yillik gelir vergisi beyannamesi (Mart 31)
///
/// Toplam: 40 etkinlik/yil
/// </summary>
public class GenerateTaxCalendarHandler : IRequestHandler<GenerateTaxCalendarCommand, int>
{
    private readonly ICalendarEventRepository _repository;
    private readonly IUnitOfWork _uow;

    public GenerateTaxCalendarHandler(ICalendarEventRepository repository, IUnitOfWork uow)
        => (_repository, _uow) = (repository, uow);

    public async Task<int> Handle(
        GenerateTaxCalendarCommand request,
        CancellationToken cancellationToken)
    {
        var year = request.Year;
        var tenantId = request.TenantId;
        var count = 0;

        // 1. KDV beyanname — her ayin KDV'si takip eden ayin 26'sina kadar
        for (int month = 1; month <= 12; month++)
        {
            var deadlineDate = GetNextMonthDate(year, month, 26);
            var ev = CalendarEvent.Create(
                tenantId,
                $"KDV Beyanname — {year}/{month:D2}",
                deadlineDate.Date,
                deadlineDate.Date.AddHours(23).AddMinutes(59),
                EventType.Deadline,
                isAllDay: true,
                description: $"{year} yili {month}. ay KDV beyannamesi son teslim tarihi.");
            await _repository.AddAsync(ev, cancellationToken);
            count++;
        }

        // 2. Gecici vergi — 3 donem (Q1: Mayis 17, Q2: Agustos 17, Q3: Kasim 17)
        var provisionalTaxDeadlines = new[]
        {
            (Quarter: 1, DeadlineMonth: 5, Day: 17, Label: "1. Donem (Ocak-Mart)"),
            (Quarter: 2, DeadlineMonth: 8, Day: 17, Label: "2. Donem (Nisan-Haziran)"),
            (Quarter: 3, DeadlineMonth: 11, Day: 17, Label: "3. Donem (Temmuz-Eylul)")
        };

        foreach (var pt in provisionalTaxDeadlines)
        {
            var deadlineDate = new DateTime(year, pt.DeadlineMonth, pt.Day, 0, 0, 0, DateTimeKind.Utc);
            var ev = CalendarEvent.Create(
                tenantId,
                $"Gecici Vergi — {pt.Label}",
                deadlineDate.Date,
                deadlineDate.Date.AddHours(23).AddMinutes(59),
                EventType.Deadline,
                isAllDay: true,
                description: $"{year} yili {pt.Label} gecici vergi beyannamesi son teslim tarihi.");
            await _repository.AddAsync(ev, cancellationToken);
            count++;
        }

        // 3. SGK bildirge — her ayin SGK'si takip eden ayin 26'sina kadar
        for (int month = 1; month <= 12; month++)
        {
            var deadlineDate = GetNextMonthDate(year, month, 26);
            var ev = CalendarEvent.Create(
                tenantId,
                $"SGK Bildirge — {year}/{month:D2}",
                deadlineDate.Date,
                deadlineDate.Date.AddHours(23).AddMinutes(59),
                EventType.Deadline,
                isAllDay: true,
                description: $"{year} yili {month}. ay SGK prim bildirgeleri son teslim tarihi.");
            await _repository.AddAsync(ev, cancellationToken);
            count++;
        }

        // 4. Ba-Bs formlari — her ayin Ba-Bs'i takip eden ayin son gunu
        for (int month = 1; month <= 12; month++)
        {
            var nextMonth = month == 12
                ? new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                : new DateTime(year, month + 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var lastDayOfNextMonth = nextMonth.AddMonths(1).AddDays(-1);
            var ev = CalendarEvent.Create(
                tenantId,
                $"Ba-Bs Form — {year}/{month:D2}",
                lastDayOfNextMonth.Date,
                lastDayOfNextMonth.Date.AddHours(23).AddMinutes(59),
                EventType.Deadline,
                isAllDay: true,
                description: $"{year} yili {month}. ay Ba-Bs formlari son teslim tarihi.");
            await _repository.AddAsync(ev, cancellationToken);
            count++;
        }

        // 5. Yillik gelir vergisi beyannamesi — takip eden yilin Mart 31
        var annualDeadline = new DateTime(year + 1, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        var annualEvent = CalendarEvent.Create(
            tenantId,
            $"Yillik Gelir Vergisi Beyannamesi — {year}",
            annualDeadline.Date,
            annualDeadline.Date.AddHours(23).AddMinutes(59),
            EventType.Deadline,
            isAllDay: true,
            description: $"{year} yili yillik gelir vergisi beyannamesi son teslim tarihi.");
        await _repository.AddAsync(annualEvent, cancellationToken);
        count++;

        await _uow.SaveChangesAsync(cancellationToken);
        return count;
    }

    /// <summary>
    /// Verilen ayin takip eden ayinda belirtilen gunu dondurur.
    /// Ay 28/29/30 gunlukse, son gune clamp eder.
    /// </summary>
    private static DateTime GetNextMonthDate(int year, int month, int day)
    {
        var nextMonth = month == 12
            ? new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            : new DateTime(year, month + 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var clampedDay = Math.Min(day, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month));
        return new DateTime(nextMonth.Year, nextMonth.Month, clampedDay, 0, 0, 0, DateTimeKind.Utc);
    }
}
