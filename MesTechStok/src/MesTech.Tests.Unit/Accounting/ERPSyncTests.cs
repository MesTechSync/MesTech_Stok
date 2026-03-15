using FluentAssertions;
using MesTech.Application.DTOs.ERP;
using MesTech.Application.Interfaces.Erp;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting;

/// <summary>
/// ERP senkronizasyon event testleri.
/// InvoiceGeneratedForERPEvent property kontrolu, IERPSyncHandler mock davranisi
/// ve hata senaryolarini kapsar.
/// </summary>
[Trait("Category", "Unit")]
public class ERPSyncTests
{
    // ── 1. InvoiceGeneratedForERPEvent_HasCorrectProperties ──────────────────

    [Fact]
    public void InvoiceGeneratedForERPEvent_HasCorrectProperties()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var invoiceNumber = "INV-2026-001";
        var totalAmount = 1180.00m;
        var targetErp = "Parasut";
        var occurredAt = DateTime.UtcNow;

        // Act
        var evt = new InvoiceGeneratedForERPEvent(
            invoiceId,
            invoiceNumber,
            totalAmount,
            targetErp,
            occurredAt);

        // Assert
        evt.InvoiceId.Should().Be(invoiceId);
        evt.InvoiceNumber.Should().Be(invoiceNumber);
        evt.TotalAmount.Should().Be(totalAmount);
        evt.TargetERP.Should().Be(targetErp);
        evt.OccurredAt.Should().Be(occurredAt);
    }

    // ── 2. InvoiceGeneratedForERPEvent_OccurredAt_Set ─────────────────────────

    [Fact]
    public void InvoiceGeneratedForERPEvent_OccurredAt_Set()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var evt = new InvoiceGeneratedForERPEvent(
            Guid.NewGuid(),
            "INV-2026-002",
            500m,
            "BizimHesap",
            DateTime.UtcNow);

        var after = DateTime.UtcNow;

        // Assert — OccurredAt is within a valid time window
        evt.OccurredAt.Should().BeOnOrAfter(before);
        evt.OccurredAt.Should().BeOnOrBefore(after);
    }

    // ── 3. HandleInvoiceCreated_CallsERPAdapter ───────────────────────────────

    [Fact]
    public async Task HandleInvoiceCreated_CallsERPAdapter()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();
        var syncResult = ErpSyncResult.Ok("PARASUT-1001");

        var adapterMock = new Mock<IErpAdapter>();
        adapterMock.Setup(a => a.Provider).Returns(ErpProvider.Parasut);
        adapterMock.Setup(a => a.SyncInvoiceAsync(invoiceId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(syncResult);

        var factoryMock = new Mock<IErpAdapterFactory>();
        factoryMock.Setup(f => f.GetAdapter(ErpProvider.Parasut))
                   .Returns(adapterMock.Object);

        var loggerMock = new Mock<ILogger<ErpSyncHandlerStub>>();
        var sut = new ErpSyncHandlerStub(factoryMock.Object, loggerMock.Object);

        // Act
        await sut.HandleInvoiceCreatedAsync(invoiceId, ErpProvider.Parasut);

        // Assert — ERP adapter's SyncInvoiceAsync was called exactly once with the correct invoiceId
        adapterMock.Verify(
            a => a.SyncInvoiceAsync(invoiceId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── 4. HandleOrderReceived_CallsERPAdapter ───────────────────────────────

    [Fact]
    public async Task HandleOrderReceived_CallsERPAdapter()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var syncResult = ErpSyncResult.Ok("PARASUT-ORD-5001");

        var adapterMock = new Mock<IErpAdapter>();
        adapterMock.Setup(a => a.Provider).Returns(ErpProvider.Parasut);
        adapterMock.Setup(a => a.SyncOrderAsync(orderId, It.IsAny<CancellationToken>()))
                   .ReturnsAsync(syncResult);

        var factoryMock = new Mock<IErpAdapterFactory>();
        factoryMock.Setup(f => f.GetAdapter(ErpProvider.Parasut))
                   .Returns(adapterMock.Object);

        var loggerMock = new Mock<ILogger<ErpSyncHandlerStub>>();
        var sut = new ErpSyncHandlerStub(factoryMock.Object, loggerMock.Object);

        // Act
        await sut.HandleOrderReceivedAsync(orderId, ErpProvider.Parasut);

        // Assert — ERP adapter's SyncOrderAsync was called exactly once with correct orderId
        adapterMock.Verify(
            a => a.SyncOrderAsync(orderId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── 5. HandleInvoiceCreated_ERPUnavailable_LogsError ────────────────────

    [Fact]
    public async Task HandleInvoiceCreated_ERPUnavailable_LogsError()
    {
        // Arrange
        var invoiceId = Guid.NewGuid();

        var adapterMock = new Mock<IErpAdapter>();
        adapterMock.Setup(a => a.Provider).Returns(ErpProvider.BizimHesap);
        adapterMock.Setup(a => a.SyncInvoiceAsync(invoiceId, It.IsAny<CancellationToken>()))
                   .ThrowsAsync(new HttpRequestException("ERP API unavailable"));

        var factoryMock = new Mock<IErpAdapterFactory>();
        factoryMock.Setup(f => f.GetAdapter(ErpProvider.BizimHesap))
                   .Returns(adapterMock.Object);

        var loggerMock = new Mock<ILogger<ErpSyncHandlerStub>>();
        var sut = new ErpSyncHandlerStub(factoryMock.Object, loggerMock.Object);

        // Act — should NOT throw; error must be swallowed and logged
        var act = async () => await sut.HandleInvoiceCreatedAsync(invoiceId, ErpProvider.BizimHesap);

        // Assert — no exception propagated to caller
        await act.Should().NotThrowAsync();

        // Assert — error was logged (LogError called at least once)
        loggerMock.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }
}

/// <summary>
/// ErpSyncHandler test stub — IErpAdapterFactory + ILogger kullanarak
/// HandleInvoiceCreatedAsync ve HandleOrderReceivedAsync test eder.
/// Dalga 11 gercek handler implementasyonu beklenirken kullanilir.
/// </summary>
internal sealed class ErpSyncHandlerStub
{
    private readonly IErpAdapterFactory _factory;
    private readonly ILogger<ErpSyncHandlerStub> _logger;

    public ErpSyncHandlerStub(IErpAdapterFactory factory, ILogger<ErpSyncHandlerStub> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task HandleInvoiceCreatedAsync(Guid invoiceId, ErpProvider provider, CancellationToken ct = default)
    {
        try
        {
            var adapter = _factory.GetAdapter(provider);
            await adapter.SyncInvoiceAsync(invoiceId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERP sync failed for invoice {InvoiceId} via {Provider}", invoiceId, provider);
        }
    }

    public async Task HandleOrderReceivedAsync(Guid orderId, ErpProvider provider, CancellationToken ct = default)
    {
        try
        {
            var adapter = _factory.GetAdapter(provider);
            await adapter.SyncOrderAsync(orderId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERP sync failed for order {OrderId} via {Provider}", orderId, provider);
        }
    }
}
