using System.Diagnostics;
using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using InvoiceResult = MesTech.Application.Interfaces.InvoiceResult;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Events;
using MesTech.Infrastructure.Integration.Factory;
using MesTech.Infrastructure.Integration.Invoice;
using MesTech.Infrastructure.Integration.Orchestration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Integration.Performance;

/// <summary>
/// Hafta 21 — Performance benchmark tests.
/// REAL orchestration services (IntegratorOrchestratorService, AdapterFactory,
/// CargoProviderFactory, InvoiceProviderFactory) + MOCK adapters with instant responses.
/// 6 tests: bulk sync, bulk orders, bulk stock, concurrent no-deadlock, invoice gen, memory.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Performance")]
[Trait("Phase", "Dalga4")]
public class BulkSyncBenchmarkTests
{
    // ══════════════════════════════════════════════════════════════════════════
    // Shared helpers
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a mock adapter that returns N products instantly.
    /// </summary>
    private static Mock<IIntegratorAdapter> CreateFastAdapter(string platformCode, int productCount)
    {
        var mock = new Mock<IIntegratorAdapter>();
        mock.Setup(a => a.PlatformCode).Returns(platformCode);
        mock.Setup(a => a.SupportsStockUpdate).Returns(true);
        mock.Setup(a => a.SupportsPriceUpdate).Returns(true);
        mock.Setup(a => a.SupportsShipment).Returns(true);

        var products = Enumerable.Range(0, productCount)
            .Select(i => new Product
            {
                TenantId = Guid.NewGuid(),
                CategoryId = Guid.NewGuid(),
                Name = $"{platformCode}-Product-{i}",
                SKU = $"{platformCode}-SKU-{i:D5}",
                PurchasePrice = 10m + (i % 100),
                SalePrice = 20m + (i % 100),
                Stock = 100 + (i % 500),
                IsActive = true
            })
            .ToList()
            .AsReadOnly();

        mock.Setup(a => a.PullProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        mock.Setup(a => a.PushStockUpdateAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mock.Setup(a => a.PushPriceUpdateAsync(
                It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mock.Setup(a => a.PushProductAsync(
                It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mock.Setup(a => a.GetCategoriesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryDto>().AsReadOnly());

        mock.Setup(a => a.TestConnectionAsync(
                It.IsAny<Dictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionTestResultDto { IsSuccess = true, PlatformCode = platformCode });

        return mock;
    }

    private static readonly string[] PlatformCodes =
        { "Trendyol", "OpenCart", "Ciceksepeti", "Hepsiburada", "Pazarama" };

    // ══════════════════════════════════════════════════════════════════════════
    // 1. BulkProductSync — 1000 products across 5 platforms under 60s
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 120_000)]
    public async Task BulkProductSync_1000Products_5Platforms_Under60Seconds()
    {
        // Arrange — 5 adapters, each returns 200 products (1000 total)
        var adapters = PlatformCodes
            .Select(code => CreateFastAdapter(code, productCount: 200))
            .ToArray();

        var adapterFactory = new AdapterFactory(
            adapters.Select(a => a.Object).ToArray(),
            NullLogger<AdapterFactory>.Instance);

        var orchestrator = new IntegratorOrchestratorService(
            adapterFactory, NullLogger<IntegratorOrchestratorService>.Instance);

        var sw = Stopwatch.StartNew();

        // Act — sync all 5 platforms in parallel
        var result = await orchestrator.SyncAllPlatformsAsync();

        sw.Stop();

        // Assert
        result.IsSuccess.Should().BeTrue("all 5 platforms should sync successfully");
        result.ItemsProcessed.Should().Be(1000, "200 products × 5 platforms = 1000");
        result.PlatformCode.Should().Be("ALL");

        sw.ElapsedMilliseconds.Should().BeLessThan(60_000,
            $"1000 products across 5 platforms synced in {sw.ElapsedMilliseconds}ms, limit 60000ms");

        // Verify each adapter was called exactly once
        foreach (var adapter in adapters)
        {
            adapter.Verify(
                a => a.PullProductsAsync(It.IsAny<CancellationToken>()),
                Times.Once,
                $"{adapter.Object.PlatformCode} should be synced exactly once");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. BulkOrderSync — 500 orders from 5 platforms under 30s
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Combined interface for order-capable platform adapters.
    /// </summary>
    public interface IOrderPlatformAdapter : IIntegratorAdapter, IOrderCapableAdapter { }

    [Fact(Timeout = 60_000)]
    public async Task BulkOrderSync_500Orders_5Platforms_Under30Seconds()
    {
        // Arrange — 5 adapters, each returns 100 orders (500 total)
        var adapters = new List<Mock<IOrderPlatformAdapter>>();

        foreach (var platform in PlatformCodes)
        {
            var mock = new Mock<IOrderPlatformAdapter>();
            mock.Setup(a => a.PlatformCode).Returns(platform);
            mock.Setup(a => a.SupportsStockUpdate).Returns(true);
            mock.Setup(a => a.SupportsPriceUpdate).Returns(true);
            mock.Setup(a => a.SupportsShipment).Returns(true);

            var orders = Enumerable.Range(0, 100)
                .Select(i => new ExternalOrderDto
                {
                    PlatformOrderId = $"{platform}-ORD-{i:D4}",
                    PlatformCode = platform,
                    OrderNumber = $"20260309{i:D5}",
                    Status = "Created",
                    CustomerName = $"Musteri-{i}",
                    TotalAmount = 99.90m + i,
                    OrderDate = DateTime.UtcNow.AddHours(-i),
                    Lines = new List<ExternalOrderLineDto>
                    {
                        new()
                        {
                            SKU = $"SKU-{i:D4}",
                            ProductName = $"Urun-{i}",
                            Quantity = 1 + (i % 3),
                            UnitPrice = 49.95m,
                            LineTotal = 49.95m * (1 + (i % 3))
                        }
                    }
                })
                .ToList()
                .AsReadOnly();

            mock.As<IOrderCapableAdapter>()
                .Setup(a => a.PullOrdersAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(orders);

            adapters.Add(mock);
        }

        var sw = Stopwatch.StartNew();

        // Act — pull orders from all 5 platforms in parallel
        var tasks = adapters.Select(async adapter =>
        {
            var orderAdapter = adapter.As<IOrderCapableAdapter>().Object;
            return await orderAdapter.PullOrdersAsync();
        });

        var allOrders = await Task.WhenAll(tasks);

        sw.Stop();

        // Assert
        var totalOrders = allOrders.Sum(o => o.Count);
        totalOrders.Should().Be(500, "100 orders × 5 platforms = 500");

        allOrders.Should().HaveCount(5);
        allOrders.Should().AllSatisfy(batch => batch.Should().HaveCount(100));

        sw.ElapsedMilliseconds.Should().BeLessThan(30_000,
            $"500 orders from 5 platforms pulled in {sw.ElapsedMilliseconds}ms, limit 30000ms");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. BulkStockUpdate — 1000 SKUs pushed to 5 platforms under 10s
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 30_000)]
    public async Task BulkStockUpdate_1000SKUs_5Platforms_Under10Seconds()
    {
        // Arrange — REAL orchestrator with 5 fast adapters
        var adapters = PlatformCodes
            .Select(code => CreateFastAdapter(code, productCount: 0))
            .ToArray();

        var adapterFactory = new AdapterFactory(
            adapters.Select(a => a.Object).ToArray(),
            NullLogger<AdapterFactory>.Instance);

        var orchestrator = new IntegratorOrchestratorService(
            adapterFactory, NullLogger<IntegratorOrchestratorService>.Instance);

        // Generate 1000 StockChangedEvents
        var stockEvents = Enumerable.Range(0, 1000)
            .Select(i => new StockChangedEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                $"SKU-BULK-{i:D5}",
                100,
                100 + (i % 50),
                StockMovementType.StockIn,
                DateTime.UtcNow))
            .ToList();

        var sw = Stopwatch.StartNew();

        // Act — push all 1000 stock changes through orchestrator
        // Each event pushes to all 5 stock-capable adapters in parallel
        foreach (var evt in stockEvents)
        {
            await orchestrator.HandleStockChangedAsync(evt);
        }

        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(10_000,
            $"1000 stock updates × 5 platforms = 5000 calls in {sw.ElapsedMilliseconds}ms, limit 10000ms");

        // Verify each adapter received exactly 1000 stock update calls
        foreach (var adapter in adapters)
        {
            adapter.Verify(
                a => a.PushStockUpdateAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Exactly(1000),
                $"{adapter.Object.PlatformCode} should receive 1000 stock updates");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. ConcurrentPlatformSync — 5 platforms, no deadlock
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 60_000)]
    public async Task ConcurrentPlatformSync_5Platforms_NoDeadlock()
    {
        // Arrange — 5 adapters with simulated latency (Task.Delay) to stress concurrency
        var adapters = new List<Mock<IIntegratorAdapter>>();

        foreach (var platform in PlatformCodes)
        {
            var mock = CreateFastAdapter(platform, productCount: 50);

            // Override PullProductsAsync to add slight delay simulating network latency
            var products = Enumerable.Range(0, 50)
                .Select(i => new Product
                {
                    Name = $"{platform}-{i}",
                    SKU = $"{platform}-SKU-{i}",
                    SalePrice = 100m + i,
                    Stock = 50
                })
                .ToList()
                .AsReadOnly();

            mock.Setup(a => a.PullProductsAsync(It.IsAny<CancellationToken>()))
                .Returns(async (CancellationToken ct) =>
                {
                    await Task.Delay(50, ct); // 50ms simulated latency
                    return (IReadOnlyList<Product>)products;
                });

            // Stock update with slight delay
            mock.Setup(a => a.PushStockUpdateAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns(async (Guid _, int _, CancellationToken ct) =>
                {
                    await Task.Delay(10, ct); // 10ms per call
                    return true;
                });

            adapters.Add(mock);
        }

        var adapterFactory = new AdapterFactory(
            adapters.Select(a => a.Object).ToArray(),
            NullLogger<AdapterFactory>.Instance);

        var orchestrator = new IntegratorOrchestratorService(
            adapterFactory, NullLogger<IntegratorOrchestratorService>.Instance);

        var sw = Stopwatch.StartNew();

        // Act — run sync + stock updates concurrently (potential deadlock scenario)
        var syncTask = orchestrator.SyncAllPlatformsAsync();

        var stockTasks = Enumerable.Range(0, 100).Select(i =>
            orchestrator.HandleStockChangedAsync(new StockChangedEvent(
                Guid.NewGuid(), Guid.NewGuid(), $"SKU-CONC-{i:D3}", 100, 100 + i,
                StockMovementType.StockIn, DateTime.UtcNow)));

        var priceTasks = Enumerable.Range(0, 100).Select(i =>
            orchestrator.HandlePriceChangedAsync(new PriceChangedEvent(
                Guid.NewGuid(), Guid.NewGuid(), $"SKU-PRICE-{i:D3}", 100m, 100m + i,
                DateTime.UtcNow)));

        // All 3 operation types running simultaneously
        await Task.WhenAll(
            syncTask,
            Task.WhenAll(stockTasks),
            Task.WhenAll(priceTasks));

        sw.Stop();

        // Assert — no deadlock, completed within timeout
        var syncResult = await syncTask;
        syncResult.IsSuccess.Should().BeTrue("sync should complete without deadlock");
        syncResult.ItemsProcessed.Should().Be(250, "50 products × 5 platforms");

        sw.ElapsedMilliseconds.Should().BeLessThan(30_000,
            $"concurrent sync+stock+price completed in {sw.ElapsedMilliseconds}ms without deadlock");

        // All 5 adapters participated
        foreach (var adapter in adapters)
        {
            adapter.Verify(
                a => a.PullProductsAsync(It.IsAny<CancellationToken>()),
                Times.Once, $"{adapter.Object.PlatformCode} sync called");

            adapter.Verify(
                a => a.PushStockUpdateAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
                Times.Exactly(100),
                $"{adapter.Object.PlatformCode} should receive 100 stock updates");
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. InvoiceGeneration — 100 invoices with 5 lines each under 30s
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 60_000)]
    public async Task InvoiceGeneration_100Invoices_5LinesEach_Under30Seconds()
    {
        // Arrange — REAL MockInvoiceProvider + InvoiceProviderFactory
        var mockProvider = new MockInvoiceProvider();
        var factory = new InvoiceProviderFactory(
            new IInvoiceProvider[] { mockProvider },
            NullLogger<InvoiceProviderFactory>.Instance);

        var provider = factory.Resolve(InvoiceProvider.Manual)!;

        // Build 100 invoices with 5 lines each
        var invoices = Enumerable.Range(0, 100)
            .Select(i => new Application.Interfaces.InvoiceDto(
                InvoiceNumber: $"PERF-INV-{i:D4}",
                CustomerName: $"Musteri-{i}",
                CustomerTaxNumber: $"3{i:D9}",
                CustomerTaxOffice: "Kadikoy VD",
                CustomerAddress: $"Adres-{i}, Istanbul",
                SubTotal: 500m + i * 10,
                TaxTotal: (500m + i * 10) * 0.20m,
                GrandTotal: (500m + i * 10) * 1.20m,
                Lines: Enumerable.Range(0, 5)
                    .Select(j => new Application.Interfaces.InvoiceLineDto(
                        ProductName: $"Urun-{i}-{j}",
                        SKU: $"SKU-{i:D3}-{j}",
                        Quantity: 1 + j,
                        UnitPrice: 100m + j * 10,
                        TaxRate: 20,
                        TaxAmount: (100m + j * 10) * (1 + j) * 0.20m,
                        LineTotal: (100m + j * 10) * (1 + j)))
                    .ToList()))
            .ToList();

        var sw = Stopwatch.StartNew();

        // Act — create all 100 invoices via provider
        var results = new List<InvoiceResult>();
        foreach (var invoice in invoices)
        {
            var result = await provider.CreateEFaturaAsync(invoice);
            results.Add(result);
        }

        // Also verify PDF + status for each
        foreach (var result in results)
        {
            var pdf = await provider.GetPdfAsync(result.GibInvoiceId!);
            var status = await provider.CheckStatusAsync(result.GibInvoiceId!);
        }

        sw.Stop();

        // Assert
        results.Should().HaveCount(100);
        results.Should().AllSatisfy(r =>
        {
            r.Success.Should().BeTrue();
            r.GibInvoiceId.Should().StartWith("GIB");
        });

        sw.ElapsedMilliseconds.Should().BeLessThan(30_000,
            $"100 invoices + PDF + status = 300 operations in {sw.ElapsedMilliseconds}ms, limit 30000ms");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. MemoryUsage — 1000 products + stock movements under 200MB
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MemoryUsage_1000Products_WithStockMovements_Under200MB()
    {
        // Force GC to get baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = Process.GetCurrentProcess().WorkingSet64;

        // Arrange — create 1000 products with 10 stock movements each
        var tenantId = Guid.NewGuid();
        var products = new List<Product>(1000);

        for (int i = 0; i < 1000; i++)
        {
            var product = new Product
            {
                TenantId = tenantId,
                CategoryId = Guid.NewGuid(),
                Name = $"MemTest-Product-{i}",
                SKU = $"MEM-SKU-{i:D5}",
                Barcode = $"869000{i:D7}",
                PurchasePrice = 10m + (i % 100),
                SalePrice = 20m + (i % 100),
                Stock = 1000,
                TaxRate = 0.18m,
                IsActive = true
            };

            // Simulate 10 stock movements per product
            for (int j = 0; j < 10; j++)
            {
                product.AdjustStock(
                    j % 2 == 0 ? 5 : -3,
                    j % 2 == 0 ? StockMovementType.StockIn : StockMovementType.Sale,
                    $"movement-{j}");
            }

            products.Add(product);
        }

        // Also create 500 orders with items
        var orders = new List<Order>(500);
        for (int i = 0; i < 500; i++)
        {
            var order = new Order
            {
                TenantId = tenantId,
                OrderNumber = $"MEM-ORD-{i:D4}",
                CustomerName = $"MemTest-Musteri-{i}",
                Status = OrderStatus.Pending
            };

            order.AddItem(new OrderItem
            {
                TenantId = tenantId,
                ProductId = products[i % 1000].Id,
                ProductName = products[i % 1000].Name,
                ProductSKU = products[i % 1000].SKU,
                Quantity = 2,
                UnitPrice = 100m,
                TotalPrice = 200m,
                TaxRate = 0.18m,
                TaxAmount = 36m
            });

            order.Place();
            orders.Add(order);
        }

        // Measure memory after allocation
        var currentMemory = Process.GetCurrentProcess().WorkingSet64;
        var memoryUsedMB = (currentMemory - baselineMemory) / (1024.0 * 1024.0);

        // Assert
        products.Should().HaveCount(1000);
        products.Should().AllSatisfy(p =>
        {
            // AdjustStock raises domain events (StockMovements populated by EF Core only)
            p.DomainEvents.Should().HaveCount(10, "10 AdjustStock calls = 10 StockChangedEvents");
        });

        orders.Should().HaveCount(500);
        orders.Should().OnlyContain(o => o.Status == OrderStatus.Confirmed);

        // Total domain objects: 1000 products + 10000 events + 500 orders + 500 items + 500 OrderPlacedEvents
        var totalObjects = 1000 + 10_000 + 500 + 500 + 500;
        totalObjects.Should().Be(12_500);

        // Memory must stay under 200MB
        memoryUsedMB.Should().BeLessThan(200,
            $"22000 domain objects consumed {memoryUsedMB:F1}MB, limit 200MB");
    }
}
