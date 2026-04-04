using FluentAssertions;
using MesTech.Application.Features.Product.Commands.AutoCompetePrice;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class BulkAutoCompetePriceHandlerTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ISender> _mediatorMock = new();
    private readonly Mock<ILogger<BulkAutoCompetePriceHandler>> _loggerMock = new();

    private BulkAutoCompetePriceHandler CreateHandler() =>
        new(_productRepoMock.Object, _mediatorMock.Object, _loggerMock.Object);

    [Fact]
    public async Task Handle_NoActiveProducts_ShouldReturnZeroCounts()
    {
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Product>());

        var command = new BulkAutoCompetePriceCommand(Guid.NewGuid(), "trendyol");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.TotalProcessed.Should().Be(0);
        result.PriceChanged.Should().Be(0);
        result.Skipped.Should().Be(0);
        result.Failed.Should().Be(0);
    }

    [Fact]
    public async Task Handle_InactiveProduct_ShouldBeSkipped()
    {
        var products = new[]
        {
            new Product { IsActive = false, SalePrice = 100m }
        };
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var command = new BulkAutoCompetePriceCommand(Guid.NewGuid(), "trendyol");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.TotalProcessed.Should().Be(0);
        _mediatorMock.Verify(m => m.Send(It.IsAny<AutoCompetePriceCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ActiveProduct_PriceChanged_ShouldCountAsChanged()
    {
        var products = new[]
        {
            new Product { IsActive = true, SalePrice = 100m, PurchasePrice = 60m }
        };
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mediatorMock.Setup(m => m.Send(It.IsAny<AutoCompetePriceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AutoCompetePriceResult.Changed(100m, 89m, 90m, "Rival", "Changed"));

        var command = new BulkAutoCompetePriceCommand(Guid.NewGuid(), "trendyol");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.TotalProcessed.Should().Be(1);
        result.PriceChanged.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ActiveProduct_NoChange_ShouldCountAsSkipped()
    {
        var products = new[]
        {
            new Product { IsActive = true, SalePrice = 100m, PurchasePrice = 60m }
        };
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mediatorMock.Setup(m => m.Send(It.IsAny<AutoCompetePriceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AutoCompetePriceResult.NoChange(100m, "No change"));

        var command = new BulkAutoCompetePriceCommand(Guid.NewGuid(), "trendyol");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MediatorThrows_ShouldCountAsFailed()
    {
        var products = new[]
        {
            new Product { IsActive = true, SalePrice = 100m, PurchasePrice = 60m }
        };
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mediatorMock.Setup(m => m.Send(It.IsAny<AutoCompetePriceCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Adapter error"));

        var command = new BulkAutoCompetePriceCommand(Guid.NewGuid(), "trendyol");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.Failed.Should().Be(1);
        result.Details.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_NoPlatformSpecified_ShouldUseAllPlatforms()
    {
        var products = new[]
        {
            new Product { IsActive = true, SalePrice = 100m, PurchasePrice = 60m }
        };
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mediatorMock.Setup(m => m.Send(It.IsAny<AutoCompetePriceCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AutoCompetePriceResult.NoChange(100m, "Ok"));

        var command = new BulkAutoCompetePriceCommand(Guid.NewGuid(), null);
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        // 5 platforms: trendyol, hepsiburada, n11, ciceksepati, amazon
        result.TotalProcessed.Should().Be(5);
        _mediatorMock.Verify(m => m.Send(It.IsAny<AutoCompetePriceCommand>(), It.IsAny<CancellationToken>()), Times.Exactly(5));
    }

    [Fact]
    public async Task Handle_ZeroPriceProduct_ShouldBeSkipped()
    {
        var products = new[]
        {
            new Product { IsActive = true, SalePrice = 0m }
        };
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var command = new BulkAutoCompetePriceCommand(Guid.NewGuid(), "trendyol");
        var result = await CreateHandler().Handle(command, CancellationToken.None);

        result.TotalProcessed.Should().Be(0);
    }
}
