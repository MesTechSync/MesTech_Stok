using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;
using MesTech.Application.Features.Calendar.Commands.DeleteCalendarEvent;
using MesTech.Application.Features.Calendar.Commands.UpdateCalendarEvent;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEventById;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEvents;
using MesTech.Application.Features.Onboarding.Commands.StartOnboarding;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Entities.Onboarding;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — Calendar & Onboarding Handler Tests
// CreateCalendarEvent, UpdateCalendarEvent, DeleteCalendarEvent,
// GetCalendarEvents, GetCalendarEventById, StartOnboarding
// ═══════════════════════════════════════════════════════════════

#region CreateCalendarEventHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Calendar")]
public class CreateCalendarEventHandlerTests2
{
    private readonly Mock<ICalendarEventRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly CreateCalendarEventHandler _sut;

    public CreateCalendarEventHandlerTests2()
    {
        _sut = new CreateCalendarEventHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsGuid()
    {
        var cmd = new CreateCalendarEventCommand(
            Guid.NewGuid(), "Team Meeting",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1));

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<CalendarEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithAttendees_AddsAttendees()
    {
        var attendees = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var cmd = new CreateCalendarEventCommand(
            Guid.NewGuid(), "Sprint Review",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(2),
            AttendeeUserIds: attendees);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<CalendarEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region UpdateCalendarEventHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Calendar")]
public class UpdateCalendarEventHandlerTests2
{
    private readonly Mock<ICalendarEventRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdateCalendarEventHandler _sut;

    public UpdateCalendarEventHandlerTests2()
    {
        _sut = new UpdateCalendarEventHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_EventNotFound_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CalendarEvent?)null);

        var cmd = new UpdateCalendarEventCommand(Guid.NewGuid(), IsCompleted: true);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MarkCompleted_SavesChanges()
    {
        var ev = CalendarEvent.Create(
            Guid.NewGuid(), "Test Event",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1),
            EventType.Custom, false, Guid.NewGuid());

        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);

        var cmd = new UpdateCalendarEventCommand(ev.Id, IsCompleted: true);
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().NotThrowAsync();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region DeleteCalendarEventHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Calendar")]
public class DeleteCalendarEventHandlerTests2
{
    private readonly Mock<ICalendarEventRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteCalendarEventHandler _sut;

    public DeleteCalendarEventHandlerTests2()
    {
        _sut = new DeleteCalendarEventHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_EventNotFound_ThrowsKeyNotFoundException()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CalendarEvent?)null);

        var cmd = new DeleteCalendarEventCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_EventExists_SoftDeletesAndSaves()
    {
        var ev = CalendarEvent.Create(
            Guid.NewGuid(), "To Delete",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1),
            EventType.Custom, false, Guid.NewGuid());

        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);

        var cmd = new DeleteCalendarEventCommand(ev.Id);
        var act = () => _sut.Handle(cmd, CancellationToken.None);

        await act.Should().NotThrowAsync();
        ev.IsDeleted.Should().BeTrue();
        ev.DeletedAt.Should().NotBeNull();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetCalendarEventsHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Calendar")]
public class GetCalendarEventsHandlerTests2
{
    private readonly Mock<ICalendarEventRepository> _repoMock = new();
    private readonly GetCalendarEventsHandler _sut;

    public GetCalendarEventsHandlerTests2()
    {
        _sut = new GetCalendarEventsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_ValidQuery_ReturnsEvents()
    {
        _repoMock.Setup(r => r.GetByDateRangeAsync(
                It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent>().AsReadOnly());

        var query = new GetCalendarEventsQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region GetCalendarEventByIdHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Calendar")]
public class GetCalendarEventByIdHandlerTests2
{
    private readonly Mock<ICalendarEventRepository> _repoMock = new();
    private readonly GetCalendarEventByIdHandler _sut;

    public GetCalendarEventByIdHandlerTests2()
    {
        _sut = new GetCalendarEventByIdHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_EventNotFound_ReturnsNull()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CalendarEvent?)null);

        var query = new GetCalendarEventByIdQuery(Guid.NewGuid());
        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullQuery_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion

#region StartOnboardingHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Onboarding")]
public class StartOnboardingHandlerTests
{
    private readonly Mock<IOnboardingProgressRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly StartOnboardingHandler _sut;

    public StartOnboardingHandlerTests()
    {
        _sut = new StartOnboardingHandler(_repoMock.Object, _uowMock.Object);
    }

    [Fact]
    public async Task Handle_NewTenant_CreatesOnboardingProgress()
    {
        _repoMock.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OnboardingProgress?)null);

        var cmd = new StartOnboardingCommand(Guid.NewGuid());
        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.Should().NotBeEmpty();
        _repoMock.Verify(r => r.AddAsync(It.IsAny<OnboardingProgress>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingTenant_ThrowsInvalidOperationException()
    {
        var existing = OnboardingProgress.Start(Guid.NewGuid());
        _repoMock.Setup(r => r.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var cmd = new StartOnboardingCommand(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NullCommand_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _sut.Handle(null!, CancellationToken.None));
    }
}

#endregion
