using System.IO;
using FluentAssertions;
using MesTech.Application.DTOs.Invoice;
using MesTech.Application.Features.Product.Commands.AutoCompetePrice;
using MesTech.Application.Features.Product.Commands.SaveProductVariants;
using MesTech.Application.Features.Product.Commands.ValidateBulkImport;
using MesTech.Application.Features.Product.Queries.FetchProductFromPlatform;
using MesTech.Application.Features.Product.Queries.GetBuyboxStatus;
using MesTech.Application.Features.Product.Queries.GetPlatformProducts;
using MesTech.Application.Features.Product.Queries.GetProducts;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

// ═══════════════════════════════════════════════════════════════
// DEV 5 — Product Handler Batch Tests (8 handlers)
// ═══════════════════════════════════════════════════════════════

#region AutoCompetePriceHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class AutoCompetePriceHandlerBatchTests
{
    private readonly Mock<IBuyboxService> _buyboxMock = new();
    private readonly Mock<IAdapterFactory> _adapterFactoryMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly AutoCompetePriceHandler _sut;

    public AutoCompetePriceHandlerBatchTests()
    {
        _sut = new AutoCompetePriceHandler(
            _buyboxMock.Object,
            _adapterFactoryMock.Object,
            _productRepoMock.Object,
            Mock.Of<ILogger<AutoCompetePriceHandler>>());
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var cmd = new AutoCompetePriceCommand(Guid.NewGuid(), productId, "trendyol", 10m);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamad");
        _productRepoMock.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region BulkAutoCompetePriceHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class BulkAutoCompetePriceHandlerBatchTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<ISender> _mediatorMock = new();
    private readonly BulkAutoCompetePriceHandler _sut;

    public BulkAutoCompetePriceHandlerBatchTests()
    {
        _sut = new BulkAutoCompetePriceHandler(
            _productRepoMock.Object,
            _mediatorMock.Object,
            Mock.Of<ILogger<BulkAutoCompetePriceHandler>>());
    }

    [Fact]
    public async Task Handle_NoActiveProducts_ReturnsZeroProcessed()
    {
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var cmd = new BulkAutoCompetePriceCommand(Guid.NewGuid(), "trendyol");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.TotalProcessed.Should().Be(0);
        result.PriceChanged.Should().Be(0);
        _productRepoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region SaveProductVariantsHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class SaveProductVariantsHandlerBatchTests
{
    private readonly Mock<IProductVariantRepository> _variantRepoMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly SaveProductVariantsHandler _sut;

    public SaveProductVariantsHandlerBatchTests()
    {
        _sut = new SaveProductVariantsHandler(
            _variantRepoMock.Object,
            _productRepoMock.Object,
            _uowMock.Object,
            Mock.Of<ILogger<SaveProductVariantsHandler>>());
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        var productId = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var cmd = new SaveProductVariantsCommand(Guid.NewGuid(), productId, new List<ProductVariantInput>());

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("bulunamadi");
        _productRepoMock.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region ValidateBulkImportHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class ValidateBulkImportHandlerBatchTests
{
    private readonly Mock<IBulkProductImportService> _importServiceMock = new();
    private readonly ValidateBulkImportHandler _sut;

    public ValidateBulkImportHandlerBatchTests()
    {
        _sut = new ValidateBulkImportHandler(_importServiceMock.Object);
    }

    [Fact]
    public async Task Handle_EmptyStream_ReturnsInvalid()
    {
        using var emptyStream = new MemoryStream();
        var cmd = new ValidateBulkImportCommand(emptyStream, "test.xlsx");

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}

#endregion

#region FetchProductFromPlatformHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class FetchProductFromPlatformHandlerBatchTests
{
    private readonly Mock<IProductScraperService> _scraperMock = new();
    private readonly FetchProductFromPlatformHandler _sut;

    public FetchProductFromPlatformHandlerBatchTests()
    {
        _sut = new FetchProductFromPlatformHandler(
            _scraperMock.Object,
            Mock.Of<ILogger<FetchProductFromPlatformHandler>>());
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsNull()
    {
        _scraperMock.Setup(s => s.ScrapeFromUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ScrapedProductDto?)null);

        var query = new FetchProductFromPlatformQuery("https://trendyol.com/p/12345");

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Should().BeNull();
        _scraperMock.Verify(s => s.ScrapeFromUrlAsync("https://trendyol.com/p/12345", It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetBuyboxStatusHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class GetBuyboxStatusHandlerBatchTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IBuyboxService> _buyboxMock = new();
    private readonly GetBuyboxStatusHandler _sut;

    public GetBuyboxStatusHandlerBatchTests()
    {
        _sut = new GetBuyboxStatusHandler(_productRepoMock.Object, _buyboxMock.Object);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsRecommendation()
    {
        var productId = Guid.NewGuid();
        _productRepoMock.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var query = new GetBuyboxStatusQuery(Guid.NewGuid(), productId);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ProductId.Should().Be(productId);
        result.Recommendation.Should().Contain("bulunamadi");
        _productRepoMock.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region GetPlatformProductsHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class GetPlatformProductsHandlerBatchTests
{
    private readonly Mock<IAdapterFactory> _adapterFactoryMock = new();
    private readonly GetPlatformProductsHandler _sut;

    public GetPlatformProductsHandlerBatchTests()
    {
        _sut = new GetPlatformProductsHandler(
            _adapterFactoryMock.Object,
            Mock.Of<ILogger<GetPlatformProductsHandler>>());
    }

    [Fact]
    public async Task Handle_NoAdapter_ReturnsEmptyResult()
    {
        _adapterFactoryMock.Setup(f => f.Resolve(It.IsAny<string>()))
            .Returns((IIntegratorAdapter?)null);

        var query = new GetPlatformProductsQuery("unknown_platform");

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _adapterFactoryMock.Verify(f => f.Resolve("unknown_platform"), Times.Once);
    }
}

#endregion

#region GetProductsHandler

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class GetProductsHandlerBatchTests
{
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly GetProductsHandler _sut;

    public GetProductsHandlerBatchTests()
    {
        _sut = new GetProductsHandler(_productRepoMock.Object);
    }

    [Fact]
    public async Task Handle_NoProducts_ReturnsEmptyPage()
    {
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        var query = new GetProductsQuery(Guid.NewGuid());

        var result = await _sut.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        _productRepoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion
