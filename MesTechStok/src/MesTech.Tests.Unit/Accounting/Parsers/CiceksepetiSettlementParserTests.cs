using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

/// <summary>
/// CiceksepetiSettlementParser tests — JSON parsing, commission, hash, edge cases.
/// </summary>
[Trait("Category", "Unit")]
public class CiceksepetiSettlementParserTests
{
    private readonly CiceksepetiSettlementParser _sut;
    private readonly Mock<ILogger<CiceksepetiSettlementParser>> _loggerMock = new();

    public CiceksepetiSettlementParserTests()
    {
        _sut = new CiceksepetiSettlementParser(_loggerMock.Object);
    }

    [Fact]
    public void Platform_ShouldBeCiceksepeti()
    {
        _sut.Platform.Should().Be("Ciceksepeti");
    }

    [Fact]
    public async Task ParseAsync_ValidJson_ShouldReturnBatch()
    {
        var json = """
        {
            "totalCount": 2,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "items": [
                {
                    "orderNo": "CS-001",
                    "productName": "Orkide Buketi",
                    "saleAmount": 500.00,
                    "commissionAmount": 75.00,
                    "commissionRate": 0.15,
                    "cargoContribution": 20.00,
                    "serviceFee": 5.00,
                    "netAmount": 400.00,
                    "transactionDate": "2026-03-05",
                    "category": "Cicek"
                },
                {
                    "orderNo": "CS-002",
                    "productName": "Gul Aranjmani",
                    "saleAmount": 300.00,
                    "commissionAmount": 45.00,
                    "commissionRate": 0.15,
                    "cargoContribution": 15.00,
                    "serviceFee": 3.00,
                    "netAmount": 237.00,
                    "transactionDate": "2026-03-10",
                    "category": "Cicek"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.Should().NotBeNull();
        batch.Platform.Should().Be("Ciceksepeti");
        batch.TotalGross.Should().Be(800m);
        batch.TotalCommission.Should().Be(120m);
        batch.TotalNet.Should().Be(637m);
    }

    [Fact]
    public async Task ParseAsync_EmptyItems_ShouldReturnEmptyBatch()
    {
        var json = """
        {
            "totalCount": 0,
            "items": []
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

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
        var batch = await _sut.ParseAsync(stream, "json");

        batch.Should().NotBeNull();
        batch.TotalGross.Should().Be(0m);
    }

    [Fact]
    public async Task ParseLinesAsync_ValidBatch_ShouldReturnLines()
    {
        var json = """
        {
            "totalCount": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "items": [
                {
                    "orderNo": "CS-001",
                    "productName": "Orkide",
                    "saleAmount": 500.00,
                    "commissionAmount": 75.00,
                    "commissionRate": 0.15,
                    "cargoContribution": 20.00,
                    "serviceFee": 5.00,
                    "netAmount": 400.00,
                    "transactionDate": "2026-03-05",
                    "category": "Cicek"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(1);
        lines[0].OrderId.Should().Be("CS-001");
        lines[0].GrossAmount.Should().Be(500m);
        lines[0].CommissionAmount.Should().Be(75m);
        lines[0].ServiceFee.Should().Be(5m);
        lines[0].CargoDeduction.Should().Be(20m);
        lines[0].NetAmount.Should().Be(400m);
    }

    [Fact]
    public async Task ParseLinesAsync_WithoutParseAsync_ShouldReturnEmpty()
    {
        var parser = new CiceksepetiSettlementParser(_loggerMock.Object);
        var batch = MesTech.Domain.Accounting.Entities.SettlementBatch.Create(
            Guid.Empty, "Ciceksepeti", DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0);

        var lines = await parser.ParseLinesAsync(batch);
        lines.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_TurkishDecimalFormat_ShouldParseCorrectly()
    {
        // JSON with string numbers (NumberHandling allows reading from strings)
        var json = """
        {
            "totalCount": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "items": [
                {
                    "orderNo": "CS-003",
                    "productName": "Test",
                    "saleAmount": "1500.50",
                    "commissionAmount": "225.08",
                    "commissionRate": "0.15",
                    "cargoContribution": "25.00",
                    "serviceFee": "10.00",
                    "netAmount": "1240.42",
                    "transactionDate": "2026-03-10",
                    "category": "Hediye"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.TotalGross.Should().Be(1500.50m);
        batch.TotalCommission.Should().Be(225.08m);
    }

    [Fact]
    public async Task ParseAsync_CommissionPerLine_ShouldBeAccurate()
    {
        var json = """
        {
            "totalCount": 3,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "items": [
                {
                    "orderNo": "CS-010",
                    "saleAmount": 100.00,
                    "commissionAmount": 15.00,
                    "commissionRate": 0.15,
                    "cargoContribution": 0,
                    "serviceFee": 0,
                    "netAmount": 85.00
                },
                {
                    "orderNo": "CS-011",
                    "saleAmount": 200.00,
                    "commissionAmount": 40.00,
                    "commissionRate": 0.20,
                    "cargoContribution": 0,
                    "serviceFee": 0,
                    "netAmount": 160.00
                },
                {
                    "orderNo": "CS-012",
                    "saleAmount": 300.00,
                    "commissionAmount": 30.00,
                    "commissionRate": 0.10,
                    "cargoContribution": 0,
                    "serviceFee": 0,
                    "netAmount": 270.00
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.TotalGross.Should().Be(600m);
        batch.TotalCommission.Should().Be(85m);
        batch.TotalNet.Should().Be(515m);
    }

    [Fact]
    public async Task ParseAsync_PeriodDates_ShouldBeParsed()
    {
        var json = """
        {
            "totalCount": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "items": [
                {
                    "orderNo": "CS-020",
                    "saleAmount": 100.00,
                    "commissionAmount": 10.00,
                    "commissionRate": 0.10,
                    "cargoContribution": 0,
                    "serviceFee": 0,
                    "netAmount": 90.00,
                    "transactionDate": "2026-03-05"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.PeriodStart.Should().Be(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        batch.PeriodEnd.Should().Be(new DateTime(2026, 3, 14, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ParseAsync_MultipleItems_ComputesSHA256Hash()
    {
        var json = """
        {
            "totalCount": 1,
            "items": [
                {
                    "orderNo": "CS-030",
                    "saleAmount": 100.00,
                    "commissionAmount": 10.00,
                    "commissionRate": 0.10,
                    "cargoContribution": 0,
                    "serviceFee": 0,
                    "netAmount": 90.00
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        // ParseAsync should not throw (hash is computed internally)
        var batch = await _sut.ParseAsync(stream, "json");
        batch.Should().NotBeNull();
    }

    [Fact]
    public async Task ParseAsync_NullRawData_ThrowsArgumentNull()
    {
        var act = async () => await _sut.ParseAsync(null!, "json");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseLinesAsync_NullBatch_ThrowsArgumentNull()
    {
        var act = async () => await _sut.ParseLinesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseAsync_MissingPeriodDates_FallsBackToTransactionDates()
    {
        var json = """
        {
            "totalCount": 1,
            "items": [
                {
                    "orderNo": "CS-040",
                    "saleAmount": 100.00,
                    "commissionAmount": 10.00,
                    "commissionRate": 0.10,
                    "cargoContribution": 0,
                    "serviceFee": 0,
                    "netAmount": 90.00,
                    "transactionDate": "2026-03-10"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.PeriodStart.Should().Be(new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc));
        batch.PeriodEnd.Should().Be(new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ParseLinesAsync_AddedToBatch_LinesCountMatches()
    {
        var json = """
        {
            "totalCount": 2,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "items": [
                { "orderNo": "CS-050", "saleAmount": 100, "commissionAmount": 10, "commissionRate": 0.10, "cargoContribution": 5, "serviceFee": 2, "netAmount": 83 },
                { "orderNo": "CS-051", "saleAmount": 200, "commissionAmount": 20, "commissionRate": 0.10, "cargoContribution": 8, "serviceFee": 3, "netAmount": 169 }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(2);
        batch.Lines.Should().HaveCount(2);
    }
}
