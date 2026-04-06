using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

[Trait("Category", "Unit")]
public class TrendyolSettlementParserTests
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly TrendyolSettlementParser _sut;
    private readonly Mock<ILogger<TrendyolSettlementParser>> _loggerMock = new();

    public TrendyolSettlementParserTests()
    {
        _sut = new TrendyolSettlementParser(_loggerMock.Object);
    }

    [Fact]
    public void Platform_ShouldBeTrendyol()
    {
        _sut.Platform.Should().Be("Trendyol");
    }

    [Fact]
    public async Task ParseAsync_ValidJson_ShouldReturnBatch()
    {
        var json = """
        {
            "totalElements": 2,
            "totalPages": 1,
            "page": 0,
            "size": 100,
            "content": [
                {
                    "orderNumber": "ORD-001",
                    "grossSalesAmount": 1000.00,
                    "commissionAmount": 150.00,
                    "serviceFee": 10.00,
                    "cargoDeduction": 30.00,
                    "refundDeduction": 0.00,
                    "netAmount": 810.00,
                    "transactionDate": "2026-03-10",
                    "category": "Elektronik",
                    "commissionRate": 0.15
                },
                {
                    "orderNumber": "ORD-002",
                    "grossSalesAmount": 500.00,
                    "commissionAmount": 75.00,
                    "serviceFee": 5.00,
                    "cargoDeduction": 15.00,
                    "refundDeduction": 0.00,
                    "netAmount": 405.00,
                    "transactionDate": "2026-03-12",
                    "category": "Giyim",
                    "commissionRate": 0.15
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");

        batch.Should().NotBeNull();
        batch.Platform.Should().Be("Trendyol");
        batch.TotalGross.Should().Be(1500m);
        batch.TotalCommission.Should().Be(225m);
        batch.TotalNet.Should().Be(1215m);
    }

    [Fact]
    public async Task ParseAsync_EmptyContent_ShouldReturnEmptyBatch()
    {
        var json = """
        {
            "totalElements": 0,
            "totalPages": 0,
            "page": 0,
            "size": 100,
            "content": []
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");

        batch.Should().NotBeNull();
        batch.TotalGross.Should().Be(0m);
        batch.TotalCommission.Should().Be(0m);
        batch.TotalNet.Should().Be(0m);
    }

    [Fact]
    public async Task ParseAsync_NullResponse_ShouldReturnEmptyBatch()
    {
        var json = "null";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");

        batch.Should().NotBeNull();
        batch.TotalGross.Should().Be(0m);
    }

    [Fact]
    public async Task ParseAsync_ShouldSetPeriodDates()
    {
        var json = """
        {
            "totalElements": 1,
            "content": [
                {
                    "orderNumber": "ORD-001",
                    "grossSalesAmount": 100,
                    "commissionAmount": 15,
                    "serviceFee": 0,
                    "cargoDeduction": 0,
                    "refundDeduction": 0,
                    "netAmount": 85,
                    "transactionDate": "2026-03-10",
                    "commissionRate": 0.15
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");

        batch.PeriodStart.Should().BeOnOrBefore(batch.PeriodEnd);
    }

    [Fact]
    public async Task ParseLinesAsync_ShouldReturnLines()
    {
        var json = """
        {
            "totalElements": 1,
            "content": [
                {
                    "orderNumber": "ORD-001",
                    "grossSalesAmount": 1000,
                    "commissionAmount": 150,
                    "serviceFee": 10,
                    "cargoDeduction": 30,
                    "refundDeduction": 0,
                    "netAmount": 810,
                    "transactionDate": "2026-03-10",
                    "commissionRate": 0.15
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");

        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(1);
        lines[0].GrossAmount.Should().Be(1000m);
        lines[0].CommissionAmount.Should().Be(150m);
        lines[0].NetAmount.Should().Be(810m);
        lines[0].OrderId.Should().Be("ORD-001");
    }

    [Fact]
    public async Task ParseLinesAsync_WithoutPriorParse_ShouldReturnEmpty()
    {
        var parser = new TrendyolSettlementParser(_loggerMock.Object);
        var batch = MesTech.Domain.Accounting.Entities.SettlementBatch.Create(
            Guid.Empty, "Trendyol",
            DateTime.UtcNow, DateTime.UtcNow,
            0m, 0m, 0m);

        var lines = await parser.ParseLinesAsync(batch);

        lines.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_WithNullStream_ShouldThrow()
    {
        var act = async () => await _sut.ParseAsync(TestTenantId, null!, "json");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseLinesAsync_WithNullBatch_ShouldThrow()
    {
        var act = async () => await _sut.ParseLinesAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseAsync_CommissionCalculation_PerLine()
    {
        var json = """
        {
            "content": [
                {
                    "orderNumber": "ORD-001",
                    "grossSalesAmount": 2000,
                    "commissionAmount": 300,
                    "serviceFee": 20,
                    "cargoDeduction": 40,
                    "refundDeduction": 0,
                    "netAmount": 1640,
                    "transactionDate": "2026-03-15",
                    "commissionRate": 0.15,
                    "category": "Elektronik"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(1);
        lines[0].CommissionAmount.Should().Be(300m);
        lines[0].ServiceFee.Should().Be(20m);
        lines[0].CargoDeduction.Should().Be(40m);
    }

    [Fact]
    public async Task ParseAsync_MultipleItems_ShouldSumTotals()
    {
        var json = """
        {
            "content": [
                { "orderNumber": "O1", "grossSalesAmount": 100, "commissionAmount": 15, "serviceFee": 0, "cargoDeduction": 0, "refundDeduction": 0, "netAmount": 85, "transactionDate": "2026-03-10", "commissionRate": 0.15 },
                { "orderNumber": "O2", "grossSalesAmount": 200, "commissionAmount": 30, "serviceFee": 0, "cargoDeduction": 0, "refundDeduction": 0, "netAmount": 170, "transactionDate": "2026-03-10", "commissionRate": 0.15 },
                { "orderNumber": "O3", "grossSalesAmount": 300, "commissionAmount": 45, "serviceFee": 0, "cargoDeduction": 0, "refundDeduction": 0, "netAmount": 255, "transactionDate": "2026-03-10", "commissionRate": 0.15 }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");

        batch.TotalGross.Should().Be(600m);
        batch.TotalCommission.Should().Be(90m);
        batch.TotalNet.Should().Be(510m);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var act = () => new TrendyolSettlementParser(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ══════════════════════════════════════
    // KDV (VAT) Tests — KÇ-12 TY-TST-004
    // ══════════════════════════════════════

    [Fact]
    public async Task ParseLinesAsync_VatAmount_ShouldBeParsedFromJson()
    {
        var json = """
        {
            "content": [
                {
                    "orderNumber": "ORD-KDV-001",
                    "grossSalesAmount": 1000,
                    "commissionAmount": 150,
                    "serviceFee": 10,
                    "cargoDeduction": 30,
                    "refundDeduction": 0,
                    "netAmount": 810,
                    "transactionDate": "2026-03-15",
                    "commissionRate": 0.15,
                    "vatAmount": 152.54
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(1);
        lines[0].VatAmount.Should().Be(152.54m,
            "vatAmount from Trendyol Finance API should map to SettlementLine.VatAmount");
    }

    [Fact]
    public async Task ParseLinesAsync_VatAmount_ZeroWhenMissing()
    {
        // vatAmount alanı JSON'da yoksa → 0 olmalı
        var json = """
        {
            "content": [
                {
                    "orderNumber": "ORD-NOVAT",
                    "grossSalesAmount": 500,
                    "commissionAmount": 75,
                    "serviceFee": 5,
                    "cargoDeduction": 15,
                    "refundDeduction": 0,
                    "netAmount": 405,
                    "transactionDate": "2026-03-12",
                    "commissionRate": 0.15
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(1);
        lines[0].VatAmount.Should().Be(0m,
            "missing vatAmount field should default to 0");
    }

    [Fact]
    public async Task ParseLinesAsync_MultipleItems_DifferentVatRates()
    {
        // Farklı KDV oranları: %10 ve %18
        var json = """
        {
            "content": [
                {
                    "orderNumber": "ORD-KDV10",
                    "grossSalesAmount": 1100,
                    "commissionAmount": 165,
                    "serviceFee": 10,
                    "cargoDeduction": 0,
                    "refundDeduction": 0,
                    "netAmount": 925,
                    "transactionDate": "2026-03-20",
                    "commissionRate": 0.15,
                    "vatAmount": 100.00
                },
                {
                    "orderNumber": "ORD-KDV18",
                    "grossSalesAmount": 590,
                    "commissionAmount": 88.50,
                    "serviceFee": 5,
                    "cargoDeduction": 0,
                    "refundDeduction": 0,
                    "netAmount": 496.50,
                    "transactionDate": "2026-03-20",
                    "commissionRate": 0.15,
                    "vatAmount": 90.00
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(2);
        lines[0].VatAmount.Should().Be(100.00m);
        lines[1].VatAmount.Should().Be(90.00m);

        // GL yevmiye doğrulaması: toplam KDV
        lines.Sum(l => l.VatAmount).Should().Be(190.00m,
            "total VAT across both lines for GL journal entry");
    }

    [Fact]
    public async Task ParseLinesAsync_VatAmount_WithTurkishDecimalFormat()
    {
        // Türkçe ondalık formatı: 152,54 (virgül ile)
        var json = """
        {
            "content": [
                {
                    "orderNumber": "ORD-TR-KDV",
                    "grossSalesAmount": 1000,
                    "commissionAmount": 150,
                    "serviceFee": 0,
                    "cargoDeduction": 0,
                    "refundDeduction": 0,
                    "netAmount": 850,
                    "transactionDate": "2026-03-25",
                    "commissionRate": 0.15,
                    "vatAmount": 152.54
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines[0].VatAmount.Should().Be(152.54m,
            "parser should handle decimal format correctly for vatAmount");
        lines[0].GrossAmount.Should().Be(1000m);
    }
}
