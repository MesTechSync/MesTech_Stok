using FluentAssertions;
using MesTech.Application.EventHandlers;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using IUnitOfWork = MesTech.Domain.Interfaces.IUnitOfWork;
using IOrderRepository = MesTech.Domain.Interfaces.IOrderRepository;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.EventHandlers;

/// <summary>
/// DEV 5 — TY-SINIR-003: SettlementImportedOrderPaymentHandler unit tests.
/// Settlement import → Order.MarkAsPaid + SetCommission + SetCargoExpense chain.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Settlement")]
public class SettlementPaymentHandlerTests
{
    private readonly Mock<ISettlementBatchRepository> _settlementRepoMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<SettlementImportedOrderPaymentHandler>> _loggerMock = new();

    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static Order CreateTestOrder(Guid tenantId, string orderNumber) => new()
    {
        TenantId = tenantId,
        OrderNumber = orderNumber,
        CustomerId = Guid.NewGuid(),
        Status = OrderStatus.Confirmed,
        SourcePlatform = PlatformType.Trendyol
    };

    private SettlementImportedOrderPaymentHandler CreateSut() => new(
        _settlementRepoMock.Object,
        _orderRepoMock.Object,
        _uowMock.Object,
        _loggerMock.Object);

    private static SettlementBatch CreateBatchWithLines(params (string orderNumber, decimal gross, decimal commission, decimal cargo, decimal net)[] items)
    {
        var batch = SettlementBatch.Create(TestTenantId, "Trendyol",
            DateTime.UtcNow.AddDays(-7), DateTime.UtcNow,
            items.Sum(i => i.gross), items.Sum(i => i.commission), items.Sum(i => i.net));

        foreach (var item in items)
        {
            var line = SettlementLine.Create(
                TestTenantId, batch.Id, item.orderNumber,
                item.gross, item.commission, 0m, item.cargo, 0m, item.net);
            batch.AddLine(line);
        }

        return batch;
    }

    // ══════════════════════════════════════
    // 1. Tam zincir: Settlement → Order.MarkAsPaid + SetCommission
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_MatchingOrder_ShouldMarkAsPaidAndSetCommission()
    {
        // Arrange
        var batch = CreateBatchWithLines(("ORD-001", 1000m, 150m, 30m, 810m));

        _settlementRepoMock
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var order = CreateTestOrder(TestTenantId,"ORD-001");
        _orderRepoMock
            .Setup(r => r.GetByOrderNumberAsync("ORD-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        // Act
        var result = await sut.HandleAsync(batch.Id, TestTenantId, CancellationToken.None);

        // Assert
        result.Matched.Should().Be(1);
        result.NotFound.Should().Be(0);
        result.TotalLines.Should().Be(1);
        order.PaymentStatus.Should().Be("Paid");

        _orderRepoMock.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ══════════════════════════════════════
    // 2. Sipariş bulunamadı
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_OrderNotFound_ShouldReturnNotFound()
    {
        var batch = CreateBatchWithLines(("ORD-MISSING", 500m, 75m, 0m, 425m));

        _settlementRepoMock
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        _orderRepoMock
            .Setup(r => r.GetByOrderNumberAsync("ORD-MISSING", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var sut = CreateSut();

        var result = await sut.HandleAsync(batch.Id, TestTenantId, CancellationToken.None);

        result.NotFound.Should().Be(1);
        result.Matched.Should().Be(0);
    }

    // ══════════════════════════════════════
    // 3. Zaten ödenmiş → idempotent
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_AlreadyPaid_ShouldSkip()
    {
        var batch = CreateBatchWithLines(("ORD-PAID", 500m, 75m, 0m, 425m));

        _settlementRepoMock
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var order = CreateTestOrder(TestTenantId,"ORD-PAID");
        order.MarkAsPaid(); // zaten paid

        _orderRepoMock
            .Setup(r => r.GetByOrderNumberAsync("ORD-PAID", It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var sut = CreateSut();

        var result = await sut.HandleAsync(batch.Id, TestTenantId, CancellationToken.None);

        result.AlreadyPaid.Should().Be(1);
        result.Matched.Should().Be(0);
        _orderRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ══════════════════════════════════════
    // 4. Batch bulunamadı
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_BatchNotFound_ShouldReturnEmpty()
    {
        _settlementRepoMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SettlementBatch?)null);

        var sut = CreateSut();

        var result = await sut.HandleAsync(Guid.NewGuid(), TestTenantId, CancellationToken.None);

        result.TotalLines.Should().Be(0);
        result.Matched.Should().Be(0);
    }

    // ══════════════════════════════════════
    // 5. Boş batch
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_EmptyBatch_ShouldReturnZeros()
    {
        var batch = SettlementBatch.Create(TestTenantId, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow, 0m, 0m, 0m);

        _settlementRepoMock
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var sut = CreateSut();

        var result = await sut.HandleAsync(batch.Id, TestTenantId, CancellationToken.None);

        result.TotalLines.Should().Be(0);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ══════════════════════════════════════
    // 6. Çoklu satır — karışık sonuçlar
    // ══════════════════════════════════════

    [Fact]
    public async Task HandleAsync_MixedResults_ShouldReportCorrectly()
    {
        var batch = CreateBatchWithLines(
            ("ORD-MATCH", 1000m, 150m, 30m, 810m),
            ("ORD-LOST", 500m, 75m, 0m, 425m),
            ("ORD-PREPAID", 200m, 30m, 0m, 170m));

        _settlementRepoMock
            .Setup(r => r.GetByIdAsync(batch.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var matchOrder = CreateTestOrder(TestTenantId,"ORD-MATCH");
        var paidOrder = CreateTestOrder(TestTenantId,"ORD-PREPAID");
        paidOrder.MarkAsPaid();

        _orderRepoMock
            .Setup(r => r.GetByOrderNumberAsync("ORD-MATCH", It.IsAny<CancellationToken>()))
            .ReturnsAsync(matchOrder);
        _orderRepoMock
            .Setup(r => r.GetByOrderNumberAsync("ORD-LOST", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);
        _orderRepoMock
            .Setup(r => r.GetByOrderNumberAsync("ORD-PREPAID", It.IsAny<CancellationToken>()))
            .ReturnsAsync(paidOrder);

        _uowMock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = CreateSut();

        var result = await sut.HandleAsync(batch.Id, TestTenantId, CancellationToken.None);

        result.TotalLines.Should().Be(3);
        result.Matched.Should().Be(1);
        result.NotFound.Should().Be(1);
        result.AlreadyPaid.Should().Be(1);
    }
}
