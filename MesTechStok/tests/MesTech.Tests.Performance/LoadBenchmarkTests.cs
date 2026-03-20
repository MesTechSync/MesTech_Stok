using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace MesTech.Tests.Performance;

/// <summary>
/// I-18 P-01: Load Benchmark Tests — 7 simulated performance scenarios.
/// These tests use in-process computation to validate timing thresholds
/// without requiring external infrastructure (DB, API, etc.).
/// Each scenario performs realistic computational work to exercise
/// the patterns used in production code paths.
/// </summary>
[Trait("Category", "Performance")]
[Trait("EMR", "I-18-P01")]
public sealed class LoadBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public LoadBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ══════════════════════════════════════════════════════════════════
    // 1. ProductSync_1000_Items — simulate 1000 product sync, <500ms
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void ProductSync_1000_Items_ShouldComplete_Under500ms()
    {
        // Arrange — build 1000 product DTOs with realistic field sizes
        var products = new List<ProductDto>(1000);

        var sw = Stopwatch.StartNew();

        // Act — simulate serialization, validation, and dictionary indexing
        for (int i = 0; i < 1000; i++)
        {
            var product = new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = $"Urun-{i:D4}-{Guid.NewGuid():N}",
                SKU = $"SKU-{i:D4}",
                Barcode = $"8690000{i:D6}",
                Stock = 100 + (i % 50),
                PurchasePrice = 10.50m + (i % 200),
                SalePrice = 25.99m + (i % 300),
                TaxRate = 0.18m,
                CategoryName = $"Kategori-{i % 20}",
                IsActive = i % 10 != 0
            };
            products.Add(product);
        }

        // Simulate sync: index by SKU, detect duplicates, compute hash
        var skuIndex = new Dictionary<string, ProductDto>(1000);
        var duplicates = new List<string>();
        foreach (var p in products)
        {
            var hash = ComputeSimpleHash(p.SKU + p.Name + p.Barcode);
            if (!skuIndex.TryAdd(p.SKU, p))
                duplicates.Add(p.SKU);

            // Simulate price calculation with tax
            var grossPrice = p.SalePrice * (1 + p.TaxRate);
            var margin = (grossPrice - p.PurchasePrice) / grossPrice * 100;
            _ = margin; // force computation
        }

        // Simulate batch grouping by category
        var categoryGroups = products
            .GroupBy(p => p.CategoryName)
            .Select(g => new { Category = g.Key, Count = g.Count(), TotalStock = g.Sum(p => p.Stock) })
            .OrderByDescending(g => g.TotalStock)
            .ToList();

        sw.Stop();

        // Output
        var elapsed = sw.ElapsedMilliseconds;
        _output.WriteLine($"[P-01 S1] ProductSync 1000 items: {elapsed}ms");
        _output.WriteLine($"  SKU index size: {skuIndex.Count}");
        _output.WriteLine($"  Category groups: {categoryGroups.Count}");
        _output.WriteLine($"  Duplicates: {duplicates.Count}");
        _output.WriteLine($"  Target: <500ms | Status: {(elapsed < 500 ? "PASS" : "FAIL")}");

        // Assert
        skuIndex.Should().HaveCount(1000);
        categoryGroups.Should().HaveCountGreaterThan(0);
        elapsed.Should().BeLessThan(500,
            "1000 product sync simulation should complete under 500ms");
    }

    // ══════════════════════════════════════════════════════════════════
    // 2. OrderFetch_500_WithIncludes — simulate 500 order fetch, <200ms
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void OrderFetch_500_WithIncludes_ShouldComplete_Under200ms()
    {
        // Arrange — build 500 orders with 2–4 items each
        var orders = new List<OrderDto>(500);
        var rng = new Random(42);

        for (int i = 0; i < 500; i++)
        {
            var itemCount = rng.Next(2, 5);
            var items = Enumerable.Range(0, itemCount).Select(j => new OrderItemDto
            {
                Id = Guid.NewGuid(),
                ProductName = $"Product-{i}-{j}",
                SKU = $"SKU-{i:D4}-{j}",
                Quantity = rng.Next(1, 10),
                UnitPrice = 15.00m + rng.Next(0, 500),
                TaxRate = 0.18m
            }).ToList();

            orders.Add(new OrderDto
            {
                Id = Guid.NewGuid(),
                OrderNumber = $"ORD-{i:D5}",
                CustomerName = $"Customer-{i % 100}",
                Status = (i % 5) switch
                {
                    0 => "Pending",
                    1 => "Confirmed",
                    2 => "Shipped",
                    3 => "Delivered",
                    _ => "Cancelled"
                },
                OrderDate = DateTime.UtcNow.AddDays(-rng.Next(0, 365)),
                Items = items,
                TotalAmount = items.Sum(it => it.UnitPrice * it.Quantity * (1 + it.TaxRate))
            });
        }

        // Act — simulate paged fetch with sorting, filtering, Include projection
        var sw = Stopwatch.StartNew();

        var page = orders
            .Where(o => o.Status != "Cancelled")
            .OrderByDescending(o => o.OrderDate)
            .Skip(0)
            .Take(50)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.CustomerName,
                o.Status,
                o.OrderDate,
                o.TotalAmount,
                ItemCount = o.Items.Count,
                TopItem = o.Items.OrderByDescending(it => it.UnitPrice * it.Quantity).FirstOrDefault()?.ProductName
            })
            .ToList();

        // Simulate Include: resolve all items for page results
        var pageOrderIds = page.Select(p => p.Id).ToHashSet();
        var resolvedItems = orders
            .Where(o => pageOrderIds.Contains(o.Id))
            .SelectMany(o => o.Items)
            .ToList();

        sw.Stop();

        // Output
        var elapsed = sw.ElapsedMilliseconds;
        _output.WriteLine($"[P-01 S2] OrderFetch 500 with Includes: {elapsed}ms");
        _output.WriteLine($"  Page size: {page.Count}");
        _output.WriteLine($"  Resolved items: {resolvedItems.Count}");
        _output.WriteLine($"  Target: <200ms | Status: {(elapsed < 200 ? "PASS" : "FAIL")}");

        // Assert
        page.Should().HaveCount(50);
        resolvedItems.Should().HaveCountGreaterThan(0);
        elapsed.Should().BeLessThan(200,
            "500 order fetch with includes should complete under 200ms");
    }

    // ══════════════════════════════════════════════════════════════════
    // 3. ConcurrentApi_100_Requests — Task.WhenAll, P99 <1000ms
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ConcurrentApi_100_Requests_P99_Under1000ms()
    {
        // Arrange
        const int requestCount = 100;
        var latencies = new ConcurrentBag<long>();

        // Shared data pool simulating DB
        var productPool = Enumerable.Range(0, 5000)
            .Select(i => new ProductDto
            {
                Id = Guid.NewGuid(),
                Name = $"ConcProduct-{i}",
                SKU = $"CONC-{i:D5}",
                Stock = 100 + (i % 200),
                SalePrice = 20m + (i % 500)
            })
            .ToDictionary(p => p.SKU);

        // Act — fire 100 concurrent simulated API requests
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, requestCount).Select(async i =>
        {
            var taskSw = Stopwatch.StartNew();

            // Simulate API work: query, filter, serialize
            await Task.Run(() =>
            {
                var searchTerm = $"CONC-{(i * 50):D5}";
                var results = productPool.Values
                    .Where(p => p.Stock > 50 && p.SalePrice > 30m)
                    .OrderBy(p => p.Name)
                    .Skip(i % 10 * 20)
                    .Take(20)
                    .ToList();

                // Simulate JSON serialization overhead
                var serialized = System.Text.Json.JsonSerializer.Serialize(results);
                _ = System.Text.Json.JsonSerializer.Deserialize<List<ProductDto>>(serialized);

                // Simulate response header/status computation
                var checksum = serialized.Length ^ results.Count;
                _ = checksum;
            });

            taskSw.Stop();
            latencies.Add(taskSw.ElapsedMilliseconds);
        }).ToArray();

        await Task.WhenAll(tasks);
        sw.Stop();

        // Calculate percentiles
        var sorted = latencies.OrderBy(x => x).ToArray();
        var p50 = sorted[sorted.Length / 2];
        var p95 = sorted[(int)(sorted.Length * 0.95)];
        var p99 = sorted[Math.Min((int)(sorted.Length * 0.99), sorted.Length - 1)];
        var max = sorted[^1];
        var avg = sorted.Average();

        // Output
        _output.WriteLine($"[P-01 S3] ConcurrentApi 100 requests: {sw.ElapsedMilliseconds}ms total");
        _output.WriteLine($"  P50: {p50}ms | P95: {p95}ms | P99: {p99}ms | Max: {max}ms | Avg: {avg:F1}ms");
        _output.WriteLine($"  Target (P99): <1000ms | Status: {(p99 < 1000 ? "PASS" : "FAIL")}");

        // Assert
        sorted.Length.Should().Be(requestCount);
        p99.Should().BeLessThan(1000,
            "P99 latency for 100 concurrent API requests should be under 1000ms");
    }

    // ══════════════════════════════════════════════════════════════════
    // 4. ParallelInvoice_10 — 10 parallel invoice creations, <2000ms
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task ParallelInvoice_10_ShouldComplete_Under2000ms()
    {
        // Arrange
        var invoiceResults = new ConcurrentBag<(int Index, long LatencyMs, Guid InvoiceId)>();

        // Act — create 10 invoices in parallel with realistic computation
        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var taskSw = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                // Simulate invoice line generation (20-50 lines per invoice)
                var lineCount = 20 + (i * 3);
                var lines = Enumerable.Range(0, lineCount).Select(j => new
                {
                    ProductName = $"InvProduct-{i}-{j}",
                    Quantity = 1 + (j % 5),
                    UnitPrice = 15.50m + (j * 2.25m),
                    TaxRate = j % 3 == 0 ? 0.18m : 0.08m
                }).ToList();

                // Simulate invoice computation
                var subtotal = lines.Sum(l => l.UnitPrice * l.Quantity);
                var taxTotal = lines.Sum(l => l.UnitPrice * l.Quantity * l.TaxRate);
                var grandTotal = subtotal + taxTotal;

                // Simulate UBL-TR XML generation
                var xmlBuilder = new System.Text.StringBuilder(4096);
                xmlBuilder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                xmlBuilder.Append("<Invoice xmlns=\"urn:oasis:names:specification:ubl:schema:xsd:Invoice-2\">");
                xmlBuilder.Append($"<ID>INV-{i:D4}</ID>");
                xmlBuilder.Append($"<IssueDate>{DateTime.UtcNow:yyyy-MM-dd}</IssueDate>");
                foreach (var line in lines)
                {
                    xmlBuilder.Append("<InvoiceLine>");
                    xmlBuilder.Append($"<Quantity>{line.Quantity}</Quantity>");
                    xmlBuilder.Append($"<LineExtensionAmount>{line.UnitPrice * line.Quantity:F2}</LineExtensionAmount>");
                    xmlBuilder.Append("</InvoiceLine>");
                }
                xmlBuilder.Append($"<LegalMonetaryTotal><PayableAmount>{grandTotal:F2}</PayableAmount></LegalMonetaryTotal>");
                xmlBuilder.Append("</Invoice>");

                var xml = xmlBuilder.ToString();

                // Simulate hash/signature computation
                var hash = 0L;
                foreach (char c in xml)
                    hash = (hash * 31 + c) % long.MaxValue;

                invoiceResults.Add((i, 0, Guid.NewGuid()));
            });

            taskSw.Stop();
            return (Index: i, LatencyMs: taskSw.ElapsedMilliseconds);
        }).ToArray();

        var results = await Task.WhenAll(tasks);
        sw.Stop();

        var totalElapsed = sw.ElapsedMilliseconds;
        var maxLatency = results.Max(r => r.LatencyMs);

        // Output
        _output.WriteLine($"[P-01 S4] ParallelInvoice 10: {totalElapsed}ms total");
        foreach (var r in results.OrderBy(r => r.Index))
            _output.WriteLine($"  Invoice {r.Index + 1}: {r.LatencyMs}ms");
        _output.WriteLine($"  Max single: {maxLatency}ms");
        _output.WriteLine($"  Target: <2000ms | Status: {(totalElapsed < 2000 ? "PASS" : "FAIL")}");

        // Assert
        results.Should().HaveCount(10);
        totalElapsed.Should().BeLessThan(2000,
            "10 parallel invoice creations should complete under 2000ms");
    }

    // ══════════════════════════════════════════════════════════════════
    // 5. Dashboard_10K_Orders — aggregate stats, <500ms
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void Dashboard_10K_Orders_Aggregate_ShouldComplete_Under500ms()
    {
        // Arrange — generate 10K orders with realistic distribution
        var rng = new Random(123);
        var statuses = new[] { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" };
        var platforms = new[] { "Trendyol", "Hepsiburada", "N11", "Amazon", "Shopify" };

        var orders = Enumerable.Range(0, 10_000).Select(i => new OrderDto
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"DASH-{i:D6}",
            CustomerName = $"Customer-{i % 500}",
            Status = statuses[rng.Next(statuses.Length)],
            OrderDate = DateTime.UtcNow.AddDays(-rng.Next(0, 365)),
            TotalAmount = 25m + rng.Next(0, 5000),
            Items = new List<OrderItemDto>
            {
                new() { Id = Guid.NewGuid(), ProductName = $"P-{i}", Quantity = rng.Next(1, 5), UnitPrice = 50m, TaxRate = 0.18m, SKU = $"S-{i}" }
            },
            Platform = platforms[rng.Next(platforms.Length)]
        }).ToList();

        // Act — simulate 5 KPI aggregation queries
        var sw = Stopwatch.StartNew();

        // KPI 1: Total order count
        var totalOrders = orders.Count;

        // KPI 2: Total revenue
        var totalRevenue = orders.Sum(o => o.TotalAmount);

        // KPI 3: Average order value
        var avgOrderValue = orders.Average(o => o.TotalAmount);

        // KPI 4: Status breakdown with revenue
        var statusBreakdown = orders
            .GroupBy(o => o.Status)
            .Select(g => new
            {
                Status = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount),
                AvgValue = g.Average(o => o.TotalAmount)
            })
            .OrderByDescending(g => g.Revenue)
            .ToList();

        // KPI 5: Platform breakdown
        var platformBreakdown = orders
            .GroupBy(o => o.Platform)
            .Select(g => new
            {
                Platform = g.Key,
                Count = g.Count(),
                Revenue = g.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(g => g.Revenue)
            .ToList();

        // KPI 6: Last 30 days trend (daily)
        var last30Days = orders
            .Where(o => o.OrderDate >= DateTime.UtcNow.AddDays(-30))
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new { Date = g.Key, Count = g.Count(), Revenue = g.Sum(o => o.TotalAmount) })
            .OrderBy(g => g.Date)
            .ToList();

        // KPI 7: Top 10 customers
        var topCustomers = orders
            .GroupBy(o => o.CustomerName)
            .Select(g => new { Customer = g.Key, OrderCount = g.Count(), TotalSpent = g.Sum(o => o.TotalAmount) })
            .OrderByDescending(g => g.TotalSpent)
            .Take(10)
            .ToList();

        sw.Stop();

        // Output
        var elapsed = sw.ElapsedMilliseconds;
        _output.WriteLine($"[P-01 S5] Dashboard 10K orders aggregate: {elapsed}ms");
        _output.WriteLine($"  Total: {totalOrders} | Revenue: {totalRevenue:N2} | Avg: {avgOrderValue:N2}");
        _output.WriteLine($"  Status groups: {statusBreakdown.Count} | Platforms: {platformBreakdown.Count}");
        _output.WriteLine($"  Last 30 days points: {last30Days.Count} | Top customers: {topCustomers.Count}");
        _output.WriteLine($"  Target: <500ms | Status: {(elapsed < 500 ? "PASS" : "FAIL")}");

        // Assert
        totalOrders.Should().Be(10_000);
        statusBreakdown.Should().HaveCount(5);
        platformBreakdown.Should().HaveCount(5);
        topCustomers.Should().HaveCount(10);
        elapsed.Should().BeLessThan(500,
            "dashboard aggregate over 10K orders should complete under 500ms");
    }

    // ══════════════════════════════════════════════════════════════════
    // 6. StockUpdate_500_Burst — 500 stock updates, <5000ms
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void StockUpdate_500_Burst_ShouldComplete_Under5000ms()
    {
        // Arrange — build 500 products with stock tracking
        var products = Enumerable.Range(0, 500).Select(i => new StockItem
        {
            Id = Guid.NewGuid(),
            SKU = $"BURST-{i:D4}",
            CurrentStock = 100,
            MinStock = 5,
            MaxStock = 500,
            Movements = new List<StockMovement>()
        }).ToList();

        var skuIndex = products.ToDictionary(p => p.SKU);

        // Act — simulate 500 burst stock updates with movement logging
        var sw = Stopwatch.StartNew();

        var lostUpdates = 0;
        var movementLog = new List<StockMovement>(500);

        foreach (var product in products)
        {
            var previousStock = product.CurrentStock;
            var adjustment = -10;

            product.CurrentStock += adjustment;

            // Simulate movement record creation
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                SKU = product.SKU,
                PreviousStock = previousStock,
                NewStock = product.CurrentStock,
                Quantity = adjustment,
                Type = "Sale",
                Timestamp = DateTime.UtcNow,
                Reason = "Burst performance test"
            };
            product.Movements.Add(movement);
            movementLog.Add(movement);

            // Simulate constraint validation
            if (product.CurrentStock < product.MinStock)
            {
                // Would trigger reorder event
                _ = new { product.SKU, product.CurrentStock, product.MinStock };
            }

            // Verify via lookup
            if (skuIndex[product.SKU].CurrentStock != previousStock + adjustment)
                lostUpdates++;
        }

        // Simulate batch persistence: serialize all movements
        var serialized = System.Text.Json.JsonSerializer.Serialize(movementLog);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<StockMovement>>(serialized);

        // Verify integrity
        var allAt90 = products.All(p => p.CurrentStock == 90);

        sw.Stop();

        // Output
        var elapsed = sw.ElapsedMilliseconds;
        _output.WriteLine($"[P-01 S6] StockUpdate 500 burst: {elapsed}ms");
        _output.WriteLine($"  Movements logged: {movementLog.Count}");
        _output.WriteLine($"  Lost updates: {lostUpdates}");
        _output.WriteLine($"  All at expected stock (90): {allAt90}");
        _output.WriteLine($"  Serialized size: {serialized.Length} bytes");
        _output.WriteLine($"  Target: <5000ms | Status: {(elapsed < 5000 ? "PASS" : "FAIL")}");

        // Assert
        movementLog.Should().HaveCount(500);
        lostUpdates.Should().Be(0, "zero stock updates should be lost during burst");
        allAt90.Should().BeTrue("each product stock should be 100 - 10 = 90");
        deserialized.Should().HaveCount(500);
        elapsed.Should().BeLessThan(5000,
            "500 stock updates burst should complete under 5000ms");
    }

    // ══════════════════════════════════════════════════════════════════
    // 7. MemoryStability_5min — allocate/release loop, GC <200MB
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void MemoryStability_5min_Compressed_ShouldStayUnder200MB()
    {
        // Compressed simulation: 300 iterations (~30s) instead of 5 full minutes.
        // Each iteration allocates ~500KB, processes, and releases.
        const int iterations = 300;
        const long maxMemoryBytes = 200L * 1024 * 1024; // 200 MB

        // Force baseline GC
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        var baselineMemory = GC.GetTotalMemory(true);

        var memorySnapshots = new List<(int Iteration, long MemoryMB)>();
        var sw = Stopwatch.StartNew();

        for (int iter = 0; iter < iterations; iter++)
        {
            // Allocate: create ~500KB of objects
            var batch = new List<ProductDto>(500);
            for (int i = 0; i < 500; i++)
            {
                batch.Add(new ProductDto
                {
                    Id = Guid.NewGuid(),
                    Name = $"MemTest-{iter}-{i}-{Guid.NewGuid():N}",
                    SKU = $"MEM-{iter:D4}-{i:D4}",
                    Barcode = $"8690{iter:D4}{i:D4}",
                    Stock = 100 + (i % 50),
                    PurchasePrice = 10.50m + (i % 200),
                    SalePrice = 25.99m + (i % 300),
                    TaxRate = 0.18m,
                    CategoryName = $"Cat-{i % 20}",
                    IsActive = true
                });
            }

            // Process: do meaningful work on the batch
            var grouped = batch
                .GroupBy(p => p.CategoryName)
                .Select(g => new { Cat = g.Key, Avg = g.Average(p => p.SalePrice), Count = g.Count() })
                .ToList();

            var sorted = batch.OrderBy(p => p.SalePrice).ThenBy(p => p.Name).ToList();
            var serialized = System.Text.Json.JsonSerializer.Serialize(sorted.Take(50));
            _ = System.Text.Json.JsonSerializer.Deserialize<List<ProductDto>>(serialized);

            // Release: let batch go out of scope (GC collectable)
            batch.Clear();

            // Snapshot every 30 iterations
            if (iter % 30 == 0 || iter == iterations - 1)
            {
                var currentMemory = GC.GetTotalMemory(false);
                memorySnapshots.Add((iter, currentMemory / (1024 * 1024)));
            }
        }

        sw.Stop();

        // Final GC and measurement
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.WaitForPendingFinalizers();
        var finalMemory = GC.GetTotalMemory(true);

        var growthMB = (finalMemory - baselineMemory) / (1024.0 * 1024.0);

        // Detect monotonic increase (leak indicator)
        var memValues = memorySnapshots.Select(s => s.MemoryMB).ToList();
        var isMonotonic = true;
        for (int i = 1; i < memValues.Count; i++)
        {
            if (memValues[i] <= memValues[i - 1])
            {
                isMonotonic = false;
                break;
            }
        }

        // Output
        _output.WriteLine($"[P-01 S7] MemoryStability {iterations} iterations: {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"  Baseline: {baselineMemory / (1024 * 1024)}MB | Final: {finalMemory / (1024 * 1024)}MB");
        _output.WriteLine($"  Growth: {growthMB:F1}MB | Monotonic: {(isMonotonic ? "YES (suspect)" : "NO (healthy)")}");
        _output.WriteLine("  Snapshots:");
        foreach (var snap in memorySnapshots)
            _output.WriteLine($"    Iter {snap.Iteration,4}: {snap.MemoryMB}MB");
        _output.WriteLine($"  Target: <200MB | Status: {(finalMemory < maxMemoryBytes ? "PASS" : "FAIL")}");

        // Assert
        finalMemory.Should().BeLessThan(maxMemoryBytes,
            "GC-managed memory should stay under 200MB during sustained allocate/release cycles");

        if (memValues.Count >= 4)
        {
            isMonotonic.Should().BeFalse(
                "memory should not grow monotonically — potential leak detected");
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // Internal DTOs for simulation
    // ══════════════════════════════════════════════════════════════════

    private sealed class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public int Stock { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal TaxRate { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    private sealed class OrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
        public string Platform { get; set; } = string.Empty;
    }

    private sealed class OrderItemDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
    }

    private sealed class StockItem
    {
        public Guid Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int MinStock { get; set; }
        public int MaxStock { get; set; }
        public List<StockMovement> Movements { get; set; } = new();
    }

    private sealed class StockMovement
    {
        public Guid Id { get; set; }
        public string SKU { get; set; } = string.Empty;
        public int PreviousStock { get; set; }
        public int NewStock { get; set; }
        public int Quantity { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    private static long ComputeSimpleHash(string input)
    {
        long hash = 17;
        foreach (char c in input)
            hash = hash * 31 + c;
        return hash;
    }
}
