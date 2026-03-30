using FluentAssertions;
using MesTech.Application.Commands.CreateOrder;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Stock;

/// <summary>
/// CreateOrderHandler: manual sipariş oluşturma (stok etkisi yok).
/// OrderNumber formatı: ORD-yyyyMMdd-{8char}
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Handler")]
[Trait("Group", "Order")]
public class CreateOrderHandlerTests
{
    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public CreateOrderHandlerTests()
    {
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private CreateOrderHandler CreateHandler() =>
        new(_orderRepo.Object, _uow.Object);

    [Fact]
    public async Task Handle_ValidCommand_CreatesOrderAndReturnsSuccess()
    {
        var cmd = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerName: "Test Müşteri",
            CustomerEmail: "test@example.com",
            OrderType: "SALE",
            Notes: "Test sipariş");

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.OrderId.Should().NotBe(Guid.Empty);
        result.OrderNumber.Should().StartWith("ORD-");
        _orderRepo.Verify(r => r.AddAsync(It.Is<Order>(o =>
            o.CustomerName == "Test Müşteri" &&
            o.Type == "SALE")), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_WithRequiredDate_SetsRequiredDate()
    {
        var requiredDate = DateTime.UtcNow.AddDays(7);
        var cmd = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerName: "Acil Müşteri",
            CustomerEmail: null,
            OrderType: "URGENT",
            Notes: null,
            RequiredDate: requiredDate);

        Order? capturedOrder = null;
        _orderRepo.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .Callback<Order>(o => capturedOrder = o)
            .Returns(Task.CompletedTask);

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.RequiredDate.Should().Be(requiredDate);
    }

    [Fact]
    public async Task Handle_OrderNumber_ContainsDate()
    {
        var cmd = new CreateOrderCommand(
            CustomerId: Guid.NewGuid(),
            CustomerName: "Tarih Test",
            CustomerEmail: null,
            OrderType: "SALE",
            Notes: null);

        var handler = CreateHandler();
        var result = await handler.Handle(cmd, CancellationToken.None);

        result.OrderNumber.Should().Contain(DateTime.UtcNow.ToString("yyyyMMdd"));
    }
}
