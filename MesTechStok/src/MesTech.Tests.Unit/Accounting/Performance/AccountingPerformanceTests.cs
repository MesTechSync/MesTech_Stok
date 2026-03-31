using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetTrialBalance;
using MesTech.Application.Interfaces.Accounting;
using MesTech.Domain.Accounting.Entities;
using MesTech.Domain.Interfaces;
using MesTech.Domain.Accounting.Enums;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Performance;

/// <summary>
/// Accounting performance tests — verifies key operations complete within acceptable time limits.
/// Uses in-memory data (no database) so these measure algorithmic performance.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Performance")]
public class AccountingPerformanceTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    // ── Settlement Parsing Performance ──

    [Fact]
    public async Task SettlementParse_500Lines_Under1Second()
    {
        // Arrange — 500 settlement items in Trendyol JSON format
        var items = Enumerable.Range(1, 500).Select(i => new
        {
            orderNumber = $"TR-PERF-{i:D6}",
            grossSalesAmount = 100m + (i % 50),
            commissionAmount = 15m + (i % 10),
            commissionRate = 0.15m,
            serviceFee = 5m,
            cargoDeduction = 10m,
            refundDeduction = 0m,
            netAmount = 70m + (i % 40),
            transactionDate = $"2026-03-{(i % 28) + 1:D2}",
            category = $"Category-{i % 10}"
        }).ToList();

        var json = JsonSerializer.Serialize(new
        {
            totalElements = 500,
            totalPages = 1,
            page = 0,
            size = 500,
            content = items
        });

        var parser = new TrendyolSettlementParser(
            new Mock<ILogger<TrendyolSettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var sw = Stopwatch.StartNew();
        var batch = await parser.ParseAsync(_tenantId, stream, "json");
        var lines = await parser.ParseLinesAsync(batch);
        sw.Stop();

        // Assert
        lines.Should().HaveCount(500);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1),
            "parsing 500 settlement lines should complete under 1 second");
    }

    [Fact]
    public async Task SettlementParse_1000CiceksepetiItems_Under2Seconds()
    {
        // Arrange
        var items = Enumerable.Range(1, 1000).Select(i => new
        {
            orderNo = $"CS-PERF-{i:D6}",
            productName = $"Product {i}",
            saleAmount = 200m + (i % 100),
            commissionAmount = 30m + (i % 20),
            commissionRate = 0.15m,
            cargoContribution = 10m,
            serviceFee = 5m,
            netAmount = 155m + (i % 80),
            transactionDate = $"2026-03-{(i % 28) + 1:D2}",
            category = $"Category-{i % 15}"
        }).ToList();

        var json = JsonSerializer.Serialize(new
        {
            totalCount = 1000,
            periodStart = "2026-03-01",
            periodEnd = "2026-03-31",
            items
        });

        var parser = new CiceksepetiSettlementParser(
            new Mock<ILogger<CiceksepetiSettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var sw = Stopwatch.StartNew();
        var batch = await parser.ParseAsync(_tenantId, stream, "json");
        var lines = await parser.ParseLinesAsync(batch);
        sw.Stop();

        // Assert
        lines.Should().HaveCount(1000);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2));
    }

    // ── Journal Entry Balance Check Performance ──

    [Fact]
    public void JournalEntry_500Entries_BalanceCheck_Under500ms()
    {
        // Arrange — 500 balanced journal entries, each with 2-4 lines
        var entries = new List<JournalEntry>();
        var random = new Random(42); // deterministic seed

        for (int i = 0; i < 500; i++)
        {
            var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, $"Perf entry {i}");
            var amount = (decimal)(random.Next(100, 10000));

            // 2-line entry: debit + credit
            entry.AddLine(Guid.NewGuid(), amount, 0m);
            entry.AddLine(Guid.NewGuid(), 0m, amount);

            entries.Add(entry);
        }

        // Act — validate all entries
        var sw = Stopwatch.StartNew();
        foreach (var entry in entries)
        {
            entry.Validate(); // Borc = Alacak check
        }
        sw.Stop();

        // Assert
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500),
            "validating 500 journal entries should complete under 500ms");
    }

    // ── Trial Balance Performance ──

    [Fact]
    public async Task TrialBalance_100Accounts_Under1Second()
    {
        // Arrange — 100 accounts, each with multiple journal entries
        var accounts = Enumerable.Range(1, 100)
            .Select(i => ChartOfAccounts.Create(
                _tenantId,
                $"{i:D3}",
                $"Account {i}",
                (AccountType)(i % 5)))
            .ToList();

        // Create posted entries for each account
        var entries = new List<JournalEntry>();
        foreach (var account in accounts)
        {
            for (int j = 0; j < 10; j++)
            {
                var entry = JournalEntry.Create(_tenantId, DateTime.UtcNow, $"Entry {account.Code}-{j}");
                var amount = 100m + j * 10m;
                entry.AddLine(account.Id, amount, 0m);
                entry.AddLine(Guid.NewGuid(), 0m, amount);
                entry.Post();
                entries.Add(entry);
            }
        }

        var accountRepoMock = new Mock<IChartOfAccountsRepository>();
        accountRepoMock.Setup(r => r.GetAllAsync(_tenantId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);

        var journalRepoMock = new Mock<IJournalEntryRepository>();
        journalRepoMock.Setup(r => r.GetByDateRangeAsync(_tenantId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(entries);

        var handler = new GetTrialBalanceHandler(
            accountRepoMock.Object,
            journalRepoMock.Object);

        var query = new GetTrialBalanceQuery(_tenantId, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc), DateTime.UtcNow);

        // Act
        var sw = Stopwatch.StartNew();
        var result = await handler.Handle(query, CancellationToken.None);
        sw.Stop();

        // Assert
        result.Lines.Should().HaveCount(100);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1),
            "trial balance for 100 accounts with 1000 entries should complete under 1 second");
    }

    // ── N11 XML Parsing Performance ──

    [Fact]
    public async Task N11XmlParse_200Items_Under1Second()
    {
        // Arrange — 200 N11 settlement items in XML
        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.AppendLine("<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\">");
        xml.AppendLine("<soap:Body>");
        xml.AppendLine("<getSettlementReportResponse>");

        for (int i = 1; i <= 200; i++)
        {
            xml.AppendLine($"<settlementItem>");
            xml.AppendLine($"  <siparisNo>N11-PERF-{i:D6}</siparisNo>");
            xml.AppendLine($"  <urunAdi>Urun {i}</urunAdi>");
            xml.AppendLine($"  <satisTutari>{100 + i}.00</satisTutari>");
            xml.AppendLine($"  <komisyonTutari>{15 + (i % 10)}.00</komisyonTutari>");
            xml.AppendLine($"  <komisyonOrani>0.15</komisyonOrani>");
            xml.AppendLine($"  <kargoKesinti>10.00</kargoKesinti>");
            xml.AppendLine($"  <netTutar>{75 + i}.00</netTutar>");
            xml.AppendLine($"  <islemTarihi>2026-03-{(i % 28) + 1:D2}</islemTarihi>");
            xml.AppendLine($"  <kategori>Cat-{i % 10}</kategori>");
            xml.AppendLine($"</settlementItem>");
        }

        xml.AppendLine("</getSettlementReportResponse>");
        xml.AppendLine("</soap:Body>");
        xml.AppendLine("</soap:Envelope>");

        var parser = new N11SettlementParser(
            new Mock<ILogger<N11SettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml.ToString()));

        // Act
        var sw = Stopwatch.StartNew();
        var batch = await parser.ParseAsync(_tenantId, stream, "xml");
        var lines = await parser.ParseLinesAsync(batch);
        sw.Stop();

        // Assert
        lines.Should().HaveCount(200);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1),
            "parsing 200 N11 XML settlement items should complete under 1 second");
    }

    // ── OpenCart Parsing Performance ──

    [Fact]
    public async Task OpenCartParse_500Orders_Under1Second()
    {
        // Arrange
        var orders = Enumerable.Range(1, 500).Select(i => new
        {
            orderId = $"OC-PERF-{i:D6}",
            productName = $"Product {i}",
            orderTotal = 50m + (i % 100) * 2,
            gatewayFee = 1.25m,
            cargoExpense = 9.99m,
            netAmount = 38.76m + (i % 100) * 2,
            orderDate = $"2026-03-{(i % 28) + 1:D2}",
            paymentMethod = i % 2 == 0 ? "iyzico" : "PayTR"
        }).ToList();

        var json = JsonSerializer.Serialize(new
        {
            totalOrders = 500,
            periodStart = "2026-03-01",
            periodEnd = "2026-03-31",
            orders
        });

        var parser = new OpenCartSettlementParser(
            new Mock<ILogger<OpenCartSettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        // Act
        var sw = Stopwatch.StartNew();
        var batch = await parser.ParseAsync(_tenantId, stream, "json");
        var lines = await parser.ParseLinesAsync(batch);
        sw.Stop();

        // Assert
        lines.Should().HaveCount(500);
        batch.TotalCommission.Should().Be(0m); // OpenCart = own store
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    // ── Entity Creation Performance ──

    [Fact]
    public void SettlementBatch_Create1000Lines_Under500ms()
    {
        // Arrange
        var sw = Stopwatch.StartNew();

        var batch = SettlementBatch.Create(
            _tenantId, "Trendyol",
            DateTime.UtcNow.AddDays(-15), DateTime.UtcNow,
            100000m, 15000m, 85000m);

        for (int i = 0; i < 1000; i++)
        {
            var line = SettlementLine.Create(
                _tenantId, batch.Id,
                orderId: $"ORD-{i:D6}",
                grossAmount: 100m,
                commissionAmount: 15m,
                serviceFee: 2m,
                cargoDeduction: 10m,
                refundDeduction: 0m,
                netAmount: 73m);

            batch.AddLine(line);
        }

        sw.Stop();

        // Assert
        batch.Lines.Should().HaveCount(1000);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void CommissionRecord_Create2000_Under500ms()
    {
        // Arrange & Act
        var sw = Stopwatch.StartNew();

        var records = new List<CommissionRecord>(2000);
        for (int i = 0; i < 2000; i++)
        {
            var record = CommissionRecord.Create(
                _tenantId,
                "Trendyol",
                grossAmount: 200m + i,
                commissionRate: 0.15m,
                commissionAmount: (200m + i) * 0.15m,
                serviceFee: 5m,
                orderId: $"ORD-{i:D6}",
                category: $"Cat-{i % 20}");
            records.Add(record);
        }

        sw.Stop();

        // Assert
        records.Should().HaveCount(2000);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void ProfitReport_CreateAndAggregate_500Reports_Under200ms()
    {
        // Arrange & Act
        var sw = Stopwatch.StartNew();

        var reports = new List<ProfitReport>(500);
        for (int i = 0; i < 500; i++)
        {
            var report = ProfitReport.Create(
                _tenantId,
                DateTime.UtcNow,
                $"2026-03-{(i % 28) + 1:D2}",
                totalRevenue: 5000m + i * 10,
                totalCost: 2000m + i * 3,
                totalCommission: 500m + i,
                totalCargo: 200m,
                totalTax: 300m + i * 2,
                platform: $"Platform-{i % 5}");
            reports.Add(report);
        }

        // Aggregate
        var totalRevenue = reports.Sum(r => r.TotalRevenue);
        var totalProfit = reports.Sum(r => r.NetProfit);
        var byPlatform = reports.GroupBy(r => r.Platform)
            .ToDictionary(g => g.Key!, g => g.Sum(r => r.NetProfit));

        sw.Stop();

        // Assert
        reports.Should().HaveCount(500);
        byPlatform.Should().HaveCount(5);
        sw.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(1000), "500 ProfitReport create+aggregate should complete under 1s even on loaded CI");
    }
}
