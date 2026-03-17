using FluentAssertions;
using MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;
using MesTech.Application.Features.Calendar.Commands.DeleteCalendarEvent;
using MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;
using MesTech.Application.Features.Calendar.Commands.UpdateCalendarEvent;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEventById;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEvents;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.Application.Calendar;

[Trait("Category", "Unit")]
[Trait("Feature", "Calendar")]
public class CalendarCommandTests
{
    // ── CreateCalendarEvent Validator ────────────────────────────────────────

    private readonly CreateCalendarEventValidator _createValidator = new();

    private static CreateCalendarEventCommand ValidCreateCommand() => new(
        TenantId: Guid.NewGuid(),
        Title: "Haftalik Toplanti",
        StartAt: new DateTime(2026, 3, 17, 10, 0, 0),
        EndAt: new DateTime(2026, 3, 17, 11, 0, 0),
        Type: EventType.Meeting,
        IsAllDay: false,
        CreatedByUserId: Guid.NewGuid(),
        Description: "Sprint planlama toplantisi",
        Location: "Ofis Toplanti Odasi",
        RelatedOrderId: null,
        RelatedDealId: null,
        RelatedWorkTaskId: null,
        AttendeeUserIds: null
    );

    [Fact]
    public async Task CreateCalendar_ValidCommand_PassesValidation()
    {
        var result = await _createValidator.ValidateAsync(ValidCreateCommand());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCalendar_EmptyTenantId_FailsValidation()
    {
        var cmd = ValidCreateCommand() with { TenantId = Guid.Empty };
        var result = await _createValidator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCalendar_EmptyTitle_FailsValidation()
    {
        var cmd = ValidCreateCommand() with { Title = "" };
        var result = await _createValidator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCalendar_TitleTooLong_FailsValidation()
    {
        var cmd = ValidCreateCommand() with { Title = new string('T', 301) };
        var result = await _createValidator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCalendar_EndBeforeStart_NonAllDay_FailsValidation()
    {
        var cmd = ValidCreateCommand() with
        {
            StartAt = new DateTime(2026, 3, 17, 14, 0, 0),
            EndAt = new DateTime(2026, 3, 17, 10, 0, 0),
            IsAllDay = false
        };
        var result = await _createValidator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCalendar_EndBeforeStart_AllDay_PassesValidation()
    {
        var cmd = ValidCreateCommand() with
        {
            StartAt = new DateTime(2026, 3, 17, 14, 0, 0),
            EndAt = new DateTime(2026, 3, 17, 10, 0, 0),
            IsAllDay = true
        };
        var result = await _createValidator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task CreateCalendar_DescriptionTooLong_FailsValidation()
    {
        var cmd = ValidCreateCommand() with { Description = new string('D', 2001) };
        var result = await _createValidator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task CreateCalendar_LocationTooLong_FailsValidation()
    {
        var cmd = ValidCreateCommand() with { Location = new string('L', 501) };
        var result = await _createValidator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
    }

    // ── Command/Query object property tests ─────────────────────────────────

    [Fact]
    public void DeleteCalendarEventCommand_HasCorrectId()
    {
        var id = Guid.NewGuid();
        var cmd = new DeleteCalendarEventCommand(id);
        cmd.Id.Should().Be(id);
    }

    [Fact]
    public void UpdateCalendarEventCommand_HasCorrectProperties()
    {
        var id = Guid.NewGuid();
        var cmd = new UpdateCalendarEventCommand(id, IsCompleted: true);
        cmd.Id.Should().Be(id);
        cmd.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void GenerateTaxCalendarCommand_HasCorrectProperties()
    {
        var tenantId = Guid.NewGuid();
        var cmd = new GenerateTaxCalendarCommand(Year: 2026, TenantId: tenantId);
        cmd.Year.Should().Be(2026);
        cmd.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void GetCalendarEventsQuery_HasCorrectProperties()
    {
        var tenantId = Guid.NewGuid();
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 12, 31);
        var query = new GetCalendarEventsQuery(tenantId, from, to);
        query.TenantId.Should().Be(tenantId);
        query.From.Should().Be(from);
        query.To.Should().Be(to);
    }

    [Fact]
    public void GetCalendarEventByIdQuery_HasCorrectId()
    {
        var id = Guid.NewGuid();
        var query = new GetCalendarEventByIdQuery(id);
        query.Id.Should().Be(id);
    }

    [Fact]
    public void GetCalendarEventsQuery_DefaultDates_AreNull()
    {
        var query = new GetCalendarEventsQuery(Guid.NewGuid());
        query.From.Should().BeNull();
        query.To.Should().BeNull();
    }
}
