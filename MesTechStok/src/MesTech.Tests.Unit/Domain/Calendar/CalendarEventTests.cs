using FluentAssertions;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Enums;
using MesTech.Domain.Events.Calendar;
using Xunit;

namespace MesTech.Tests.Unit.Domain.Calendar;

/// <summary>
/// CalendarEvent entity unit testleri.
/// DEV 5 — H27-5.3 (emirname Task 5.3 uyarlanmis gercek entity'ye gore)
/// Not: AddAttendee/Reschedule/SetRecurrence H27 gercek entity'sinde yok —
///      Create + RaiseDomainEvent davranislari test edildi.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Domain", "Calendar")]
public class CalendarEventTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly DateTime _start = DateTime.UtcNow.AddHours(1);
    private static readonly DateTime _end = DateTime.UtcNow.AddHours(2);

    [Fact]
    public void Create_ValidData_ShouldSetTitleAndType()
    {
        var ev = CalendarEvent.Create(_tenantId, "Toplanti", _start, _end, EventType.Meeting);

        ev.Title.Should().Be("Toplanti");
        ev.Type.Should().Be(EventType.Meeting);
        ev.TenantId.Should().Be(_tenantId);
    }

    [Fact]
    public void Create_EndBeforeStart_ShouldThrow()
    {
        var act = () => CalendarEvent.Create(_tenantId, "Test", _end, _start);
        act.Should().Throw<ArgumentException>("EndAt must be after StartAt");
    }

    [Fact]
    public void Create_EmptyTitle_ShouldThrow()
    {
        var act = () => CalendarEvent.Create(_tenantId, "", _start, _end);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_ShouldRaiseCalendarEventCreatedEvent()
    {
        var ev = CalendarEvent.Create(_tenantId, "Onemli Toplanti", _start, _end);

        ev.DomainEvents.Should().ContainSingle(e => e is CalendarEventCreatedEvent);
    }

    [Fact]
    public void Create_EventCreatedEvent_ContainsCorrectEventId()
    {
        var ev = CalendarEvent.Create(_tenantId, "Test Event", _start, _end);

        var evt = ev.DomainEvents.OfType<CalendarEventCreatedEvent>().Single();
        evt.EventId.Should().Be(ev.Id);
    }

    [Fact]
    public void Create_WithLocation_ShouldPreserveLocation()
    {
        var ev = CalendarEvent.Create(_tenantId, "Toplanti", _start, _end, location: "Toplanti Odasi A");

        ev.Location.Should().Be("Toplanti Odasi A");
    }

    [Fact]
    public void Create_WithRelatedDeal_ShouldSetRelatedDealId()
    {
        var dealId = Guid.NewGuid();
        var ev = CalendarEvent.Create(_tenantId, "Deal Review", _start, _end, relatedDealId: dealId);

        ev.RelatedDealId.Should().Be(dealId);
    }

    [Fact]
    public void Create_AllDayEvent_ShouldSetIsAllDayTrue()
    {
        var ev = CalendarEvent.Create(_tenantId, "Tatil", _start, _start.AddHours(1), isAllDay: true);

        ev.IsAllDay.Should().BeTrue();
    }

    [Theory]
    [InlineData(EventType.Meeting)]
    [InlineData(EventType.Deadline)]
    [InlineData(EventType.Reminder)]
    public void Create_WithEventType_ShouldPreserveType(EventType eventType)
    {
        var ev = CalendarEvent.Create(_tenantId, "Typed Event", _start, _end, type: eventType);

        ev.Type.Should().Be(eventType);
    }

    [Fact]
    public void Create_WithCreatedByUser_ShouldSetCreatedByUserId()
    {
        var userId = Guid.NewGuid();
        var ev = CalendarEvent.Create(_tenantId, "User Event", _start, _end, createdByUserId: userId);

        ev.CreatedByUserId.Should().Be(userId);
    }
}
