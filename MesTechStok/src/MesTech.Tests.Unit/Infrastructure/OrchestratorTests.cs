using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Integration.Orchestration;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// IntegratorOrchestratorService unit tests — DEV5 cross-check for DEV3's orchestration logic.
/// Key scenarios: SyncAll parallel execution, one-fail-others-continue, event-driven push.
/// </summary>
[Trait("Category", "Unit")]
public class OrchestratorTests
{
    private static Mock<IIntegratorAdapter> CreateMockAdapter(
        string platformCode,
        int productCount = 5,
        bool supportsStock = true,
        bool supportsPrice = true)
    {
        var mock = new Mock<IIntegratorAdapter>();
        mock.Setup(a => a.PlatformCode).Returns(platformCode);
        mock.Setup(a => a.SupportsStockUpdate).Returns(supportsStock);
        mock.Setup(a => a.SupportsPriceUpdate).Returns(supportsPrice);
        mock.Setup(a => a.PullProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(0, productCount)
                .Select(_ => new Product { Name = "Test" })
                .ToList()
                .AsReadOnly() as IReadOnlyList<Product>);
        mock.Setup(a => a.PushStockUpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mock.Setup(a => a.PushPriceUpdateAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return mock;
    }

    private static (IntegratorOrchestratorService orchestrator, Mock<IAdapterFactory> factory) CreateOrchestrator(
        params Mock<IIntegratorAdapter>[] adapters)
    {
        var factory = new Mock<IAdapterFactory>();
        var adapterList = adapters.Select(m => m.Object).ToList();
        factory.Setup(f => f.GetAll()).Returns(adapterList.AsReadOnly());

        foreach (var adapter in adapters)
        {
            factory.Setup(f => f.Resolve(adapter.Object.PlatformCode))
                .Returns(adapter.Object);
        }

        var logger = new Mock<ILogger<IntegratorOrchestratorService>>();
        var orchestrator = new IntegratorOrchestratorService(factory.Object, logger.Object);
        return (orchestrator, factory);
    }

    // ── SyncPlatformAsync ──

    [Fact]
    public async Task SyncPlatform_KnownPlatform_ShouldReturnSuccess()
    {
        var adapter = CreateMockAdapter("Trendyol", productCount: 10);
        var (orchestrator, _) = CreateOrchestrator(adapter);

        var result = await orchestrator.SyncPlatformAsync("Trendyol");

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("Trendyol");
        result.ItemsProcessed.Should().Be(10);
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SyncPlatform_UnknownPlatform_ShouldReturnErrorWithMessage()
    {
        var (orchestrator, _) = CreateOrchestrator();

        var result = await orchestrator.SyncPlatformAsync("Amazon");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Amazon");
    }

    [Fact]
    public async Task SyncPlatform_AdapterThrows_ShouldReturnErrorNotCrash()
    {
        var adapter = CreateMockAdapter("Failing");
        adapter.Setup(a => a.PullProductsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var (orchestrator, _) = CreateOrchestrator(adapter);

        var result = await orchestrator.SyncPlatformAsync("Failing");

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection refused");
    }

    // ── SyncAllPlatformsAsync — parallel, one fails others continue ──

    [Fact]
    public async Task SyncAll_AllSucceed_ShouldReturnAggregatedSuccess()
    {
        var t = CreateMockAdapter("Trendyol", 10);
        var o = CreateMockAdapter("OpenCart", 5);
        var (orchestrator, _) = CreateOrchestrator(t, o);

        var result = await orchestrator.SyncAllPlatformsAsync();

        result.IsSuccess.Should().BeTrue();
        result.PlatformCode.Should().Be("ALL");
        result.ItemsProcessed.Should().Be(15); // 10 + 5
    }

    [Fact]
    public async Task SyncAll_OneFailsOthersContinue_ShouldReturnPartialFailure()
    {
        var success = CreateMockAdapter("Trendyol", 10);
        var failing = CreateMockAdapter("OpenCart");
        failing.Setup(a => a.PullProductsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("DB connection lost"));

        var (orchestrator, _) = CreateOrchestrator(success, failing);

        var result = await orchestrator.SyncAllPlatformsAsync();

        // One failed → overall not success
        result.IsSuccess.Should().BeFalse();
        // But successful adapter's items still processed
        result.ItemsProcessed.Should().Be(10);
        result.Warnings.Should().ContainSingle(w => w.Contains("OpenCart"));
    }

    [Fact]
    public async Task SyncAll_NoAdapters_ShouldReturnSuccessWithZeroItems()
    {
        var (orchestrator, _) = CreateOrchestrator();

        var result = await orchestrator.SyncAllPlatformsAsync();

        result.IsSuccess.Should().BeTrue();
        result.ItemsProcessed.Should().Be(0);
    }

    // ── RegisteredAdapters ──

    [Fact]
    public async Task RegisterAdapter_ShouldAddToRegisteredList()
    {
        var (orchestrator, _) = CreateOrchestrator();
        var newAdapter = CreateMockAdapter("Hepsiburada");

        await orchestrator.RegisterAdapterAsync(newAdapter.Object);

        orchestrator.RegisteredAdapters.Should().ContainSingle(a => a.PlatformCode == "Hepsiburada");
    }

    [Fact]
    public async Task RegisterAdapter_Duplicate_ShouldNotAddTwice()
    {
        var adapter = CreateMockAdapter("Trendyol");
        var (orchestrator, _) = CreateOrchestrator(adapter);

        var duplicate = CreateMockAdapter("Trendyol");
        await orchestrator.RegisterAdapterAsync(duplicate.Object);

        orchestrator.RegisteredAdapters
            .Count(a => a.PlatformCode == "Trendyol")
            .Should().Be(1);
    }

    [Fact]
    public async Task RemoveAdapter_ShouldRemoveFromList()
    {
        var adapter = CreateMockAdapter("Trendyol");
        var (orchestrator, _) = CreateOrchestrator(adapter);

        await orchestrator.RemoveAdapterAsync("Trendyol");

        orchestrator.RegisteredAdapters.Should().BeEmpty();
    }

    // ── HandleStockChangedAsync — event-driven push ──

    [Fact]
    public async Task HandleStockChanged_ShouldPushToAllStockCapableAdapters()
    {
        var stockCapable = CreateMockAdapter("Trendyol", supportsStock: true);
        var noStock = CreateMockAdapter("OpenCart", supportsStock: false);
        var (orchestrator, _) = CreateOrchestrator(stockCapable, noStock);

        var evt = new StockChangedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-001", 50, 45,
            StockMovementType.Sale, DateTime.UtcNow);

        await orchestrator.HandleStockChangedAsync(evt);

        stockCapable.Verify(a =>
            a.PushStockUpdateAsync(evt.ProductId, 45, It.IsAny<CancellationToken>()), Times.Once);
        noStock.Verify(a =>
            a.PushStockUpdateAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── HandlePriceChangedAsync ──

    [Fact]
    public async Task HandlePriceChanged_ShouldPushToPriceCapableAdapters()
    {
        var priceCapable = CreateMockAdapter("Trendyol", supportsPrice: true);
        var noPrice = CreateMockAdapter("Manual", supportsPrice: false);
        var (orchestrator, _) = CreateOrchestrator(priceCapable, noPrice);

        var evt = new PriceChangedEvent(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-002", 100m, 89.90m, DateTime.UtcNow);

        await orchestrator.HandlePriceChangedAsync(evt);

        priceCapable.Verify(a =>
            a.PushPriceUpdateAsync(evt.ProductId, 89.90m, It.IsAny<CancellationToken>()), Times.Once);
        noPrice.Verify(a =>
            a.PushPriceUpdateAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
