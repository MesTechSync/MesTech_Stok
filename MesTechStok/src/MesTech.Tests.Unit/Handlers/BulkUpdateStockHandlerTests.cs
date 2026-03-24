using FluentAssertions;
using MesTech.Application.Commands.BulkUpdateStock;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class BulkUpdateStockHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly BulkUpdateStockHandler _sut;

    public BulkUpdateStockHandlerTests()
    {
        _productRepo = new Mock<IProductRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new BulkUpdateStockHandler(_productRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_AllValid_ReturnsFullSuccess()
    {
        // Arrange
        var product = new Product { SKU = "SKU-001", Stock = 10 };
        _productRepo.Setup(r => r.GetBySKUAsync("SKU-001")).ReturnsAsync(product);

        var command = new BulkUpdateStockCommand(
            new[] { new BulkUpdateStockItem("SKU-001", 20) });

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_NegativeStock_ReturnsFailure()
    {
        var command = new BulkUpdateStockCommand(
            new[] { new BulkUpdateStockItem("SKU-BAD", -5) });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(0);
        result.FailedCount.Should().Be(1);
        result.Failures[0].Reason.Should().Contain("negative");
    }

    [Fact]
    public async Task Handle_SkuNotFound_ReturnsFailure()
    {
        _productRepo.Setup(r => r.GetBySKUAsync("MISSING")).ReturnsAsync((Product?)null);

        var command = new BulkUpdateStockCommand(
            new[] { new BulkUpdateStockItem("MISSING", 10) });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.FailedCount.Should().Be(1);
        result.Failures[0].Reason.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_MixedItems_ReportsPartialSuccess()
    {
        var product = new Product { SKU = "OK-SKU", Stock = 5 };
        _productRepo.Setup(r => r.GetBySKUAsync("OK-SKU")).ReturnsAsync(product);
        _productRepo.Setup(r => r.GetBySKUAsync("BAD-SKU")).ReturnsAsync((Product?)null);

        var command = new BulkUpdateStockCommand(new[]
        {
            new BulkUpdateStockItem("OK-SKU", 20),
            new BulkUpdateStockItem("BAD-SKU", 10),
            new BulkUpdateStockItem("NEG-SKU", -1)
        });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_NoSuccess_DoesNotCallSave()
    {
        var command = new BulkUpdateStockCommand(
            new[] { new BulkUpdateStockItem("X", -1) });

        await _sut.Handle(command, CancellationToken.None);

        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never());
    }
}
