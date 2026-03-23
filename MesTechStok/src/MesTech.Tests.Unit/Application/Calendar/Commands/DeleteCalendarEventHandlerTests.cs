using FluentAssertions;
using MesTech.Application.Features.Calendar.Commands.DeleteCalendarEvent;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Calendar.Commands;

[Trait("Category", "Unit")]
public class DeleteCalendarEventHandlerTests
{
    private readonly Mock<ICalendarEventRepository> _repoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly DeleteCalendarEventHandler _sut;

    public DeleteCalendarEventHandlerTests()
    {
        _sut = new DeleteCalendarEventHandler(_repoMock.Object, _uowMock.Object);
    }

    private static CalendarEvent CreateTestEvent()
    {
        return CalendarEvent.Create(
            tenantId: Guid.NewGuid(),
            title: "Event To Delete",
            startAt: new DateTime(2026, 3, 24, 10, 0, 0),
            endAt: new DateTime(2026, 3, 24, 11, 0, 0),
            type: EventType.Custom);
    }

    [Fact]
    public async Task Handle_EventExists_ShouldSoftDeleteAndSave()
    {
        // Arrange
        var ev = CreateTestEvent();
        _repoMock.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);
        var command = new DeleteCalendarEventCommand(ev.Id);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        ev.IsDeleted.Should().BeTrue();
        ev.DeletedAt.Should().NotBeNull();
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EventNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CalendarEvent?)null);
        var command = new DeleteCalendarEventCommand(missingId);

        // Act
        var act = () => _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{missingId}*");
    }

    [Fact]
    public async Task Handle_EventExists_ShouldSetDeletedAtToUtcNow()
    {
        // Arrange
        var ev = CreateTestEvent();
        var beforeDelete = DateTime.UtcNow;
        _repoMock.Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);

        // Act
        await _sut.Handle(new DeleteCalendarEventCommand(ev.Id), CancellationToken.None);

        // Assert
        ev.DeletedAt.Should().BeOnOrAfter(beforeDelete);
        ev.DeletedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }
}
