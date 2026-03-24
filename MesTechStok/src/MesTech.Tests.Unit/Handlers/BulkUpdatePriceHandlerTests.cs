using FluentAssertions;
using MesTech.Application.Commands.BulkUpdatePrice;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
public class BulkUpdatePriceHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo;
    private readonly Mock<IUnitOfWork> _uow;
    private readonly BulkUpdatePriceHandler _sut;

    public BulkUpdatePriceHandlerTests()
    {
        _productRepo = new Mock<IProductRepository>();
        _uow = new Mock<IUnitOfWork>();
        _sut = new BulkUpdatePriceHandler(_productRepo.Object, _uow.Object);
    }

    [Fact]
    public async Task Handle_ValidPrice_UpdatesAndSaves()
    {
        var product = new Product { SKU = "SKU-P1", SalePrice = 100m };
        _productRepo.Setup(r => r.GetBySKUAsync("SKU-P1")).ReturnsAsync(product);

        var command = new BulkUpdatePriceCommand(
            new[] { new BulkUpdatePriceItem("SKU-P1", 150m) });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.SuccessCount.Should().Be(1);
        result.FailedCount.Should().Be(0);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task Handle_ZeroPrice_ReturnsFailure()
    {
        var command = new BulkUpdatePriceCommand(
            new[] { new BulkUpdatePriceItem("SKU-Z", 0m) });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.FailedCount.Should().Be(1);
        result.Failures[0].Reason.Should().Contain("greater than 0");
    }

    [Fact]
    public async Task Handle_NegativePrice_ReturnsFailure()
    {
        var command = new BulkUpdatePriceCommand(
            new[] { new BulkUpdatePriceItem("SKU-N", -10m) });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.FailedCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_SkuNotFound_ReturnsFailure()
    {
        _productRepo.Setup(r => r.GetBySKUAsync("GHOST")).ReturnsAsync((Product?)null);

        var command = new BulkUpdatePriceCommand(
            new[] { new BulkUpdatePriceItem("GHOST", 99m) });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.FailedCount.Should().Be(1);
        result.Failures[0].Reason.Should().Contain("not found");
    }
}
