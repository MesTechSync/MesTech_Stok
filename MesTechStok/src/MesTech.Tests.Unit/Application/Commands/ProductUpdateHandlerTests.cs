using FluentAssertions;
using MesTech.Application.Commands.ApplyOptimizedPrice;
using MesTech.Application.Commands.UpdateProductContent;
using MesTech.Application.Commands.UpdateProductPrice;
using MesTech.Application.Queries.GetProductByBarcode;
using MesTech.Domain.Entities;
using MesTech.Domain.Entities.AI;
using MesTech.Domain.Interfaces;
using MesTech.Tests.Unit._Shared;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Commands;

// ════════════════════════════════════════════════════════════════
// D5-114: Product Management Handler Tests
// Covers: GetProductByBarcode, UpdateProductPrice,
//         UpdateProductContent, ApplyOptimizedPrice
// ════════════════════════════════════════════════════════════════

#region GetProductByBarcode

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class GetProductByBarcodeHandlerAdditionalTests
{
    private readonly Mock<IProductRepository> _productRepo = new();

    private GetProductByBarcodeHandler CreateHandler() => new(_productRepo.Object);

    [Fact]
    public async Task Handle_ExistingBarcode_ReturnsDto_WithCorrectStockStatus()
    {
        // Arrange — low stock product
        var product = FakeData.CreateProduct(
            sku: "BAR-LOW", barcode: "8690000111111",
            stock: 3, minimumStock: 10, purchasePrice: 50m, salePrice: 100m);
        _productRepo.Setup(r => r.GetByBarcodeAsync("8690000111111", It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await CreateHandler().Handle(
            new GetProductByBarcodeQuery("8690000111111"), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SKU.Should().Be("BAR-LOW");
        result.StockStatus.Should().Be("Low");
        result.NeedsReorder.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NonExistentBarcode_ReturnsNull()
    {
        // Arrange
        _productRepo.Setup(r => r.GetByBarcodeAsync("0000000000000", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await CreateHandler().Handle(
            new GetProductByBarcodeQuery("0000000000000"), CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var handler = CreateHandler();
        var act = () => handler.Handle(null!, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}

#endregion

#region UpdateProductPrice

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class UpdateProductPriceHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IPriceRecommendationRepository> _priceRecoRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<UpdateProductPriceHandler>> _logger = new();

    private UpdateProductPriceHandler CreateHandler() =>
        new(_productRepo.Object, _priceRecoRepo.Object, _unitOfWork.Object, _logger.Object);

    [Fact]
    public async Task Handle_ExistingProduct_UpdatesPriceAndSavesRecommendation()
    {
        // Arrange
        var product = FakeData.CreateProduct(sku: "PRC-001", salePrice: 100m);
        var productId = product.Id;
        var tenantId = Guid.NewGuid();

        _productRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductPriceCommand
        {
            ProductId = productId,
            SKU = "PRC-001",
            RecommendedPrice = 130m,
            MinPrice = 90m,
            MaxPrice = 200m,
            Reasoning = "AI recommendation",
            TenantId = tenantId
        };

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        product.SalePrice.Should().Be(130m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _priceRecoRepo.Verify(r => r.AddAsync(
            It.Is<PriceRecommendation>(p =>
                p.RecommendedPrice == 130m &&
                p.Source == "ai.price.recommended" &&
                p.ProductId == productId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_DoesNotSave()
    {
        // Arrange
        var command = new UpdateProductPriceCommand
        {
            ProductId = Guid.NewGuid(),
            SKU = "MISSING-SKU",
            RecommendedPrice = 50m,
            TenantId = Guid.NewGuid()
        };

        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepo.Setup(r => r.GetBySKUAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _priceRecoRepo.Verify(r => r.AddAsync(It.IsAny<PriceRecommendation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductFoundBySKU_WhenIdMisses_UpdatesPrice()
    {
        // Arrange — GetByIdAsync returns null, GetBySKUAsync returns product
        var product = FakeData.CreateProduct(sku: "FALLBACK-SKU", salePrice: 80m);
        var command = new UpdateProductPriceCommand
        {
            ProductId = Guid.NewGuid(), // wrong ID
            SKU = "FALLBACK-SKU",
            RecommendedPrice = 95m,
            TenantId = Guid.NewGuid()
        };

        _productRepo.Setup(r => r.GetByIdAsync(command.ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepo.Setup(r => r.GetBySKUAsync("FALLBACK-SKU", It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        product.SalePrice.Should().Be(95m);
    }
}

#endregion

#region UpdateProductContent

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class UpdateProductContentHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<UpdateProductContentHandler>> _logger = new();

    private UpdateProductContentHandler CreateHandler() =>
        new(_productRepo.Object, _unitOfWork.Object, _logger.Object);

    [Fact]
    public async Task Handle_ExistingProduct_UpdatesDescriptionAndMetadata()
    {
        // Arrange
        var product = FakeData.CreateProduct(sku: "CNT-001");
        product.Description = "Old description";
        var productId = product.Id;

        _productRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductContentCommand
        {
            ProductId = productId,
            SKU = "CNT-001",
            GeneratedContent = "AI-generated premium description",
            AiProvider = "gpt-4",
            TenantId = Guid.NewGuid()
        };

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        product.Description.Should().Be("AI-generated premium description");
        product.UpdatedBy.Should().Be("mesa-ai");
        product.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _productRepo.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_DoesNotUpdate()
    {
        // Arrange
        var command = new UpdateProductContentCommand
        {
            ProductId = Guid.NewGuid(),
            SKU = "GHOST-SKU",
            GeneratedContent = "Content for missing product",
            AiProvider = "gpt-4",
            TenantId = Guid.NewGuid()
        };

        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepo.Setup(r => r.GetBySKUAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        _productRepo.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyContent_StillUpdatesDescription()
    {
        // Arrange — edge case: empty string content
        var product = FakeData.CreateProduct(sku: "CNT-EMPTY");
        product.Description = "Existing description";

        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateProductContentCommand
        {
            ProductId = product.Id,
            SKU = "CNT-EMPTY",
            GeneratedContent = string.Empty,
            AiProvider = "gpt-4",
            TenantId = Guid.NewGuid()
        };

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — handler sets empty string without validation
        product.Description.Should().BeEmpty();
        _productRepo.Verify(r => r.UpdateAsync(product, It.IsAny<CancellationToken>()), Times.Once);
    }
}

#endregion

#region ApplyOptimizedPrice

[Trait("Category", "Unit")]
[Trait("Feature", "Product")]
public class ApplyOptimizedPriceHandlerTests
{
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IPriceRecommendationRepository> _priceRecoRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ILogger<ApplyOptimizedPriceHandler>> _logger = new();

    private ApplyOptimizedPriceHandler CreateHandler() =>
        new(_productRepo.Object, _priceRecoRepo.Object, _unitOfWork.Object, _logger.Object);

    [Fact]
    public async Task Handle_HighConfidence_AppliesClampedPriceAndSavesRecommendation()
    {
        // Arrange
        var product = FakeData.CreateProduct(sku: "OPT-001", salePrice: 100m, purchasePrice: 60m);
        var productId = product.Id;

        _productRepo.Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ApplyOptimizedPriceCommand
        {
            ProductId = productId,
            SKU = "OPT-001",
            RecommendedPrice = 250m, // above MaxPrice — should clamp
            MinPrice = 80m,
            MaxPrice = 200m,
            Confidence = 0.85,
            Reasoning = "Competitor undercut detected",
            TenantId = Guid.NewGuid()
        };

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — price clamped to MaxPrice
        product.SalePrice.Should().Be(200m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _priceRecoRepo.Verify(r => r.AddAsync(
            It.Is<PriceRecommendation>(p =>
                p.Source == "ai.price.optimized" &&
                p.Confidence == 0.85 &&
                p.RecommendedPrice == 250m),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LowConfidence_SkipsWithoutApplying()
    {
        // Arrange — confidence below 60% threshold
        var product = FakeData.CreateProduct(sku: "OPT-LOW", salePrice: 100m);

        _productRepo.Setup(r => r.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var command = new ApplyOptimizedPriceCommand
        {
            ProductId = product.Id,
            SKU = "OPT-LOW",
            RecommendedPrice = 120m,
            MinPrice = 80m,
            MaxPrice = 200m,
            Confidence = 0.50, // below 0.6 threshold
            TenantId = Guid.NewGuid()
        };

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert — price unchanged, no save
        product.SalePrice.Should().Be(100m);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _priceRecoRepo.Verify(r => r.AddAsync(It.IsAny<PriceRecommendation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProductNotFound_DoesNotApply()
    {
        // Arrange
        _productRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);
        _productRepo.Setup(r => r.GetBySKUAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new ApplyOptimizedPriceCommand
        {
            ProductId = Guid.NewGuid(),
            SKU = "NOPE",
            RecommendedPrice = 100m,
            MinPrice = 50m,
            MaxPrice = 200m,
            Confidence = 0.90,
            TenantId = Guid.NewGuid()
        };

        // Act
        await CreateHandler().Handle(command, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

#endregion
