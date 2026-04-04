using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Application.Features.Dropshipping.Commands.ImportFromFeed;
using MesTech.Application.Features.Dropshipping.Commands.LinkDropshipProduct;
using MesTech.Application.Features.Dropshipping.Commands.PlaceDropshipOrder;
using MesTech.Application.Features.Dropshipping.Commands.PreviewFeed;
using MesTech.Application.Features.Dropshipping.Commands.SyncSupplierPrices;
using MesTech.Application.Features.Dropshipping.Queries.GetDropshipProfitability;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Dropshipping.Enums;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Handlers;

[Trait("Category", "Unit")]
[Trait("Layer", "Dropshipping")]
public class DropshippingBatchTests
{
    // ── 1. CreateFeedSourceCommandHandler ──

    [Fact]
    public async Task CreateFeedSourceHandler_ValidRequest_AddsFeedAndReturnsId()
    {
        // Arrange
        var feedRepo = new Mock<ISupplierFeedRepository>();
        var currentUser = new Mock<ICurrentUserService>();
        var tenantProvider = new Mock<ITenantProvider>();
        currentUser.Setup(u => u.UserId).Returns(Guid.NewGuid());
        tenantProvider.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        var sut = new CreateFeedSourceCommandHandler(feedRepo.Object, currentUser.Object, tenantProvider.Object);

        var command = new CreateFeedSourceCommand(
            SupplierId: Guid.NewGuid(),
            Name: "Test Feed",
            FeedUrl: "https://example.com/feed.xml",
            Format: FeedFormat.Xml,
            PriceMarkupPercent: 10m,
            PriceMarkupFixed: 0m,
            SyncIntervalMinutes: 60,
            TargetPlatforms: null,
            AutoDeactivateOnZeroStock: true);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        feedRepo.Verify(r => r.AddAsync(It.IsAny<SupplierFeed>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── 2. ImportFromFeedHandler ──

    [Fact]
    public async Task ImportFromFeedHandler_FeedNotFound_ThrowsInvalidOperation()
    {
        // Arrange
        var feedRepo = new Mock<ISupplierFeedRepository>();
        var productRepo = new Mock<IDropshipProductRepository>();
        var uow = new Mock<IUnitOfWork>();
        var parsers = Enumerable.Empty<IFeedParserService>();
        var httpFactory = new Mock<IHttpClientFactory>();
        var logger = new Mock<ILogger<ImportFromFeedHandler>>();

        feedRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupplierFeed?)null);

        var sut = new ImportFromFeedHandler(
            feedRepo.Object, productRepo.Object, uow.Object,
            parsers, httpFactory.Object, logger.Object);

        var command = new ImportFromFeedCommand(Guid.NewGuid(), new List<string> { "SKU1" }, 1.2m);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(command, CancellationToken.None));
    }

    // ── 3. LinkDropshipProductHandler ──

    [Fact]
    public async Task LinkDropshipProductHandler_ValidRequest_LinksAndSaves()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var mesTechProductId = Guid.NewGuid();

        var dropProduct = DropshipProduct.Create(
            tenantId, Guid.NewGuid(), "EXT-001", "Test Product", 100m, 10);

        var repo = new Mock<IDropshipProductRepository>();
        repo.Setup(r => r.GetByIdAsync(dropProduct.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dropProduct);

        var uow = new Mock<IUnitOfWork>();
        var sut = new LinkDropshipProductHandler(repo.Object, uow.Object);

        var command = new LinkDropshipProductCommand(tenantId, dropProduct.Id, mesTechProductId);

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        repo.Verify(r => r.UpdateAsync(It.Is<DropshipProduct>(p => p.IsLinked), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── 4. PlaceDropshipOrderHandler ──

    [Fact]
    public async Task PlaceDropshipOrderHandler_ValidRequest_CreatesOrderAndSaves()
    {
        // Arrange
        var repo = new Mock<IDropshipOrderRepository>();
        var uow = new Mock<IUnitOfWork>();
        var sut = new PlaceDropshipOrderHandler(repo.Object, uow.Object);

        var command = new PlaceDropshipOrderCommand(
            TenantId: Guid.NewGuid(),
            OrderId: Guid.NewGuid(),
            SupplierId: Guid.NewGuid(),
            ProductId: Guid.NewGuid(),
            SupplierOrderRef: "SUP-REF-001");

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        repo.Verify(r => r.AddAsync(It.IsAny<DropshipOrder>(), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ── 5. PreviewFeedHandler ──

    [Fact]
    public async Task PreviewFeedHandler_FeedNotFound_ThrowsInvalidOperation()
    {
        // Arrange
        var feedRepo = new Mock<ISupplierFeedRepository>();
        var productRepo = new Mock<IDropshipProductRepository>();
        var parsers = Enumerable.Empty<IFeedParserService>();
        var httpFactory = new Mock<IHttpClientFactory>();
        var logger = new Mock<ILogger<PreviewFeedHandler>>();

        feedRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupplierFeed?)null);

        var sut = new PreviewFeedHandler(
            feedRepo.Object, productRepo.Object, parsers,
            httpFactory.Object, logger.Object);

        var command = new PreviewFeedCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(command, CancellationToken.None));
    }

    // ── 6. SyncSupplierPricesHandler ──

    [Fact]
    public async Task SyncSupplierPricesHandler_SupplierNotFound_ThrowsInvalidOperation()
    {
        // Arrange
        var supplierRepo = new Mock<IDropshipSupplierRepository>();
        var productRepo = new Mock<IDropshipProductRepository>();
        var mainProductRepo = new Mock<IProductRepository>();
        var feedFetcher = new Mock<IDropshipFeedFetcher>();
        var uow = new Mock<IUnitOfWork>();

        supplierRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DropshipSupplier?)null);

        var sut = new SyncSupplierPricesHandler(
            supplierRepo.Object, productRepo.Object,
            mainProductRepo.Object, feedFetcher.Object, uow.Object);

        var command = new SyncSupplierPricesCommand(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.Handle(command, CancellationToken.None));
    }

    // ── 7. GetDropshipProfitabilityHandler ──

    [Fact]
    public async Task GetDropshipProfitabilityHandler_WithOrders_ReturnsProfitList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();

        var product = DropshipProduct.Create(
            tenantId, supplierId, "SKU-PROFIT-1", "Profit Test Product", 50m, 100);
        product.ApplyMarkup(DropshipMarkupType.FixedAmount, 30m); // SellingPrice = 80

        var order = DropshipOrder.Create(tenantId, Guid.NewGuid(), supplierId, product.Id);

        var orderRepo = new Mock<IDropshipOrderRepository>();
        orderRepo.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipOrder> { order });

        var productRepo = new Mock<IDropshipProductRepository>();
        productRepo.Setup(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DropshipProduct> { product });

        var sut = new GetDropshipProfitabilityHandler(orderRepo.Object, productRepo.Object);
        var query = new GetDropshipProfitabilityQuery(tenantId);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        result[0].QuantitySold.Should().Be(1);
        result[0].SupplierPrice.Should().Be(50m);
        result[0].CustomerPrice.Should().Be(80m);
        result[0].NetProfit.Should().Be(30m);
        orderRepo.Verify(r => r.GetByTenantAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        productRepo.Verify(r => r.GetByTenantAsync(tenantId, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
