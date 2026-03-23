using FluentAssertions;
using MesTech.Application.Features.Calendar.Commands.UpdateCalendarEvent;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Calendar.Commands;

[Trait("Category", "Unit")]
public class UpdateCalendarEventHandlerTests
{
    private readonly Mock<ICalendarEventRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly UpdateCalendarEventHandler _sut;

    public UpdateCalendarEventHandlerTests()
    {
        _sut = new UpdateCalendarEventHandler(_repoMock.Object, _uowMock.Object);
    }

    private static CalendarEvent CreateTestEvent()
    {
        return CalendarEvent.Create(
            tenantId: Guid.NewGuid(),
            title: "Test Event",
            startAt: new DateTime(2026, 3, 24, 10, 0, 0),
            endAt: new DateTime(2026, 3, 24, 11, 0, 0),
            type: EventType.Meeting);
    }

    [Fact]
    public async Task Handle_MarkAsCompleted_ShouldUpdateAndSave()
    {
        // Arrange
        var ev = CreateTestEvent();
        _repoMock.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);
        var command = new UpdateCalendarEventCommand(ev.Id, IsCompleted: true);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        ev.IsCompleted.Should().BeTrue();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MarkAsIncomplete_ShouldResetCompletedState()
    {
        // Arrange
        var ev = CreateTestEvent();
        ev.MarkAsCompleted(); // Pre-complete
        _repoMock.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);
        var command = new UpdateCalendarEventCommand(ev.Id, IsCompleted: false);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        ev.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_EventNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CalendarEvent?)null);
        var command = new UpdateCalendarEventCommand(missingId, IsCompleted: true);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{missingId}*");
    }

    [Fact]
    public async Task Handle_NoIsCompletedChange_ShouldStillSave()
    {
        // Arrange
        var ev = CreateTestEvent();
        _repoMock.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);
        var command = new UpdateCalendarEventCommand(ev.Id, IsCompleted: null);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        ev.IsCompleted.Should().BeFalse(); // unchanged
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
