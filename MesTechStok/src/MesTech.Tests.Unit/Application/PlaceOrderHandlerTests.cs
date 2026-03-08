using FluentAssertions;
using MesTech.Application.Commands.PlaceOrder;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Exceptions;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Services;
using MesTech.Tests.Unit._Shared;
using Moq;

namespace MesTech.Tests.Unit.Application;

[Trait("Category", "Unit")]
public class PlaceOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly StockCalculationService _stockCalc = new();

    private PlaceOrderHandler CreateHandler() =>
        new(_orderRepo.Object, _productRepo.Object, _unitOfWork.Object, _stockCalc);

    [Fact]
    public async Task Handle_ValidSingleItem_ShouldCreateOrder()
    {
        var product = FakeData.CreateProduct(sku: "ORD-001", stock: 100, salePrice: 50m);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Test Customer", "test@test.com", null,
            new List<PlaceOrderItem> { new(product.Id, 5, 50m) });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.OrderNumber.Should().StartWith("ORD-");
        result.OrderId.Should().NotBeEmpty();
        product.Stock.Should().Be(95);
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleItems_ShouldDeductAllStocks()
    {
        var p1 = FakeData.CreateProduct(sku: "MULTI-01", stock: 50, salePrice: 30m);
        var p2 = FakeData.CreateProduct(sku: "MULTI-02", stock: 80, salePrice: 20m);
        _productRepo.Setup(r => r.GetByIdAsync(p1.Id)).ReturnsAsync(p1);
        _productRepo.Setup(r => r.GetByIdAsync(p2.Id)).ReturnsAsync(p2);

        var handler = CreateHandler();
        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Multi Buyer", null, "Two items",
            new List<PlaceOrderItem>
            {
                new(p1.Id, 10, 30m),
                new(p2.Id, 20, 20m)
            });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        p1.Stock.Should().Be(40);
        p2.Stock.Should().Be(60);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnError()
    {
        var missingId = Guid.NewGuid();
        _productRepo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((Product?)null);

        var handler = CreateHandler();
        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Buyer", null, null,
            new List<PlaceOrderItem> { new(missingId, 1, 10m) });

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain(missingId.ToString());
        _orderRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ShouldThrow()
    {
        var product = FakeData.CreateProduct(sku: "LOW-001", stock: 3);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        var handler = CreateHandler();
        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Buyer", null, null,
            new List<PlaceOrderItem> { new(product.Id, 100, 50m) });

        var act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InsufficientStockException>();
    }

    [Fact]
    public async Task Handle_ShouldSetOrderStatusToConfirmed()
    {
        var product = FakeData.CreateProduct(sku: "STATUS-01", stock: 50);
        _productRepo.Setup(r => r.GetByIdAsync(product.Id)).ReturnsAsync(product);

        Order? capturedOrder = null;
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o);

        var handler = CreateHandler();
        var command = new PlaceOrderCommand(
            Guid.NewGuid(), "Buyer", "buyer@test.com", "Rush",
            new List<PlaceOrderItem> { new(product.Id, 2, 25m) });

        await handler.Handle(command, CancellationToken.None);

        capturedOrder.Should().NotBeNull();
        capturedOrder!.Status.Should().Be(OrderStatus.Confirmed);
        capturedOrder.CustomerName.Should().Be("Buyer");
        capturedOrder.CustomerEmail.Should().Be("buyer@test.com");
        capturedOrder.Notes.Should().Be("Rush");
    }
}
