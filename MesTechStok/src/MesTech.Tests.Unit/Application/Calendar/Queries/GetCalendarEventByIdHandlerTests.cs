using FluentAssertions;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEventById;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Calendar.Queries;

[Trait("Category", "Unit")]
public class GetCalendarEventByIdHandlerTests
{
    private readonly Mock<ICalendarEventRepository> _repository = new();

    private GetCalendarEventByIdHandler CreateHandler() =>
        new(_repository.Object);

    [Fact]
    public async Task Handle_ExistingEvent_ShouldReturnDto()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ev = CalendarEvent.Create(
            tenantId, "KDV Beyanname",
            DateTime.UtcNow, DateTime.UtcNow.AddHours(1),
            EventType.Deadline);

        _repository
            .Setup(r => r.GetByIdAsync(ev.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ev);

        var handler = CreateHandler();
        var query = new GetCalendarEventByIdQuery(ev.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("KDV Beyanname");
        result.Type.Should().Be(EventType.Deadline);
    }

    [Fact]
    public async Task Handle_NonExistentId_ShouldReturnNull()
    {
        // Arrange
        var missingId = Guid.NewGuid();
        _repository
            .Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CalendarEvent?)null);

        var handler = CreateHandler();
        var query = new GetCalendarEventByIdQuery(missingId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
