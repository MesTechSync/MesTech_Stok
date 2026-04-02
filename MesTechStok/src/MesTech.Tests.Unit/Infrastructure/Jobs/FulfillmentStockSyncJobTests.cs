using FluentAssertions;
using MesTech.Application.DTOs.Fulfillment;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Events;
using MesTech.Domain.Interfaces;
using MesTech.Infrastructure.Integration.Factory;
using MesTech.Infrastructure.Integration.Jobs;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure.Jobs;

/// <summary>
/// DEV 5 — G436: FulfillmentStockSyncJob unit tests.
/// Covers: no-providers skip, no-products skip, provider unavailable skip,
/// stock-changed event firing, stock-unchanged no-event, provider error isolation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Infrastructure")]
public class FulfillmentStockSyncJobTests
{
    private readonly Mock<IFulfillmentProviderFactory> _factoryMock = new();
    private readonly Mock<IStockSplitService> _stockSplitMock = new();
    private readonly Mock<IProductRepository> _productRepoMock = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcherMock = new();
    private readonly ILogger<FulfillmentStockSyncJob> _logger = Mock.Of<ILogger<FulfillmentStockSyncJob>>();

    private FulfillmentStockSyncJob CreateSut() =>
        new(_factoryMock.Object, _stockSplitMock.Object, _productRepoMock.Object, _dispatcherMock.Object, _logger);

    // ── Constructor guard ──

