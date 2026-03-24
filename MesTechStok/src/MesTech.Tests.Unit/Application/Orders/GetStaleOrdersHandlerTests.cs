using FluentAssertions;
using MesTech.Application.Features.Orders.Queries.GetStaleOrders;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Orders;

/// <summary>
/// GetStaleOrdersQueryHandler testleri.
/// Happy path, fresh orders filtered, empty result, default threshold.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Orders")]
public class GetStaleOrdersHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<ILogger<GetStaleOrdersQueryHandler>> _loggerMock = new();
    private readonly GetStaleOrdersQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();

    public GetStaleOrdersHandlerTests()
    {
        _handler = new GetStaleOrdersQueryHandler(_orderRepoMock.Object, _loggerMock.Object);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Happy Path
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Handle — stale orders returned with correct mapping")]
    public async Task Handle_StaleOrdersExist_ReturnsMappedDtos()
    {
        // Arrange
        var staleOrder = CreateOrder("ORD-001", DateTime.UtcNow.AddHours(-72), PlatformType.Trendyol, "Ali Veli");
        _orderRepoMock
            .Setup(r => r.GetStaleOrdersAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { staleOrder });

        var query = new GetStaleOrdersQuery(_tenantId, TimeSpan.FromHours(48));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].OrderId.Should().Be(staleOrder.Id);
        result[0].OrderNumber.Should().Be("ORD-001");
        result[0].Platform.Should().Be(PlatformType.Trendyol);
        result[0].CustomerName.Should().Be("Ali Veli");
        result[0].Elapsed.Should().BeGreaterThan(TimeSpan.FromHours(71));
    }

    [Fact(DisplayName = "Handle — multiple stale orders all returned")]
    public async Task Handle_MultipleStaleOrders_AllReturned()
    {
        // Arrange
        var orders = new List<Order>
        {
            CreateOrder("ORD-001", DateTime.UtcNow.AddHours(-96), PlatformType.Trendyol),
            CreateOrder("ORD-002", DateTime.UtcNow.AddHours(-72), PlatformType.Hepsiburada),
            CreateOrder("ORD-003", DateTime.UtcNow.AddHours(-50), PlatformType.N11)
        };
        _orderRepoMock
            .Setup(r => r.GetStaleOrdersAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var query = new GetStaleOrdersQuery(_tenantId, TimeSpan.FromHours(48));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Select(r => r.OrderNumber).Should().BeEquivalentTo("ORD-001", "ORD-002", "ORD-003");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Empty Result
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Handle — no stale orders returns empty list")]
    public async Task Handle_NoStaleOrders_ReturnsEmptyList()
    {
        // Arrange
        _orderRepoMock
            .Setup(r => r.GetStaleOrdersAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var query = new GetStaleOrdersQuery(_tenantId, TimeSpan.FromHours(48));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Default Threshold
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "GetStaleOrdersQuery — default threshold is 48 hours")]
    public void Query_DefaultThreshold_Is48Hours()
    {
        var query = new GetStaleOrdersQuery(Guid.NewGuid());

        query.EffectiveThreshold.Should().Be(TimeSpan.FromHours(48));
    }

    [Fact(DisplayName = "GetStaleOrdersQuery — custom threshold is respected")]
    public void Query_CustomThreshold_IsRespected()
    {
        var query = new GetStaleOrdersQuery(Guid.NewGuid(), TimeSpan.FromHours(24));

        query.EffectiveThreshold.Should().Be(TimeSpan.FromHours(24));
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Repository Cutoff Date
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Handle — passes correct cutoff date to repository")]
    public async Task Handle_PassesCorrectCutoffDateToRepository()
    {
        // Arrange
        var threshold = TimeSpan.FromHours(24);
        _orderRepoMock
            .Setup(r => r.GetStaleOrdersAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var query = new GetStaleOrdersQuery(_tenantId, threshold);
        var beforeCall = DateTime.UtcNow;

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _orderRepoMock.Verify(r => r.GetStaleOrdersAsync(
            _tenantId,
            It.Is<DateTime>(d => d <= beforeCall.AddHours(-24).AddSeconds(5)
                               && d >= beforeCall.AddHours(-24).AddSeconds(-5)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Logging
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Handle — logs warning when stale orders found")]
    public async Task Handle_StaleOrdersFound_LogsWarning()
    {
        // Arrange
        var staleOrder = CreateOrder("ORD-001", DateTime.UtcNow.AddHours(-72), PlatformType.Trendyol);
        _orderRepoMock
            .Setup(r => r.GetStaleOrdersAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { staleOrder });

        var query = new GetStaleOrdersQuery(_tenantId, TimeSpan.FromHours(48));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert — verify LogWarning was called (via the generic Log method)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(DisplayName = "Handle — does not log when no stale orders")]
    public async Task Handle_NoStaleOrders_DoesNotLog()
    {
        // Arrange
        _orderRepoMock
            .Setup(r => r.GetStaleOrdersAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var query = new GetStaleOrdersQuery(_tenantId, TimeSpan.FromHours(48));

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CancellationToken
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Handle — passes cancellation token to repository")]
    public async Task Handle_PassesCancellationTokenToRepository()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        _orderRepoMock
            .Setup(r => r.GetStaleOrdersAsync(_tenantId, It.IsAny<DateTime>(), cts.Token))
            .ReturnsAsync(new List<Order>());

        var query = new GetStaleOrdersQuery(_tenantId);

        // Act
        await _handler.Handle(query, cts.Token);

        // Assert
        _orderRepoMock.Verify(r => r.GetStaleOrdersAsync(
            _tenantId, It.IsAny<DateTime>(), cts.Token), Times.Once);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Elapsed Calculation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(DisplayName = "Handle — Elapsed is calculated from OrderDate to now")]
    public async Task Handle_Elapsed_CalculatedFromOrderDateToNow()
    {
        // Arrange
        var orderDate = DateTime.UtcNow.AddHours(-100);
        var order = CreateOrder("ORD-001", orderDate, PlatformType.Trendyol);
        _orderRepoMock
            .Setup(r => r.GetStaleOrdersAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order });

        var query = new GetStaleOrdersQuery(_tenantId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result[0].Elapsed.TotalHours.Should().BeApproximately(100, 1);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Helper
    // ══════════════════════════════════════════════════════════════════════════

    private Order CreateOrder(
        string orderNumber,
        DateTime orderDate,
        PlatformType? platform = null,
        string? customerName = null)
    {
        return new Order
        {
            OrderNumber = orderNumber,
            OrderDate = orderDate,
            SourcePlatform = platform,
            CustomerName = customerName
        };
    }
}
