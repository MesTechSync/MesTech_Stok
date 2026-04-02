using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using MesTech.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Invoice.Commands;

[Trait("Category", "Unit")]
public class BulkCreateInvoiceHandlerTests
{
    private readonly Mock<IInvoiceRepository> _repository = new();
    private readonly Mock<IOrderRepository> _orderRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<BulkCreateInvoiceHandler>> _logger = new();

    private BulkCreateInvoiceHandler CreateHandler() =>
        new BulkCreateInvoiceHandler(_repository.Object, _orderRepository.Object, _unitOfWork.Object, _logger.Object);

    [Fact]
    public async Task Handle_MultipleOrderIds_ShouldReturnSuccessForAll()
    {
        // Arrange
        var orderIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var orders = orderIds.Select(id => CreateFakeOrder(id)).ToList();

        _orderRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Order>)orders);

        var command = new BulkCreateInvoiceCommand(orderIds, InvoiceProvider.Sovos);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalRequested.Should().Be(3);
        result.SuccessCount.Should().Be(3);
        result.FailCount.Should().Be(0);
        result.Results.Should().HaveCount(3);
        result.Results.Should().OnlyContain(r => r.Success);
    }

    [Fact]
    public async Task Handle_EmptyOrderIds_ShouldReturnZeroCounts()
    {
        // Arrange
        _orderRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var command = new BulkCreateInvoiceCommand(new List<Guid>(), InvoiceProvider.Parasut);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.TotalRequested.Should().Be(0);
        result.SuccessCount.Should().Be(0);
        result.FailCount.Should().Be(0);
        result.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SingleOrder_ShouldGenerateInvoiceNumber()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var order = CreateFakeOrder(orderId);

        _orderRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order });

        var command = new BulkCreateInvoiceCommand(new List<Guid> { orderId }, InvoiceProvider.Sovos);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Results.Should().ContainSingle();
        var item = result.Results[0];
        item.OrderId.Should().Be(orderId);
        item.InvoiceNumber.Should().StartWith("INV-");
        item.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var act = () => handler.Handle(null!, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private static Order CreateFakeOrder(Guid orderId)
    {
        return new Order
        {
            Id = orderId,
            OrderNumber = $"ORD-{orderId.ToString("N")[..8]}",
            TenantId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            CustomerName = "Test Customer",
            OrderDate = DateTime.UtcNow
        };
    }
}
