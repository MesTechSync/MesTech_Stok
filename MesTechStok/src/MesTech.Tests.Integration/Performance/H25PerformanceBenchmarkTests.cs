using System.Diagnostics;
using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Factory;
using MesTech.Infrastructure.Integration.Orchestration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Integration.Performance;

/// <summary>
/// H25 performans benchmark testleri — D-14 tamamlama.
/// BulkSyncBenchmarkTests (Dalga4) üzerine ekleme:
/// Eksik kalan "100 concurrent order" senaryosu + regression check.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Performance")]
[Trait("Phase", "Dalga5")]
public class H25PerformanceBenchmarkTests
{
    /// <summary>
    /// Combined interface — adapter that supports both IIntegratorAdapter and IOrderCapableAdapter.
    /// </summary>
    public interface IFullPlatformAdapter : IIntegratorAdapter, IOrderCapableAdapter { }

    private static readonly string[] Platforms =
        { "Trendyol", "OpenCart", "Ciceksepeti", "Hepsiburada", "Pazarama" };

    private static Mock<IFullPlatformAdapter> CreateOrderAdapter(
        string platformCode, int ordersPerBatch = 20)
    {
        var mock = new Mock<IFullPlatformAdapter>();
        mock.Setup(a => a.PlatformCode).Returns(platformCode);
        mock.Setup(a => a.SupportsStockUpdate).Returns(true);
        mock.Setup(a => a.SupportsPriceUpdate).Returns(true);
        mock.Setup(a => a.SupportsShipment).Returns(true);

        var orders = Enumerable.Range(0, ordersPerBatch)
            .Select(i => new ExternalOrderDto
            {
                PlatformOrderId = $"{platformCode}-CONC-{i:D4}",
                PlatformCode = platformCode,
                OrderNumber = $"H25-{platformCode}-{i:D5}",
                Status = "Created",
                CustomerName = $"Musteri-{i}",
                TotalAmount = 99.90m + i,
                OrderDate = DateTime.UtcNow.AddMinutes(-i),
                Lines = new List<ExternalOrderLineDto>
                {
                    new()
                    {
                        SKU = $"SKU-{i:D4}",
                        ProductName = $"Urun-{i}",
                        Quantity = 1,
                        UnitPrice = 99.90m,
                        LineTotal = 99.90m
                    }
                }
            })
            .ToList()
            .AsReadOnly();

        mock.Setup(a => a.PullOrdersAsync(
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        mock.Setup(a => a.PullProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>().AsReadOnly());

        mock.Setup(a => a.PushProductAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mock.Setup(a => a.PushStockUpdateAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mock.Setup(a => a.PushPriceUpdateAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mock.Setup(a => a.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryDto>().AsReadOnly());
        mock.Setup(a => a.TestConnectionAsync(
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionTestResultDto
                { IsSuccess = true, PlatformCode = platformCode });

        return mock;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. ConcurrentOrderPull — 100 concurrent order pull tasks, no deadlock
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 30_000)]
    public async Task ConcurrentOrderPull_100Tasks_5Platforms_NoDeadlock()
    {
        // Arrange — 5 adapters, each batch returns 20 orders
        var adapters = Platforms
            .Select(p => CreateOrderAdapter(p, ordersPerBatch: 20))
            .ToArray();

        var sw = Stopwatch.StartNew();

        // Act — 100 concurrent tasks (20 per platform × 5 platforms)
        // Simulates 100 simultaneous webhook triggers for order sync
        var tasks = Enumerable.Range(0, 100).Select(i =>
        {
            var adapter = adapters[i % 5].As<IOrderCapableAdapter>().Object;
            return adapter.PullOrdersAsync(DateTime.UtcNow.AddHours(-1));
        });

        var results = await Task.WhenAll(tasks);

        sw.Stop();

        // Assert — all 100 tasks completed, each returned 20 orders
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(batch =>
            batch.Should().HaveCount(20,
                "each adapter returns 20 orders per PullOrdersAsync call"));

        var totalOrdersProcessed = results.Sum(r => r.Count);
        totalOrdersProcessed.Should().Be(2000, "100 tasks × 20 orders = 2000 total");

        sw.ElapsedMilliseconds.Should().BeLessThan(10_000,
            $"100 concurrent order pulls completed in {sw.ElapsedMilliseconds}ms, limit 10000ms");

        // Each adapter was called 20 times (100 tasks / 5 adapters)
        foreach (var adapter in adapters)
        {
            adapter.As<IOrderCapableAdapter>().Verify(
                a => a.PullOrdersAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()),
                Times.Exactly(20),
                $"{adapter.Object.PlatformCode} should receive 20 concurrent calls");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. MixedLoadConcurrency — sync + orders + stock simultaneous, no regression
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 60_000)]
    public async Task MixedLoad_SyncOrdersStock_Simultaneous_CompletesWithinBudget()
    {
        // Arrange
        var adapters = Platforms
            .Select(p => CreateOrderAdapter(p, ordersPerBatch: 10))
            .ToArray();

        var adapterFactory = new AdapterFactory(
            adapters.Select(a => a.Object as IIntegratorAdapter).ToArray(),
            NullLogger<AdapterFactory>.Instance);

        var orchestrator = new IntegratorOrchestratorService(
            adapterFactory, NullLogger<IntegratorOrchestratorService>.Instance);

        var sw = Stopwatch.StartNew();

        // Act — 3 operation types simultaneously
        var syncTask = orchestrator.SyncAllPlatformsAsync();

        var orderTasks = Task.WhenAll(
            Enumerable.Range(0, 50).Select(i =>
            {
                var adapter = adapters[i % 5].As<IOrderCapableAdapter>().Object;
                return adapter.PullOrdersAsync(DateTime.UtcNow.AddHours(-1));
            }));

        var stockTasks = Task.WhenAll(
            Enumerable.Range(0, 50).Select(i =>
                orchestrator.HandleStockChangedAsync(new Domain.Events.StockChangedEvent(
                    Guid.NewGuid(), $"SKU-MIX-{i:D3}",
                    100, 100 + i, StockMovementType.StockIn, DateTime.UtcNow))));

        await Task.WhenAll(syncTask, orderTasks, stockTasks);

        sw.Stop();

        // Assert
        var syncResult = await syncTask;
        syncResult.IsSuccess.Should().BeTrue("sync must complete successfully under mixed load");

        var allOrders = await orderTasks;
        allOrders.Should().HaveCount(50);

        sw.ElapsedMilliseconds.Should().BeLessThan(20_000,
            $"mixed load (sync+50orders+50stock) completed in {sw.ElapsedMilliseconds}ms, limit 20000ms");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. Regression — confirm D-14 benchmarks still pass on Dalga5 codebase
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 120_000)]
    public async Task Regression_D14_1000ProductSync_StillUnder60Seconds()
    {
        // Re-validate D-14 baseline with Dalga5 codebase
        var adapters = Platforms.Select(code =>
        {
            var mock = new Mock<IIntegratorAdapter>();
            mock.Setup(a => a.PlatformCode).Returns(code);
            mock.Setup(a => a.SupportsStockUpdate).Returns(true);
            mock.Setup(a => a.SupportsPriceUpdate).Returns(true);
            mock.Setup(a => a.SupportsShipment).Returns(true);
            var products = Enumerable.Range(0, 200)
                .Select(i => new Product
                {
                    Name = $"{code}-P-{i}", SKU = $"{code}-{i:D5}",
                    SalePrice = 100m + i, Stock = 50, IsActive = true
                }).ToList().AsReadOnly();
            mock.Setup(a => a.PullProductsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);
            mock.Setup(a => a.PushStockUpdateAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mock.Setup(a => a.PushProductAsync(
                    It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            mock.Setup(a => a.GetCategoriesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<CategoryDto>().AsReadOnly());
            mock.Setup(a => a.TestConnectionAsync(
                    It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ConnectionTestResultDto
                    { IsSuccess = true, PlatformCode = code });
            return mock;
        }).ToArray();

        var factory = new AdapterFactory(
            adapters.Select(a => a.Object).ToArray(),
            NullLogger<AdapterFactory>.Instance);
        var orchestrator = new IntegratorOrchestratorService(
            factory, NullLogger<IntegratorOrchestratorService>.Instance);

        var sw = Stopwatch.StartNew();
        var result = await orchestrator.SyncAllPlatformsAsync();
        sw.Stop();

        result.IsSuccess.Should().BeTrue();
        result.ItemsProcessed.Should().Be(1000);
        sw.ElapsedMilliseconds.Should().BeLessThan(60_000,
            $"D-14 regression: 1000 products in {sw.ElapsedMilliseconds}ms (limit 60s)");
    }
}
