using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Integration.Orchestration;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Orchestration;

public class OrchestratorTests
{
    private readonly Mock<IAdapterFactory> _factoryMock = new();
    private readonly Mock<ILogger<IntegratorOrchestratorService>> _loggerMock = new();

    private IntegratorOrchestratorService CreateOrchestrator(
        params IIntegratorAdapter[] adapters)
    {
        _factoryMock.Setup(f => f.GetAll())
            .Returns(adapters.ToList().AsReadOnly());

        // Setup Resolve for each adapter by its PlatformCode
        foreach (var adapter in adapters)
        {
            _factoryMock.Setup(f => f.Resolve(adapter.PlatformCode))
                .Returns(adapter);
        }

        return new IntegratorOrchestratorService(_factoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SyncPlatform_Success_ReturnsSuccessResult()
    {
        // Arrange
        var products = new List<Product>
        {
            new() { Name = "Product1" },
            new() { Name = "Product2" }
        };

        var adapter = new TestAdapterBuilder()
            .WithPlatformCode("Trendyol")
            .WithPullProductsResult(products)
            .BuildObject();

        var orchestrator = CreateOrchestrator(adapter);

        // Act
        var result = await orchestrator.SyncPlatformAsync("Trendyol");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Trendyol", result.PlatformCode);
        Assert.Equal(2, result.ItemsProcessed);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task SyncPlatform_UnknownPlatform_ReturnsError()
    {
        // Arrange
        _factoryMock.Setup(f => f.GetAll())
            .Returns(new List<IIntegratorAdapter>().AsReadOnly());
        _factoryMock.Setup(f => f.Resolve("UnknownPlatform"))
            .Returns((IIntegratorAdapter?)null);

        var orchestrator = new IntegratorOrchestratorService(
            _factoryMock.Object, _loggerMock.Object);

        // Act
        var result = await orchestrator.SyncPlatformAsync("UnknownPlatform");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("bulunamadi", result.ErrorMessage!);
    }

    [Fact]
    public async Task SyncAllPlatforms_Parallel_AggregatesResults()
    {
        // Arrange
        var products1 = new List<Product>
        {
            new() { Name = "P1" },
            new() { Name = "P2" }
        };
        var products2 = new List<Product>
        {
            new() { Name = "P3" }
        };

        var adapter1 = new TestAdapterBuilder()
            .WithPlatformCode("Trendyol")
            .WithPullProductsResult(products1)
            .BuildObject();

        var adapter2 = new TestAdapterBuilder()
            .WithPlatformCode("OpenCart")
            .WithPullProductsResult(products2)
            .BuildObject();

        var orchestrator = CreateOrchestrator(adapter1, adapter2);

        // Act
        var result = await orchestrator.SyncAllPlatformsAsync();

        // Assert
        Assert.Equal("ALL", result.PlatformCode);
        Assert.Equal(3, result.ItemsProcessed); // 2 + 1
    }

    [Fact]
    public async Task SyncPlatform_Timeout_GracefulFailure()
    {
        // Arrange — adapter that takes longer than the 30s timeout
        var slowMock = new Mock<IIntegratorAdapter>();
        slowMock.Setup(a => a.PlatformCode).Returns("SlowPlatform");
        slowMock.Setup(a => a.SupportsStockUpdate).Returns(false);
        slowMock.Setup(a => a.SupportsPriceUpdate).Returns(false);
        slowMock.Setup(a => a.SupportsShipment).Returns(false);
        slowMock.Setup(a => a.PullProductsAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                await Task.Delay(TimeSpan.FromSeconds(35), ct);
                return new List<Product>().AsReadOnly();
            });

        var orchestrator = CreateOrchestrator(slowMock.Object);

        // Act
        var result = await orchestrator.SyncPlatformAsync("SlowPlatform");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("timeout", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleStockChanged_CallsCorrectAdapters()
    {
        // Arrange
        var stockAdapterMock = new TestAdapterBuilder()
            .WithPlatformCode("Trendyol")
            .WithStockSupport(true)
            .WithPushStockResult(true)
            .Build();

        var noStockAdapterMock = new TestAdapterBuilder()
            .WithPlatformCode("BasicPlatform")
            .WithStockSupport(false)
            .Build();

        var orchestrator = CreateOrchestrator(
            stockAdapterMock.Object, noStockAdapterMock.Object);

        var productId = Guid.NewGuid();
        var stockEvent = new StockChangedEvent(
            productId, "SKU-001", 10, 15,
            StockMovementType.StockIn, DateTime.UtcNow);

        // Act
        await orchestrator.HandleStockChangedAsync(stockEvent);

        // Assert
        stockAdapterMock.Verify(a => a.PushStockUpdateAsync(
            productId, 15, It.IsAny<CancellationToken>()), Times.Once);
        noStockAdapterMock.Verify(a => a.PushStockUpdateAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandlePriceChanged_FiltersCorrectly()
    {
        // Arrange
        var priceAdapterMock = new TestAdapterBuilder()
            .WithPlatformCode("Trendyol")
            .WithPriceSupport(true)
            .WithPushPriceResult(true)
            .Build();

        var noPriceAdapterMock = new TestAdapterBuilder()
            .WithPlatformCode("BasicPlatform")
            .WithPriceSupport(false)
            .Build();

        var orchestrator = CreateOrchestrator(
            priceAdapterMock.Object, noPriceAdapterMock.Object);

        var productId = Guid.NewGuid();
        var priceEvent = new PriceChangedEvent(
            productId, "SKU-001", 99.90m, 79.90m, DateTime.UtcNow);

        // Act
        await orchestrator.HandlePriceChangedAsync(priceEvent);

        // Assert
        priceAdapterMock.Verify(a => a.PushPriceUpdateAsync(
            productId, 79.90m, It.IsAny<CancellationToken>()), Times.Once);
        noPriceAdapterMock.Verify(a => a.PushPriceUpdateAsync(
            It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAdapter_AddsToList()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(); // start with empty list

        var newAdapter = new TestAdapterBuilder()
            .WithPlatformCode("NewPlatform")
            .BuildObject();

        // Act
        await orchestrator.RegisterAdapterAsync(newAdapter);

        // Assert
        Assert.Contains(orchestrator.RegisteredAdapters,
            a => a.PlatformCode == "NewPlatform");
    }

    [Fact]
    public async Task RemoveAdapter_RemovesFromList()
    {
        // Arrange
        var adapter = new TestAdapterBuilder()
            .WithPlatformCode("Trendyol")
            .BuildObject();

        var orchestrator = CreateOrchestrator(adapter);
        Assert.Contains(orchestrator.RegisteredAdapters,
            a => a.PlatformCode == "Trendyol");

        // Act
        await orchestrator.RemoveAdapterAsync("Trendyol");

        // Assert
        Assert.DoesNotContain(orchestrator.RegisteredAdapters,
            a => a.PlatformCode == "Trendyol");
    }
}
