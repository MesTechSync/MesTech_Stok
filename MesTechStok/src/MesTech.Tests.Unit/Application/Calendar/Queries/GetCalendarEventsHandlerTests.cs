using FluentAssertions;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEvents;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Calendar.Queries;

[Trait("Category", "Unit")]
public class GetCalendarEventsHandlerTests
{
    private readonly Mock<ICalendarEventRepository> _repoMock = new();
    private readonly GetCalendarEventsHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();

    public GetCalendarEventsHandlerTests()
    {
        _sut = new GetCalendarEventsHandler(_repoMock.Object);
    }

    [Fact]
    public async Task Handle_EventsExist_ShouldReturnMappedDtos()
    {
        // Arrange
        var ev = CalendarEvent.Create(TenantId, "Sprint Review",
            new DateTime(2026, 3, 24, 14, 0, 0),
            new DateTime(2026, 3, 24, 15, 0, 0),
            EventType.Meeting);
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 31);
        _repoMock.Setup(r => r.GetByDateRangeAsync(TenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent> { ev }.AsReadOnly());

        var query = new GetCalendarEventsQuery(TenantId, from, to);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoEvents_ShouldReturnEmptyList()
    {
        // Arrange
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 1, 31);
        _repoMock.Setup(r => r.GetByDateRangeAsync(TenantId, from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent>().AsReadOnly());

        // Act
        var result = await _sut.Handle(new GetCalendarEventsQuery(TenantId, from, to), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
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
    public async Task Handle_NullDates_ShouldUseDefaultRange()
    {
        // Arrange
        _repoMock.Setup(r => r.GetByDateRangeAsync(TenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CalendarEvent>().AsReadOnly());

        var query = new GetCalendarEventsQuery(TenantId); // From and To are null

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
        _repoMock.Verify(r => r.GetByDateRangeAsync(
            TenantId,
            It.Is<DateTime>(d => d < DateTime.UtcNow),
            It.Is<DateTime>(d => d > DateTime.UtcNow),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
