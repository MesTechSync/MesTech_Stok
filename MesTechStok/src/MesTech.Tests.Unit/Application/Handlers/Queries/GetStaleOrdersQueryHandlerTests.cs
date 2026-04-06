using FluentAssertions;
using MesTech.Application.Features.Orders.Queries.GetStaleOrders;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Queries;

/// <summary>
/// DEV5: GetStaleOrdersQueryHandler testi — gecikmiş sipariş sorgusu.
/// P1: Gecikmiş sipariş tespiti = müşteri memnuniyeti.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class GetStaleOrdersQueryHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<ILogger<GetStaleOrdersQueryHandler>> _logger = new();

    private GetStaleOrdersQueryHandler CreateSut() => new(_orderRepo.Object, _logger.Object);

    [Fact]
    public async Task Handle_NoStaleOrders_ShouldReturnEmpty()
    {
        _orderRepo.Setup(r => r.GetStaleOrdersAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var query = new GetStaleOrdersQuery(Guid.NewGuid());
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Handle_DefaultThreshold_ShouldUse48Hours()
    {
        var query = new GetStaleOrdersQuery(Guid.NewGuid());
        query.EffectiveThreshold.Should().Be(TimeSpan.FromHours(48));
    }

    [Fact]
    public void Handle_CustomThreshold_ShouldBeRespected()
    {
        var query = new GetStaleOrdersQuery(Guid.NewGuid(), TimeSpan.FromHours(24));
        query.EffectiveThreshold.Should().Be(TimeSpan.FromHours(24));
    }

    [Fact]
    public async Task Handle_WithStaleOrders_ShouldReturnMappedDtos()
    {
        var tenantId = Guid.NewGuid();
        var order = new Order
        {
            TenantId = tenantId,
            OrderNumber = "ORD-STALE",
            SourcePlatform = PlatformType.Trendyol,
            CustomerName = "Test Müşteri"
        };

        _orderRepo.Setup(r => r.GetStaleOrdersAsync(tenantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order });

        var query = new GetStaleOrdersQuery(tenantId);
        var result = await CreateSut().Handle(query, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].OrderNumber.Should().Be("ORD-STALE");
    }
}