    [Fact]
    public void Constructor_NullFactory_Throws()
    {
        var act = () => new FulfillmentStockSyncJob(
            null!, _stockSplitMock.Object, _productRepoMock.Object, _dispatcherMock.Object, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("fulfillmentFactory");
    }

    [Fact]
    public void Constructor_NullStockSplitService_Throws()
    {
        var act = () => new FulfillmentStockSyncJob(
            _factoryMock.Object, null!, _productRepoMock.Object, _dispatcherMock.Object, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("stockSplitService");
    }

    [Fact]
    public void Constructor_NullProductRepo_Throws()
    {
        var act = () => new FulfillmentStockSyncJob(
            _factoryMock.Object, _stockSplitMock.Object, null!, _dispatcherMock.Object, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("productRepository");
    }

    [Fact]
    public void Constructor_NullDispatcher_Throws()
    {
        var act = () => new FulfillmentStockSyncJob(
            _factoryMock.Object, _stockSplitMock.Object, _productRepoMock.Object, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("eventDispatcher");
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        var act = () => new FulfillmentStockSyncJob(
            _factoryMock.Object, _stockSplitMock.Object, _productRepoMock.Object, _dispatcherMock.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // ── Early exit paths ──

    [Fact]
    public async Task ExecuteAsync_NoProviders_SkipsGracefully()
    {
        _factoryMock.Setup(f => f.GetAll()).Returns(new List<IFulfillmentProvider>().AsReadOnly());

        var sut = CreateSut();
        await sut.ExecuteAsync();

        _productRepoMock.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NoProducts_SkipsGracefully()
    {
        var provider = CreateMockProvider(FulfillmentCenter.AmazonFBA);
        _factoryMock.Setup(f => f.GetAll()).Returns(new List<IFulfillmentProvider> { provider.Object }.AsReadOnly());
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product>().AsReadOnly());

        var sut = CreateSut();
        await sut.ExecuteAsync();

        provider.Verify(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ProviderUnavailable_SkipsProvider()
    {
        var product = CreateProduct("SKU-001");
        var provider = CreateMockProvider(FulfillmentCenter.AmazonFBA, isAvailable: false);

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<IFulfillmentProvider> { provider.Object }.AsReadOnly());
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product> { product }.AsReadOnly());

        var sut = CreateSut();
        await sut.ExecuteAsync();

        provider.Verify(p => p.GetInventoryLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Stock change detection ──

    [Fact]
    public async Task ExecuteAsync_StockChanged_FiresStockChangedEvent()
    {
        var productId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var product = CreateProduct("SKU-001", productId, tenantId);
        var provider = CreateMockProvider(FulfillmentCenter.AmazonFBA);

        var inventory = new FulfillmentInventory(
            FulfillmentCenter.AmazonFBA,
            new List<FulfillmentStock> { new("SKU-001", 50, 0, 0) }.AsReadOnly(),
            DateTime.UtcNow);

        provider.Setup(p => p.GetInventoryLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<IFulfillmentProvider> { provider.Object }.AsReadOnly());
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product> { product }.AsReadOnly());

        // Before sync: 10 units, after sync: 50 units
        _stockSplitMock.SetupSequence(s => s.GetTotalAvailableBulkAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { productId, 10 } })   // pre-fetch
            .ReturnsAsync(new Dictionary<Guid, int> { { productId, 50 } });  // post-fetch

        var sut = CreateSut();
        await sut.ExecuteAsync();

        _dispatcherMock.Verify(d => d.DispatchAsync(
            It.Is<IEnumerable<StockChangedEvent>>(events => events.Any(e =>
                e.ProductId == productId &&
                e.PreviousQuantity == 10 &&
                e.NewQuantity == 50)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_StockUnchanged_NoEventDispatched()
    {
        var productId = Guid.NewGuid();
        var product = CreateProduct("SKU-002", productId);
        var provider = CreateMockProvider(FulfillmentCenter.AmazonFBA);

        var inventory = new FulfillmentInventory(
            FulfillmentCenter.AmazonFBA,
            new List<FulfillmentStock> { new("SKU-002", 30, 0, 0) }.AsReadOnly(),
            DateTime.UtcNow);

        provider.Setup(p => p.GetInventoryLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventory);

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<IFulfillmentProvider> { provider.Object }.AsReadOnly());
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product> { product }.AsReadOnly());

        // Same stock before and after
        _stockSplitMock.Setup(s => s.GetTotalAvailableBulkAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int> { { productId, 30 } });

        var sut = CreateSut();
        await sut.ExecuteAsync();

        _dispatcherMock.Verify(d => d.DispatchAsync(
            It.Is<IEnumerable<StockChangedEvent>>(events => !events.Any()),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Error isolation ──

    [Fact]
    public async Task ExecuteAsync_ProviderThrows_ContinuesNextProvider()
    {
        var product = CreateProduct("SKU-003");
        var failingProvider = CreateMockProvider(FulfillmentCenter.AmazonFBA);
        var workingProvider = CreateMockProvider(FulfillmentCenter.Hepsilojistik);

        failingProvider.Setup(p => p.GetInventoryLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API down"));

        workingProvider.Setup(p => p.GetInventoryLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FulfillmentInventory(
                FulfillmentCenter.Hepsilojistik,
                new List<FulfillmentStock>().AsReadOnly(),
                DateTime.UtcNow));

        _factoryMock.Setup(f => f.GetAll())
            .Returns(new List<IFulfillmentProvider> { failingProvider.Object, workingProvider.Object }.AsReadOnly());
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product> { product }.AsReadOnly());

        var sut = CreateSut();
        await sut.ExecuteAsync(); // should NOT throw

        workingProvider.Verify(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyInventory_SkipsStockUpdate()
    {
        var product = CreateProduct("SKU-004");
        var provider = CreateMockProvider(FulfillmentCenter.AmazonFBA);

        provider.Setup(p => p.GetInventoryLevelsAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FulfillmentInventory(
                FulfillmentCenter.AmazonFBA,
                new List<FulfillmentStock>().AsReadOnly(),
                DateTime.UtcNow));

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<IFulfillmentProvider> { provider.Object }.AsReadOnly());
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product> { product }.AsReadOnly());

        var sut = CreateSut();
        await sut.ExecuteAsync();

        _stockSplitMock.Verify(s => s.UpdateFulfillmentStockAsync(
            It.IsAny<Guid>(), It.IsAny<FulfillmentCenter>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        var product = CreateProduct("SKU-005");
        var provider = CreateMockProvider(FulfillmentCenter.AmazonFBA);

        _factoryMock.Setup(f => f.GetAll()).Returns(new List<IFulfillmentProvider> { provider.Object }.AsReadOnly());
        _productRepoMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<Product> { product }.AsReadOnly());

        var cts = new CancellationTokenSource();
        cts.Cancel();

        var sut = CreateSut();
        await sut.Invoking(s => s.ExecuteAsync(cts.Token)).Should().ThrowAsync<OperationCanceledException>();
    }

    // ── Helpers ──

    private static Mock<IFulfillmentProvider> CreateMockProvider(FulfillmentCenter center, bool isAvailable = true)
    {
        var mock = new Mock<IFulfillmentProvider>();
        mock.Setup(p => p.Center).Returns(center);
        mock.Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>())).ReturnsAsync(isAvailable);
        return mock;
    }

    private static Product CreateProduct(string sku, Guid? id = null, Guid? tenantId = null)
    {
        return new Product
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId ?? Guid.NewGuid(),
            SKU = sku,
            Name = $"Product {sku}"
        };
    }
}
