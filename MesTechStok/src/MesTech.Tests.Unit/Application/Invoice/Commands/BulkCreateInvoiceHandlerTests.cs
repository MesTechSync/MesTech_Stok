using FluentAssertions;
using MesTech.Application.Features.Invoice.Commands;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Invoice.Commands;

[Trait("Category", "Unit")]
public class BulkCreateInvoiceHandlerTests
{
    private readonly Mock<IInvoiceRepository> _repository = new();
    private readonly Mock<ILogger<BulkCreateInvoiceHandler>> _logger = new();

    private BulkCreateInvoiceHandler CreateHandler() =>
        new BulkCreateInvoiceHandler(_repository.Object, _logger.Object);

    [Fact]
    public async Task Handle_MultipleOrderIds_ShouldReturnSuccessForAll()
    {
        // Arrange
        var orderIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
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
}
