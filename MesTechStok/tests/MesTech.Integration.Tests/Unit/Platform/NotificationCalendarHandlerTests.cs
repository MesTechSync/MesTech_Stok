using FluentAssertions;
using MesTech.Application.Features.Notifications.Commands.MarkNotificationRead;
using MesTech.Application.Features.Notifications.Commands.SendNotification;
using MesTech.Application.Features.Notifications.Commands.UpdateNotificationSettings;
using MesTech.Application.Features.Notifications.Queries.GetNotifications;
using MesTech.Application.Features.Notifications.Queries.GetNotificationSettings;
using MesTech.Application.Features.Calendar.Commands.CreateCalendarEvent;
using MesTech.Application.Features.Calendar.Commands.DeleteCalendarEvent;
using MesTech.Application.Features.Calendar.Commands.UpdateCalendarEvent;
using MesTech.Application.Features.Calendar.Commands.GenerateTaxCalendar;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEventById;
using MesTech.Application.Features.Calendar.Queries.GetCalendarEvents;
using MesTech.Application.Interfaces;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Platform;

[Trait("Category", "Unit")]
[Trait("Layer", "Notification")]
[Trait("Group", "Handler")]
public class NotificationCalendarHandlerTests
{
    private readonly Mock<IUnitOfWork> _uow = new();

    public NotificationCalendarHandlerTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    // ═══ NOTIFICATION HANDLERS ═══

    [Fact]
    public async Task MarkNotificationRead_NullRequest_Throws()
    {
        var repo = new Mock<INotificationLogRepository>();
        var handler = new MarkNotificationReadHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task SendNotification_NullRequest_Throws()
    {
        var repo = new Mock<INotificationLogRepository>();
        var publisher = new Mock<IMessagePublisher>();
        var monitor = new Mock<IMesaEventMonitor>();
        var logger = Mock.Of<ILogger<SendNotificationHandler>>();
        var handler = new SendNotificationHandler(repo.Object, _uow.Object, publisher.Object, monitor.Object, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateNotificationSettings_NullRequest_Throws()
    {
        var repo = new Mock<INotificationSettingRepository>();
        var logger = Mock.Of<ILogger<UpdateNotificationSettingsHandler>>();
        var handler = new UpdateNotificationSettingsHandler(repo.Object, _uow.Object, logger);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetNotifications_NullRequest_Throws()
    {
        var repo = new Mock<INotificationLogRepository>();
        var handler = new GetNotificationsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetNotificationSettings_NullRequest_Throws()
    {
        var repo = new Mock<INotificationSettingRepository>();
        var handler = new GetNotificationSettingsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    // ═══ CALENDAR HANDLERS ═══

    [Fact]
    public async Task CreateCalendarEvent_NullRequest_Throws()
    {
        var repo = new Mock<ICalendarEventRepository>();
        var handler = new CreateCalendarEventHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteCalendarEvent_NullRequest_Throws()
    {
        var repo = new Mock<ICalendarEventRepository>();
        var handler = new DeleteCalendarEventHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateCalendarEvent_NullRequest_Throws()
    {
        var repo = new Mock<ICalendarEventRepository>();
        var handler = new UpdateCalendarEventHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateTaxCalendar_NullRequest_Throws()
    {
        var repo = new Mock<ICalendarEventRepository>();
        var handler = new GenerateTaxCalendarHandler(repo.Object, _uow.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetCalendarEventById_NullRequest_Throws()
    {
        var repo = new Mock<ICalendarEventRepository>();
        var handler = new GetCalendarEventByIdHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task GetCalendarEvents_NullRequest_Throws()
    {
        var repo = new Mock<ICalendarEventRepository>();
        var handler = new GetCalendarEventsHandler(repo.Object);
        await Assert.ThrowsAnyAsync<Exception>(() => handler.Handle(null!, CancellationToken.None));
    }
}
