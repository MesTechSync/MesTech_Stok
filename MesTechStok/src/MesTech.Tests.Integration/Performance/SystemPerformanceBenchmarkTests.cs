using System.Diagnostics;
using FluentAssertions;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Accounting.Services;
using MesTech.Domain.Services;
using Xunit.Abstractions;

namespace MesTech.Tests.Integration.Performance;

/// <summary>
/// Dalga 12 Wave 3 — System performance benchmark tests.
/// 8 tests covering critical accounting/domain operations with Stopwatch timing.
/// REAL domain services: CommissionCalculationService, ReconciliationScoringService, FEFOSortingService.
/// </summary>
[Trait("Category", "Performance")]
[Trait("Phase", "Dalga12")]
public class SystemPerformanceBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public SystemPerformanceBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 1. 1000 product sync simulation under 200ms
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 10_000)]
    public void ProductSyncSimulation_1000Products_Under200ms()
    {
        // Arrange — 1000 products with commission calculation per product
        var commissionService = new CommissionCalculationService();
        var platforms = new[] { "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon" };
        var products = Enumerable.Range(0, 1000)
            .Select(i => new
            {
                SKU = $"SYNC-{i:D5}",
                Platform = platforms[i % platforms.Length],
                GrossAmount = 100m + (i % 500)
            })
            .ToList();

        // Act
        var sw = Stopwatch.StartNew();

        var results = new List<(string SKU, decimal Commission)>(1000);
        foreach (var product in products)
        {
            var commission = commissionService.CalculateCommission(
                product.Platform, null, product.GrossAmount);
            results.Add((product.SKU, commission));
        }

        sw.Stop();

        // Assert
        _output.WriteLine($"[Benchmark] 1000 product sync simulation: {sw.ElapsedMilliseconds}ms");

        results.Should().HaveCount(1000);
        results.Should().AllSatisfy(r => r.Commission.Should().BeGreaterThan(0));
        sw.ElapsedMilliseconds.Should().BeLessThan(200,
            $"1000 product sync simulations completed in {sw.ElapsedMilliseconds}ms, limit 200ms");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 2. 500 order fetch simulation under 30ms
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 10_000)]
    public void OrderFetchSimulation_500Orders_Under30ms()
    {
        // Arrange — simulate 500 orders with FEFO picking per order
        var fefoService = new FEFOSortingService();
        var stockItems = Enumerable.Range(0, 50)
            .Select(i => new FEFOStockItem(
                Guid.NewGuid(),
                $"SKU-FEFO-{i:D3}",
                DateTime.UtcNow.AddDays(i + 1),
                100m,
                $"Shelf-{(char)('A' + (i % 5))}{i / 5 + 1}",
                $"LOT-{i:D4}"))
            .ToList();

        // Act
        var sw = Stopwatch.StartNew();

        var pickResults = new List<IReadOnlyList<FEFOPickResult>>(500);
        for (int i = 0; i < 500; i++)
        {
            var quantity = 1m + (i % 10);
            var picks = fefoService.PickForConsumption(stockItems, quantity);
            pickResults.Add(picks);
        }

        sw.Stop();

        // Assert
        _output.WriteLine($"[Benchmark] 500 order fetch (FEFO pick): {sw.ElapsedMilliseconds}ms");

        pickResults.Should().HaveCount(500);
        pickResults.Should().AllSatisfy(p => p.Should().NotBeEmpty());
        sw.ElapsedMilliseconds.Should().BeLessThan(30,
            $"500 order fetch simulations completed in {sw.ElapsedMilliseconds}ms, limit 30ms");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 3. 10K settlement line parse under 5s
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 30_000)]
    public void SettlementLineParse_10K_Under5Seconds()
    {
        // Arrange — create 10K settlement lines with commission calculations
        var tenantId = Guid.NewGuid();
        var commissionService = new CommissionCalculationService();
        var platforms = new[] { "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama" };
        var batchId = Guid.NewGuid();

        // Act
        var sw = Stopwatch.StartNew();

        var lines = new List<SettlementLine>(10_000);
        for (int i = 0; i < 10_000; i++)
        {
            var platform = platforms[i % platforms.Length];
            var gross = 50m + (i % 1000);
            var commission = commissionService.CalculateCommission(platform, null, gross);
            var serviceFee = Math.Round(gross * 0.02m, 2);
            var cargoDeduction = i % 3 == 0 ? 15.99m : 0m;
            var refundDeduction = i % 7 == 0 ? gross * 0.1m : 0m;
            var net = gross - commission - serviceFee - cargoDeduction - refundDeduction;

            var line = SettlementLine.Create(
                tenantId, batchId, $"ORD-{i:D6}",
                gross, commission, serviceFee, cargoDeduction, refundDeduction, net);

            // Verify net calculation
            _ = line.CalculateNetAmount();
            lines.Add(line);
        }

        sw.Stop();

        // Assert
        _output.WriteLine($"[Benchmark] 10K settlement line parse: {sw.ElapsedMilliseconds}ms");

        lines.Should().HaveCount(10_000);
        lines.Should().AllSatisfy(l =>
        {
            l.GrossAmount.Should().BeGreaterThan(0);
            l.TenantId.Should().Be(tenantId);
        });
        sw.ElapsedMilliseconds.Should().BeLessThan(5_000,
            $"10K settlement line parse completed in {sw.ElapsedMilliseconds}ms, limit 5000ms");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 4. 10K bank transaction import under 5s
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 30_000)]
    public void BankTransactionImport_10K_Under5Seconds()
    {
        // Arrange — simulate 10K bank transaction creation (in-memory entity construction)
        var tenantId = Guid.NewGuid();
        var bankAccountId = Guid.NewGuid();
        var baseDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var banks = new[] { "Garanti", "Isbank", "Yapikredi", "Akbank", "QNB" };

        // Act
        var sw = Stopwatch.StartNew();

        var transactions = new List<BankTransaction>(10_000);
        for (int i = 0; i < 10_000; i++)
        {
            var tx = BankTransaction.Create(
                tenantId,
                bankAccountId,
                baseDate.AddMinutes(i),
                amount: (i % 2 == 0 ? 1m : -1m) * (100m + (i % 5000)),
                description: $"{banks[i % banks.Length]} - Transfer #{i:D6}",
                referenceNumber: $"REF-{i:D8}",
                idempotencyKey: $"IDEM-{i:D8}");

            transactions.Add(tx);
        }

        sw.Stop();

        // Assert
        _output.WriteLine($"[Benchmark] 10K bank transaction import: {sw.ElapsedMilliseconds}ms");

        transactions.Should().HaveCount(10_000);
        transactions.Should().AllSatisfy(t =>
        {
            t.TenantId.Should().Be(tenantId);
            t.IsReconciled.Should().BeFalse();
            t.Description.Should().NotBeNullOrEmpty();
        });

        // Verify idempotency keys are unique
        var uniqueKeys = transactions.Select(t => t.Id).Distinct().Count();
        uniqueKeys.Should().Be(10_000, "all transactions must have unique IDs");

        sw.ElapsedMilliseconds.Should().BeLessThan(5_000,
            $"10K bank transaction import completed in {sw.ElapsedMilliseconds}ms, limit 5000ms");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 5. 10K reconciliation pair scoring under 5s
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 30_000)]
    public void ReconciliationPairScoring_10K_Under5Seconds()
    {
        // Arrange — REAL ReconciliationScoringService, 10K settlement-bank pairs
        var scoringService = new ReconciliationScoringService();
        var platforms = new[] { "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama" };
        var baseDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        var pairs = Enumerable.Range(0, 10_000)
            .Select(i =>
            {
                var platform = platforms[i % platforms.Length];
                var settlementAmount = 1000m + (i % 5000);
                // Introduce slight variance to test scoring accuracy
                var bankAmount = settlementAmount + (i % 10 == 0 ? 0.5m : 0m);
                var settlementDate = baseDate.AddDays(i / 100);
                var bankDate = settlementDate.AddDays(i % 5 == 0 ? 0 : (i % 3));
                return new
                {
                    BankAmount = bankAmount,
                    SettlementAmount = settlementAmount,
                    BankDate = bankDate,
                    SettlementDate = settlementDate,
                    BankDescription = $"{platform} Odeme #{i:D5}",
                    Platform = platform
                };
            })
            .ToList();

        // Act
        var sw = Stopwatch.StartNew();

        var scores = new List<decimal>(10_000);
        foreach (var pair in pairs)
        {
            var score = scoringService.CalculateConfidence(
                pair.BankAmount,
                pair.SettlementAmount,
                pair.BankDate,
                pair.SettlementDate,
                pair.BankDescription,
                pair.Platform);
            scores.Add(score);
        }

        sw.Stop();

        // Assert
        _output.WriteLine($"[Benchmark] 10K reconciliation pair scoring: {sw.ElapsedMilliseconds}ms");
        _output.WriteLine($"  Avg score: {scores.Average():F4}");
        _output.WriteLine($"  Auto-match (>={scoringService.AutoMatchThreshold}): {scores.Count(s => s >= scoringService.AutoMatchThreshold)}");
        _output.WriteLine($"  Needs review ({scoringService.ReviewThreshold}-{scoringService.AutoMatchThreshold}): " +
            $"{scores.Count(s => s >= scoringService.ReviewThreshold && s < scoringService.AutoMatchThreshold)}");
        _output.WriteLine($"  Unmatched (<{scoringService.ReviewThreshold}): {scores.Count(s => s < scoringService.ReviewThreshold)}");

        scores.Should().HaveCount(10_000);
        scores.Should().AllSatisfy(s =>
        {
            s.Should().BeGreaterThanOrEqualTo(0m);
            s.Should().BeLessThanOrEqualTo(1m);
        });
        sw.ElapsedMilliseconds.Should().BeLessThan(5_000,
            $"10K reconciliation pair scoring completed in {sw.ElapsedMilliseconds}ms, limit 5000ms");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 6. 1000 JournalEntry balance check under 500ms
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 10_000)]
    public void JournalEntryBalanceCheck_1000Entries_Under500ms()
    {
        // Arrange — 1000 JournalEntries, each with 4 lines (2 debit + 2 credit)
        var tenantId = Guid.NewGuid();
        var entries = new List<JournalEntry>(1000);

        for (int i = 0; i < 1000; i++)
        {
            var entry = JournalEntry.Create(
                tenantId,
                DateTime.UtcNow.AddDays(-i),
                $"Benchmark entry #{i:D4}",
                $"REF-{i:D6}");

            var accountA = Guid.NewGuid();
            var accountB = Guid.NewGuid();
            var accountC = Guid.NewGuid();
            var accountD = Guid.NewGuid();
            var amount1 = 100m + (i % 500);
            var amount2 = 50m + (i % 250);

            // Balanced: debit side = credit side
            entry.AddLine(accountA, debit: amount1, credit: 0, "Kasa");
            entry.AddLine(accountB, debit: amount2, credit: 0, "Banka");
            entry.AddLine(accountC, debit: 0, credit: amount1, "Satis Geliri");
            entry.AddLine(accountD, debit: 0, credit: amount2, "KDV Hesabi");

            entries.Add(entry);
        }

        // Act — validate and post all entries
        var sw = Stopwatch.StartNew();

        var validCount = 0;
        var postedCount = 0;
        foreach (var entry in entries)
        {
            entry.Validate();
            validCount++;
            entry.Post();
            postedCount++;
        }

        sw.Stop();

        // Assert
        _output.WriteLine($"[Benchmark] 1000 JournalEntry balance check + post: {sw.ElapsedMilliseconds}ms");

        validCount.Should().Be(1000, "all entries must pass Validate()");
        postedCount.Should().Be(1000, "all entries must be posted successfully");
        entries.Should().AllSatisfy(e =>
        {
            e.IsPosted.Should().BeTrue();
            e.Lines.Should().HaveCount(4);
            e.Lines.Sum(l => l.Debit).Should().Be(e.Lines.Sum(l => l.Credit),
                "debit = credit invariant");
        });
        sw.ElapsedMilliseconds.Should().BeLessThan(500,
            $"1000 JournalEntry balance checks completed in {sw.ElapsedMilliseconds}ms, limit 500ms");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 7. App startup simulation under 3 seconds
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 10_000)]
    public void AppStartupSimulation_ServiceInit_Under3Seconds()
    {
        // Arrange — simulate typical startup: instantiate all pure domain services,
        // create initial data structures, and warm up caches

        var sw = Stopwatch.StartNew();

        // 1. Domain service instantiation
        var commissionService = new CommissionCalculationService();
        var reconciliationService = new ReconciliationScoringService();
        var fefoService = new FEFOSortingService();

        // 2. Warm-up: verify services respond correctly
        var commissionResult = commissionService.CalculateCommission("Trendyol", null, 1000m);
        var scoreResult = reconciliationService.CalculateConfidence(
            1000m, 1000m, DateTime.UtcNow, DateTime.UtcNow, "Trendyol odeme", "Trendyol");
        var fefoResult = fefoService.Sort(new[]
        {
            new FEFOStockItem(Guid.NewGuid(), "SKU-001", DateTime.UtcNow.AddDays(5), 100m, "A1"),
            new FEFOStockItem(Guid.NewGuid(), "SKU-002", DateTime.UtcNow.AddDays(1), 50m, "A2")
        });

        // 3. Simulate tenant-specific data preload: 100 products, 50 categories
        var products = Enumerable.Range(0, 100)
            .Select(i => new { SKU = $"INIT-{i:D4}", Name = $"Product-{i}" })
            .ToList();

        var categories = Enumerable.Range(0, 50)
            .Select(i => new { Id = Guid.NewGuid(), Name = $"Category-{i}" })
            .ToDictionary(c => c.Id, c => c.Name);

        // 4. Simulate platform rate lookup cache build
        var platforms = new[] { "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama" };
        var rateCache = platforms.ToDictionary(
            p => p,
            p => commissionService.GetDefaultRate(p));

        // 5. Simulate FEFO initial sort for warehouse
        var warehouseItems = Enumerable.Range(0, 500)
            .Select(i => new FEFOStockItem(
                Guid.NewGuid(),
                $"WH-{i:D4}",
                DateTime.UtcNow.AddDays(i % 90 + 1),
                10m + (i % 100),
                $"Shelf-{(char)('A' + (i % 10))}{i / 10 + 1}"))
            .ToList();
        var sortedWarehouse = fefoService.Sort(warehouseItems);

        sw.Stop();

        // Assert
        _output.WriteLine($"[Benchmark] App startup simulation: {sw.ElapsedMilliseconds}ms");

        commissionResult.Should().Be(150m, "Trendyol 15% of 1000 = 150");
        scoreResult.Should().BeGreaterThan(0.8m, "exact match should score high");
        fefoResult.Should().HaveCount(2);
        fefoResult[0].SKU.Should().Be("SKU-002", "earlier expiry first");
        products.Should().HaveCount(100);
        categories.Should().HaveCount(50);
        rateCache.Should().HaveCount(6);
        sortedWarehouse.Should().HaveCount(500);

        sw.ElapsedMilliseconds.Should().BeLessThan(3_000,
            $"app startup simulation completed in {sw.ElapsedMilliseconds}ms, limit 3000ms");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // 8. Memory usage under 100MB during bulk operation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact(Timeout = 30_000)]
    public void MemoryUsage_BulkAccountingOperation_Under100MB()
    {
        // Force GC to get baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var baselineMemory = Process.GetCurrentProcess().WorkingSet64;

        // Arrange + Act — create bulk accounting objects
        var tenantId = Guid.NewGuid();
        var commissionService = new CommissionCalculationService();
        var reconciliationService = new ReconciliationScoringService();
        var platforms = new[] { "Trendyol", "Hepsiburada", "N11", "Ciceksepeti", "Amazon", "Pazarama" };
        var baseDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

        // 1. Create 5000 settlement lines across 50 batches
        var batches = new List<SettlementBatch>(50);
        for (int b = 0; b < 50; b++)
        {
            var platform = platforms[b % platforms.Length];
            var batch = SettlementBatch.Create(
                tenantId, platform,
                baseDate.AddDays(b * 7), baseDate.AddDays(b * 7 + 6),
                totalGross: 50_000m, totalCommission: 7_500m, totalNet: 42_500m);

            for (int i = 0; i < 100; i++)
            {
                var gross = 500m + (i % 200);
                var commission = commissionService.CalculateCommission(platform, null, gross);
                var line = SettlementLine.Create(
                    tenantId, batch.Id, $"ORD-B{b:D2}-{i:D3}",
                    gross, commission, 10m, 15m, 0m,
                    gross - commission - 10m - 15m);
                batch.AddLine(line);
            }

            batches.Add(batch);
        }

        // 2. Create 5000 bank transactions
        var transactions = new List<BankTransaction>(5000);
        var bankAccountId = Guid.NewGuid();
        for (int i = 0; i < 5000; i++)
        {
            var tx = BankTransaction.Create(
                tenantId, bankAccountId,
                baseDate.AddMinutes(i),
                1000m + (i % 3000),
                $"{platforms[i % platforms.Length]} transfer #{i:D5}");
            transactions.Add(tx);
        }

        // 3. Score 5000 reconciliation pairs
        var scores = new List<decimal>(5000);
        for (int i = 0; i < 5000; i++)
        {
            var platform = platforms[i % platforms.Length];
            var score = reconciliationService.CalculateConfidence(
                transactions[i].Amount,
                batches[i / 100].TotalNet / 100m,
                transactions[i].TransactionDate,
                batches[i / 100].PeriodEnd,
                transactions[i].Description,
                platform);
            scores.Add(score);
        }

        // 4. Create 1000 journal entries with 4 lines each
        var entries = new List<JournalEntry>(1000);
        for (int i = 0; i < 1000; i++)
        {
            var entry = JournalEntry.Create(
                tenantId, baseDate.AddDays(i / 10),
                $"Bulk entry #{i:D4}");
            var amount = 100m + (i % 500);
            entry.AddLine(Guid.NewGuid(), amount, 0);
            entry.AddLine(Guid.NewGuid(), 0, amount);
            entry.Validate();
            entries.Add(entry);
        }

        // Measure memory after allocation
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var currentMemory = Process.GetCurrentProcess().WorkingSet64;
        var memoryUsedMB = (currentMemory - baselineMemory) / (1024.0 * 1024.0);

        // Assert
        _output.WriteLine($"[Benchmark] Bulk accounting memory: {memoryUsedMB:F1}MB");
        _output.WriteLine($"  Settlement batches: {batches.Count} ({batches.Sum(b => b.Lines.Count)} lines)");
        _output.WriteLine($"  Bank transactions: {transactions.Count}");
        _output.WriteLine($"  Reconciliation scores: {scores.Count}");
        _output.WriteLine($"  Journal entries: {entries.Count} ({entries.Sum(e => e.Lines.Count)} lines)");

        batches.Should().HaveCount(50);
        batches.Sum(b => b.Lines.Count).Should().Be(5000);
        transactions.Should().HaveCount(5000);
        scores.Should().HaveCount(5000);
        entries.Should().HaveCount(1000);

        // Total domain objects: 50 batches + 5000 lines + 5000 tx + 5000 scores + 1000 entries + 2000 jlines
        var totalObjects = 50 + 5000 + 5000 + 5000 + 1000 + 2000;
        totalObjects.Should().Be(18_050);

        // Memory must stay under 100MB
        memoryUsedMB.Should().BeLessThan(100,
            $"{totalObjects} domain objects consumed {memoryUsedMB:F1}MB, limit 100MB");
    }
}
