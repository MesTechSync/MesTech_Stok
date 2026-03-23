using FluentAssertions;
using MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Calendar.Commands;

[Trait("Category", "Unit")]
public class CreateCalendarEventHandlerTests
{
    private readonly Mock<ICalendarEventRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateCalendarEventHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public CreateCalendarEventHandlerTests()
    {
        _sut = new CreateCalendarEventHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateEventAndReturnId()
    {
        // Arrange
        var command = new CreateCalendarEventCommand(
            TenantId: TenantId,
            Title: "Sprint Planning",
            StartAt: new DateTime(2026, 3, 24, 10, 0, 0),
            EndAt: new DateTime(2026, 3, 24, 11, 0, 0),
            Type: EventType.Meeting,
            IsAllDay: false,
            CreatedByUserId: UserId,
            Description: "Weekly sprint planning",
            Location: "Office",
            RelatedOrderId: null,
            RelatedDealId: null,
            RelatedWorkTaskId: null,
            AttendeeUserIds: null);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<CalendarEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAttendees_ShouldAddAttendeesToEvent()
    {
        // Arrange
        var attendeeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        CalendarEvent? capturedEvent = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<CalendarEvent>(), It.IsAny<CancellationToken>()))
            .Callback<CalendarEvent, CancellationToken>((ev, _) => capturedEvent = ev);

        var command = new CreateCalendarEventCommand(
            TenantId: TenantId,
            Title: "Team Standup",
            StartAt: new DateTime(2026, 3, 24, 9, 0, 0),
            EndAt: new DateTime(2026, 3, 24, 9, 15, 0),
            Type: EventType.Meeting,
            IsAllDay: false,
            CreatedByUserId: UserId,
            Description: null,
            Location: null,
            RelatedOrderId: null,
            RelatedDealId: null,
            RelatedWorkTaskId: null,
            AttendeeUserIds: attendeeIds);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Attendees.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_EmptyAttendeeList_ShouldCreateEventWithoutAttendees()
    {
        // Arrange
        CalendarEvent? capturedEvent = null;
        _repoMock.Setup(r => r.AddAsync(It.IsAny<CalendarEvent>(), It.IsAny<CancellationToken>()))
            .Callback<CalendarEvent, CancellationToken>((ev, _) => capturedEvent = ev);

        var command = new CreateCalendarEventCommand(
            TenantId: TenantId,
            Title: "Solo Task",
            StartAt: new DateTime(2026, 3, 24, 14, 0, 0),
            EndAt: new DateTime(2026, 3, 24, 15, 0, 0),
            Type: EventType.Custom,
            IsAllDay: false,
            CreatedByUserId: UserId,
            Description: null,
            Location: null,
            RelatedOrderId: null,
            RelatedDealId: null,
            RelatedWorkTaskId: null,
            AttendeeUserIds: new List<Guid>());

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedEvent.Should().NotBeNull();
        capturedEvent!.Attendees.Should().BeEmpty();
    }
}
