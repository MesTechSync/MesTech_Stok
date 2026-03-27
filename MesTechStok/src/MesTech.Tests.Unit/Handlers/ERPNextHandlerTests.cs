using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class ERPNextHandlerTests
{
    private readonly Mock<IErpBridgeService> _erpBridgeMock = new();

    [Fact]
    public async Task ERPNextCustomerHandler_ValidInput_CallsErpBridge()
    {
        var sut = new ERPNextCustomerHandler(
            _erpBridgeMock.Object, Mock.Of<ILogger<ERPNextCustomerHandler>>());

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "Test Customer", "test@test.com", "0555", CancellationToken.None);

        _erpBridgeMock.Verify(e => e.PushAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ERPNextSalesInvoiceHandler_ValidInput_CallsErpBridge()
    {
        var sut = new ERPNextSalesInvoiceHandler(
            _erpBridgeMock.Object, Mock.Of<ILogger<ERPNextSalesInvoiceHandler>>());

        await sut.HandleAsync(Guid.NewGuid(), Guid.NewGuid(), "ORD-001", 1500m, "Customer", CancellationToken.None);

        _erpBridgeMock.Verify(e => e.PushAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ERPNextStockEntryHandler_ValidInput_CallsErpBridge()
    {
        var sut = new ERPNextStockEntryHandler(
            _erpBridgeMock.Object, Mock.Of<ILogger<ERPNextStockEntryHandler>>());

        await sut.HandleAsync(Guid.NewGuid(), "SKU-001", Guid.NewGuid(), 10, 15, "Stock In", CancellationToken.None);

        _erpBridgeMock.Verify(e => e.PushAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
