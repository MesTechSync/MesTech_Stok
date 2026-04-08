using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class ERPNextStockEntryHandlerTests
{
    private readonly Mock<IErpBridgeService> _erpBridgeMock = new();
    private readonly Mock<ILogger<ERPNextStockEntryHandler>> _loggerMock = new();

    private ERPNextStockEntryHandler CreateHandler() =>
        new(_erpBridgeMock.Object, _loggerMock.Object);

    [Fact]
    public async Task HandleAsync_StockIncrease_ShouldPushMaterialReceipt()
    {
        var handler = CreateHandler();
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await handler.HandleAsync(productId, "SKU-001", tenantId, 10, 25, "Purchase received");

        _erpBridgeMock.Verify(e => e.PushStockEntryAsync(
            tenantId, productId, "SKU-001", "Material Receipt", 15, "Purchase received",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_StockDecrease_ShouldPushMaterialIssue()
    {
        var handler = CreateHandler();
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        await handler.HandleAsync(productId, "SKU-002", tenantId, 30, 20, "Order shipped");

        _erpBridgeMock.Verify(e => e.PushStockEntryAsync(
            tenantId, productId, "SKU-002", "Material Issue", 10, "Order shipped",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NoChange_ShouldPushZeroQuantity()
    {
        var handler = CreateHandler();

        await handler.HandleAsync(Guid.NewGuid(), "SKU-003", Guid.NewGuid(), 50, 50, "Correction");

        _erpBridgeMock.Verify(e => e.PushStockEntryAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), "SKU-003",
            "Material Issue", 0, "Correction",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassCancellationToken()
    {
        var handler = CreateHandler();
        using var cts = new CancellationTokenSource();

        await handler.HandleAsync(Guid.NewGuid(), "SKU-004", Guid.NewGuid(), 10, 20, "Test", cts.Token);

        _erpBridgeMock.Verify(e => e.PushStockEntryAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), cts.Token), Times.Once);
    }
}
