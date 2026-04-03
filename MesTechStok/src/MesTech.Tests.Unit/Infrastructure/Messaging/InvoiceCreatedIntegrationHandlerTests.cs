using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging;
using MesTech.Infrastructure.Messaging.Handlers;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Messaging;

[Trait("Category", "Unit")]
public class InvoiceCreatedIntegrationHandlerTests
{
    private readonly Mock<IIntegrationEventPublisher> _publisherMock;
    private readonly Mock<ILogger<InvoiceCreatedIntegrationHandler>> _loggerMock;
    private readonly InvoiceCreatedIntegrationHandler _sut;

    public InvoiceCreatedIntegrationHandlerTests()
    {
        _publisherMock = new Mock<IIntegrationEventPublisher>();
        _loggerMock = new Mock<ILogger<InvoiceCreatedIntegrationHandler>>();
        _sut = new InvoiceCreatedIntegrationHandler(_publisherMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidInvoiceEvent_PublishesIntegrationEvent()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var evt = new InvoiceCreatedEvent(invoiceId, orderId, Guid.NewGuid(), InvoiceType.EFatura, 1500m, DateTime.UtcNow);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(evt);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _publisherMock.Verify(
            p => p.PublishInvoiceCreatedAsync(
                invoiceId, orderId, string.Empty, 1500m,
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task Handle_ZeroTotal_StillPublishes()
    {
        // Arrange
        var evt = new InvoiceCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InvoiceType.EFatura, 0m, DateTime.UtcNow);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(evt);

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _publisherMock.Verify(
            p => p.PublishInvoiceCreatedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), string.Empty, 0m,
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task Handle_PublisherThrows_PropagatesException()
    {
        // Arrange — no try/catch in this handler, exception should propagate
        var evt = new InvoiceCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InvoiceType.EFatura, 100m, DateTime.UtcNow);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(evt);

        _publisherMock
            .Setup(p => p.PublishInvoiceCreatedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Bus unavailable"));

        // Act
        var act = () => _sut.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Bus unavailable");
    }

    [Fact]
    public async Task Handle_CancellationRequested_PassesTokenToPublisher()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        var evt = new InvoiceCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InvoiceType.EFatura, 250m, DateTime.UtcNow);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(evt);

        // Act
        await _sut.Handle(notification, token);

        // Assert
        _publisherMock.Verify(
            p => p.PublishInvoiceCreatedAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<decimal>(),
                token),
            Times.Once());
    }
}
