using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Finance.Commands.CreateExpense;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Messaging.Handlers;
using MesTech.Infrastructure.Messaging.Mesa;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Messaging;

[Trait("Category", "Unit")]
public class InvoiceGeneratedExpenseHandlerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<InvoiceGeneratedExpenseHandler>> _loggerMock;
    private readonly InvoiceGeneratedExpenseHandler _sut;

    public InvoiceGeneratedExpenseHandlerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<InvoiceGeneratedExpenseHandler>>();
        _sut = new InvoiceGeneratedExpenseHandler(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidInvoice_DispatchesCreateExpenseCommand()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var invoiceId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var evt = new InvoiceCreatedEvent(invoiceId, orderId, tenantId, InvoiceType.EFatura, 2500m, DateTime.UtcNow);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(evt);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateExpenseCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(It.Is<CreateExpenseCommand>(cmd =>
                cmd.TenantId == tenantId &&
                cmd.Amount == 2500m &&
                cmd.Category == ExpenseCategory.Other &&
                cmd.Title.Contains(invoiceId.ToString("N"))),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task Handle_ReturnInvoice_IncludesTypeInTitle()
    {
        // Arrange
        var evt = new InvoiceCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InvoiceType.EArsiv, 100m, DateTime.UtcNow);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(evt);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateExpenseCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(It.Is<CreateExpenseCommand>(cmd =>
                cmd.Title.Contains("EArsiv")),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task Handle_ZeroTotal_StillCreatesExpense()
    {
        // Arrange
        var evt = new InvoiceCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InvoiceType.EFatura, 0m, DateTime.UtcNow);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(evt);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateExpenseCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(It.Is<CreateExpenseCommand>(cmd => cmd.Amount == 0m),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Fact]
    public async Task Handle_MediatorThrows_SwallowsExceptionAndLogs()
    {
        // Arrange — handler has try-catch: exception is logged, not propagated.
        // This prevents one failed expense from breaking the entire event chain.
        var evt = new InvoiceCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InvoiceType.EFatura, 500m, DateTime.UtcNow);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(evt);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateExpenseCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Handler failed"));

        // Act — should NOT throw (handler catches non-cancellation exceptions)
        var act = () => _sut.Handle(notification, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Handle_ExpenseDateMatchesEventOccurredAt()
    {
        // Arrange
        var occurredAt = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);
        var evt = new InvoiceCreatedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), InvoiceType.EFatura, 750m, occurredAt);
        var notification = new DomainEventNotification<InvoiceCreatedEvent>(evt);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateExpenseCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.Handle(notification, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(It.Is<CreateExpenseCommand>(cmd =>
                cmd.ExpenseDate == occurredAt),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }
}
