using FluentAssertions;
using MesTech.Application.Features.Dashboard.Queries.GetTopProducts;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application.Products;

[Trait("Category", "Unit")]
[Trait("Domain", "Products")]
public class GetTopProductsHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();

    private GetTopProductsHandler CreateSut() => new(_orderRepo.Object);

    [Fact]
    public async Task Handle_ValidRequest_ReturnsTopProductsByRevenue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var productIdA = Guid.NewGuid();
        var productIdB = Guid.NewGuid();

        var order = FakeData.CreateOrder();
        var itemA = new OrderItem
        {
            ProductId = productIdA,
            ProductSKU = "SKU-A",
            ProductName = "Product A"
        };
        itemA.SetQuantityAndPrice(2, 100m);
        order.AddItem(itemA);

        var itemB = new OrderItem
        {
            ProductId = productIdB,
            ProductSKU = "SKU-B",
            ProductName = "Product B"
        };
        itemB.SetQuantityAndPrice(5, 50m);
        order.AddItem(itemB);

        _orderRepo
            .Setup(r => r.GetByDateRangeWithItemsAsync(
                tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order }.AsReadOnly());

        var query = new GetTopProductsQuery(tenantId, Limit: 10);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        // Product B: 5*50=250 revenue > Product A: 2*100=200 revenue
        result[0].Revenue.Should().Be(250m);
        result[0].SKU.Should().Be("SKU-B");
        result[1].Revenue.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_NoOrders_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _orderRepo
            .Setup(r => r.GetByDateRangeWithItemsAsync(
                tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>().AsReadOnly());

        var query = new GetTopProductsQuery(tenantId);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LimitClampedTo1_ReturnsOnlyTopProduct()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var order = FakeData.CreateOrder();
        var item1 = new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductSKU = "TOP-1",
            ProductName = "Top Product"
        };
        item1.SetQuantityAndPrice(10, 500m);
        order.AddItem(item1);

        var item2 = new OrderItem
        {
            ProductId = Guid.NewGuid(),
            ProductSKU = "LOW-1",
            ProductName = "Low Product"
        };
        item2.SetQuantityAndPrice(1, 10m);
        order.AddItem(item2);

        _orderRepo
            .Setup(r => r.GetByDateRangeWithItemsAsync(
                tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order }.AsReadOnly());

        var query = new GetTopProductsQuery(tenantId, Limit: 1);
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].SKU.Should().Be("TOP-1");
    }
}
