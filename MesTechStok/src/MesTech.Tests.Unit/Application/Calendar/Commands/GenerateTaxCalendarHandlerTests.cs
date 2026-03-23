using FluentAssertions;
using MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;
using MesTech.Domain.Entities.Calendar;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Calendar.Commands;

[Trait("Category", "Unit")]
public class GenerateTaxCalendarHandlerTests
{
    private readonly Mock<ICalendarEventRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private GenerateTaxCalendarHandler CreateHandler() =>
        new(_repository.Object, _uow.Object);

    [Fact]
    public async Task Handle_ValidYear_ShouldCreate40Events()
    {
        // Arrange
        var handler = CreateHandler();
        var command = new GenerateTaxCalendarCommand(2026, Guid.NewGuid());

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert — 12 KDV + 3 Gecici + 12 SGK + 12 BaBs + 1 Yillik = 40
        result.Should().Be(40);
        _repository.Verify(
            r => r.AddAsync(It.IsAny<CalendarEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(40));
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task Handle_DecemberKdv_ShouldCreateNextYearDeadline()
    {
        // Arrange — December KDV deadline should land in January of next year
        CalendarEvent? capturedDecEvent = null;
        int addCallCount = 0;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<CalendarEvent>(), It.IsAny<CancellationToken>()))
            .Callback<CalendarEvent, CancellationToken>((ev, _) =>
            {
                addCallCount++;
                // 12th KDV event = index 12 (1-based) = the December KDV
                if (addCallCount == 12)
                    capturedDecEvent = ev;
            });

        var handler = CreateHandler();
        var command = new GenerateTaxCalendarCommand(2026, Guid.NewGuid());

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — December KDV deadline is January 26 of next year
        capturedDecEvent.Should().NotBeNull();
        capturedDecEvent!.StartAt.Year.Should().Be(2027);
        capturedDecEvent.StartAt.Month.Should().Be(1);
        capturedDecEvent.StartAt.Day.Should().Be(26);
    }

    [Fact]
    public async Task Handle_AnnualIncomeTax_ShouldBeNextYearMarch31()
    {
        // Arrange
        CalendarEvent? lastEvent = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<CalendarEvent>(), It.IsAny<CancellationToken>()))
            .Callback<CalendarEvent, CancellationToken>((ev, _) => lastEvent = ev);

        var handler = CreateHandler();
        var command = new GenerateTaxCalendarCommand(2025, Guid.NewGuid());

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert — last event is annual income tax, deadline = 2026-03-31
        lastEvent.Should().NotBeNull();
        lastEvent!.StartAt.Should().Be(new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc));
    }
}
