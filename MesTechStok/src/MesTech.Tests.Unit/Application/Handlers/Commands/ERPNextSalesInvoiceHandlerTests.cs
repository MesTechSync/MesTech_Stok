using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ERPNextSalesInvoiceHandlerTests
{
    private readonly Mock<IErpBridgeService> _erpBridgeMock = new();
    private readonly Mock<ILogger<ERPNextSalesInvoiceHandler>> _loggerMock = new();

    private ERPNextSalesInvoiceHandler CreateHandler() =>
        new(_erpBridgeMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleAsync_HappyPath_ShouldCallPushSalesInvoice()
    {
        var handler = CreateHandler();
        var orderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await handler.HandleAsync(orderId, tenantId, "ORD-001", 1500.50m, "Ahmet Yilmaz");

        _erpBridgeMock.Verify(e => e.PushSalesInvoiceAsync(
            tenantId, orderId, "ORD-001", 1500.50m, "Ahmet Yilmaz",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NullCustomerName_ShouldNotThrow()
    {
        var handler = CreateHandler();

        var act = () => handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-002", 250m, null);

        await act.Should().NotThrowAsync();
        _erpBridgeMock.Verify(e => e.PushSalesInvoiceAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), "ORD-002", 250m, null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken()
    {
        var handler = CreateHandler();
        using var cts = new CancellationTokenSource();

        await handler.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-003", 100m, "Test", cts.Token);

        _erpBridgeMock.Verify(e => e.PushSalesInvoiceAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<decimal>(), It.IsAny<string?>(), cts.Token), Times.Once);
    }
}
