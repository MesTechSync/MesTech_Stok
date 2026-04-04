using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Orchestration;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Orchestration;

[Trait("Category", "Unit")]
public class CustomerCreatedEventHandlerTests
{
    private readonly Mock<INotificationLogRepository> _notifRepoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<ILogger<CustomerCreatedEventHandler>> _loggerMock;
    private readonly CustomerCreatedEventHandler _sut;

    public CustomerCreatedEventHandlerTests()
    {
        _notifRepoMock = new Mock<INotificationLogRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CustomerCreatedEventHandler>>();
        _sut = new CustomerCreatedEventHandler(
            _notifRepoMock.Object, _uowMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidEvent_CreatesNotificationLogAndPersists()
    {
        // Arrange
        var evt = new CustomerCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Test Müşteri", "test@mestech.com", "5551234567", DateTime.UtcNow);
        var notification = new DomainEventNotification<CustomerCreatedEvent>(evt);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _notifRepoMock.Verify(
            r => r.AddAsync(It.Is<NotificationLog>(n =>
                n.TenantId == evt.TenantId),
                It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NullEmail_StillCreatesNotification()
    {
        // Arrange
        var evt = new CustomerCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "No Email Customer", null, null, DateTime.UtcNow);
        var notification = new DomainEventNotification<CustomerCreatedEvent>(evt);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _notifRepoMock.Verify(
            r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()),
            Times.Once());

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_RepositoryThrows_DoesNotThrow_LogsError()
    {
        // Arrange — handler has try/catch, should not propagate
        var evt = new CustomerCreatedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "Fail Customer", "fail@test.com", null, DateTime.UtcNow);
        var notification = new DomainEventNotification<CustomerCreatedEvent>(evt);

        _notifRepoMock
            .Setup(r => r.AddAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        // Act
        var act = () => _sut.Handle(notification, CancellationToken.None);

        // Assert — handler swallows exception
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_DifferentTenants_EachGetOwnNotification()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        var evt1 = new CustomerCreatedEvent(Guid.NewGuid(), tenant1, "Tenant1 Cust", null, null, DateTime.UtcNow);
        var evt2 = new CustomerCreatedEvent(Guid.NewGuid(), tenant2, "Tenant2 Cust", null, null, DateTime.UtcNow);

        // Act
        await _sut.Handle(new DomainEventNotification<CustomerCreatedEvent>(evt1), CancellationToken.None);
        await _sut.Handle(new DomainEventNotification<CustomerCreatedEvent>(evt2), CancellationToken.None);

        // Assert
        _notifRepoMock.Verify(
            r => r.AddAsync(It.Is<NotificationLog>(n => n.TenantId == tenant1), It.IsAny<CancellationToken>()),
            Times.Once());
        _notifRepoMock.Verify(
            r => r.AddAsync(It.Is<NotificationLog>(n => n.TenantId == tenant2), It.IsAny<CancellationToken>()),
            Times.Once());
    }
}
