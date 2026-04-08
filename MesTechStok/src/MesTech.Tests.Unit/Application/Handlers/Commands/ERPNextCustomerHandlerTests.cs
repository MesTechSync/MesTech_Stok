using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ERPNextCustomerHandlerTests
{
    private readonly Mock<IErpBridgeService> _erpBridgeMock = new();
    private readonly Mock<ILogger<ERPNextCustomerHandler>> _loggerMock = new();

    private ERPNextCustomerHandler CreateHandler() =>
        new(_erpBridgeMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleAsync_HappyPath_ShouldCallPushCustomer()
    {
        var handler = CreateHandler();
        var customerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await handler.HandleAsync(customerId, tenantId, "John Doe", "john@example.com", "+905551234567");

        _erpBridgeMock.Verify(e => e.PushCustomerAsync(
            tenantId, customerId, "John Doe", "john@example.com", "+905551234567",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NullEmailAndPhone_ShouldNotThrow()
    {
        var handler = CreateHandler();

        var act = () => handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "Test Customer", null, null);

        await act.Should().NotThrowAsync();
        _erpBridgeMock.Verify(e => e.PushCustomerAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), "Test Customer", null, null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken()
    {
        var handler = CreateHandler();
        using var cts = new CancellationTokenSource();

        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "Customer", null, null, cts.Token);

        _erpBridgeMock.Verify(e => e.PushCustomerAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string?>(), It.IsAny<string?>(), cts.Token), Times.Once);
    }
}
