using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

// ════════════════════════════════════════════════════════
// DEV5 TUR 10: ERP bridge event handler tests
// Coverage: ERPNextCustomer, ERPNextSalesInvoice,
//           ERPNextStockEntry
// ════════════════════════════════════════════════════════

#region ERPNextCustomerHandler

[Trait("Category", "Unit")]
[Trait("Layer", "ERP")]
public class ERPNextCustomerHandlerTests
{
    private readonly Mock<IErpBridgeService> _erpBridge = new();
    private readonly Mock<ILogger<ERPNextCustomerHandler>> _logger = new();

    private ERPNextCustomerHandler CreateSut() =>
        new(_erpBridge.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCallErpBridgeCreateCustomer()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "Acme Corp",
            "info@acme.com", "+905551234567", CancellationToken.None);

        _erpBridge.Verify(e => e.CreateCustomerAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NullEmailAndPhone_ShouldNotThrow()
    {
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "Anonymous Customer",
            null, null, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

#endregion

#region ERPNextSalesInvoiceHandler

[Trait("Category", "Unit")]
[Trait("Layer", "ERP")]
public class ERPNextSalesInvoiceHandlerTests
{
    private readonly Mock<IErpBridgeService> _erpBridge = new();
    private readonly Mock<ILogger<ERPNextSalesInvoiceHandler>> _logger = new();

    private ERPNextSalesInvoiceHandler CreateSut() =>
        new(_erpBridge.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCallErpBridgeCreateInvoice()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "ORD-2026-001",
            1250.00m, "Acme Corp", CancellationToken.None);

        _erpBridge.Verify(e => e.CreateSalesInvoiceAsync(
            It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_NullCustomerName_ShouldNotThrow()
    {
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(
            Guid.NewGuid(), Guid.NewGuid(), "ORD-002",
            500.00m, null, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

#endregion

#region ERPNextStockEntryHandler

[Trait("Category", "Unit")]
[Trait("Layer", "ERP")]
public class ERPNextStockEntryHandlerTests
{
    private readonly Mock<IErpBridgeService> _erpBridge = new();
    private readonly Mock<ILogger<ERPNextStockEntryHandler>> _logger = new();

    private ERPNextStockEntryHandler CreateSut() =>
        new(_erpBridge.Object, _logger.Object);

    [Fact]
    public async Task HandleAsync_ShouldCallErpBridgeCreateStockEntry()
    {
        var sut = CreateSut();

        await sut.HandleAsync(
            Guid.NewGuid(), "SKU-001", Guid.NewGuid(),
            100, 95, "Satis", CancellationToken.None);

        _erpBridge.Verify(e => e.CreateStockEntryAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_StockIncrease_ShouldHandlePositiveDelta()
    {
        var sut = CreateSut();

        var act = async () => await sut.HandleAsync(
            Guid.NewGuid(), "SKU-002", Guid.NewGuid(),
            50, 75, "Tedarik", CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}

#endregion
