using FluentAssertions;
using MesTech.Application.Features.Product.Commands.AutoCompetePrice;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class AutoCompetePriceHandlerTests
{
    private readonly Mock<IBuyboxService> _buyboxMock = new();
    private readonly Mock<IAdapterFactory> _adapterFactoryMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ILogger<AutoCompetePriceHandler>> _loggerMock = new();

    private AutoCompetePriceHandler CreateHandler() =>
        new(_buyboxMock.Object, _adapterFactoryMock.Object, _productRepoMock.Object, _loggerMock.Object);

    private static AutoCompetePriceCommand CreateCommand(
        Guid? productId = null, decimal floorPrice = 50m, decimal maxDiscount = 5m) =>
        new(Guid.NewGuid(), productId ?? Guid.NewGuid(), "trendyol", floorPrice, maxDiscount);

    [Fact]
    public async Task Handle_ProductNotFound_ShouldReturnFailure()
    {
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Product?)null);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamadı");
    }

    [Fact]
    public async Task Handle_AlreadyHasBuybox_ShouldReturnNoChange()
    {
        var product = new Product { SalePrice = 100m, SKU = "TST-001" };
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);
        _buyboxMock.Setup(b => b.AnalyzeCompetitorsAsync(
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BuyboxAnalysis(true, 100m, 0m, "", Array.Empty<CompetitorPriceInfo>(), 100m, ""));

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.PriceChanged.Should().BeFalse();
        result.Reasoning.Should().Contain("Buybox");
    }

    [Fact]
    public async Task Handle_NoCompetitors_ShouldReturnNoChange()
    {
        var product = new Product { SalePrice = 100m, SKU = "TST-001" };
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);
        _buyboxMock.Setup(b => b.AnalyzeCompetitorsAsync(
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BuyboxAnalysis(false, 100m, 0m, "", Array.Empty<CompetitorPriceInfo>(), 100m, ""));

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.PriceChanged.Should().BeFalse();
        result.Reasoning.Should().Contain("Rakip bulunamadı");
    }

    [Fact]
    public async Task Handle_TargetBelowFloorPrice_ShouldClampToFloor()
    {
        var product = new Product { SalePrice = 100m, SKU = "TST-001" };
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);

        var competitors = new[] { new CompetitorPriceInfo("Rival", 40m, 0m, 5, false, DateTime.UtcNow) };
        _buyboxMock.Setup(b => b.AnalyzeCompetitorsAsync(
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BuyboxAnalysis(false, 100m, 40m, "Rival", competitors, 39.6m, ""));

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.PushPriceUpdateAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _adapterFactoryMock.Setup(f => f.Resolve(It.IsAny<string>())).Returns(adapterMock.Object);

        // FloorPrice=95 means target (39.6) clamped to 95, but 95 < 100 so it changes
        var result = await CreateHandler().Handle(CreateCommand(floorPrice: 95m, maxDiscount: 50m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.PriceChanged.Should().BeTrue();
        result.NewPrice.Should().Be(95m);
    }

    [Fact]
    public async Task Handle_AdapterNotFound_ShouldReturnFailure()
    {
        var product = new Product { SalePrice = 100m, SKU = "TST-001" };
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);

        var competitors = new[] { new CompetitorPriceInfo("Rival", 90m, 0m, 5, false, DateTime.UtcNow) };
        _buyboxMock.Setup(b => b.AnalyzeCompetitorsAsync(
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BuyboxAnalysis(false, 100m, 90m, "Rival", competitors, 89.1m, ""));

        _adapterFactoryMock.Setup(f => f.Resolve(It.IsAny<string>())).Returns((IIntegratorAdapter?)null);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("adapter bulunamadı");
    }

    [Fact]
    public async Task Handle_PushFails_ShouldReturnFailure()
    {
        var product = new Product { SalePrice = 100m, SKU = "TST-001" };
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);

        var competitors = new[] { new CompetitorPriceInfo("Rival", 90m, 0m, 5, false, DateTime.UtcNow) };
        _buyboxMock.Setup(b => b.AnalyzeCompetitorsAsync(
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BuyboxAnalysis(false, 100m, 90m, "Rival", competitors, 89.1m, ""));

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.PushPriceUpdateAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _adapterFactoryMock.Setup(f => f.Resolve(It.IsAny<string>())).Returns(adapterMock.Object);

        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("adapter hatası");
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldReturnChangedResult()
    {
        var product = new Product { SalePrice = 100m, SKU = "TST-001" };
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);

        var competitors = new[] { new CompetitorPriceInfo("Rival", 90m, 0m, 5, false, DateTime.UtcNow) };
        _buyboxMock.Setup(b => b.AnalyzeCompetitorsAsync(
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BuyboxAnalysis(false, 100m, 90m, "Rival", competitors, 89.1m, ""));

        var adapterMock = new Mock<IIntegratorAdapter>();
        adapterMock.Setup(a => a.PushPriceUpdateAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _adapterFactoryMock.Setup(f => f.Resolve(It.IsAny<string>())).Returns(adapterMock.Object);

        var result = await CreateHandler().Handle(CreateCommand(floorPrice: 50m, maxDiscount: 30m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.PriceChanged.Should().BeTrue();
        result.OldPrice.Should().Be(100m);
        result.NewPrice.Should().Be(89.1m); // 90 * 0.99, maxDiscount=30% allows this
        result.CompetitorName.Should().Be("Rival");
    }

    [Fact]
    public async Task Handle_TargetAboveCurrentPrice_ShouldReturnNoChange()
    {
        var product = new Product { SalePrice = 80m, SKU = "TST-001" };
        _productRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(product);

        var competitors = new[] { new CompetitorPriceInfo("Rival", 100m, 0m, 5, false, DateTime.UtcNow) };
        _buyboxMock.Setup(b => b.AnalyzeCompetitorsAsync(
                It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BuyboxAnalysis(false, 80m, 100m, "Rival", competitors, 99m, ""));

        var result = await CreateHandler().Handle(CreateCommand(floorPrice: 50m), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.PriceChanged.Should().BeFalse();
        result.Reasoning.Should().Contain("yüksek veya eşit");
    }
}
