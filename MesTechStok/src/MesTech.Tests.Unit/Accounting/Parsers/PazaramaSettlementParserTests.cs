using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

/// <summary>
/// PazaramaSettlementParser tests — JSON parsing, commission creation, empty data.
/// </summary>
[Trait("Category", "Unit")]
public class PazaramaSettlementParserTests
{
    private readonly PazaramaSettlementParser _sut;
    private readonly Mock<ILogger<PazaramaSettlementParser>> _loggerMock = new();

    public PazaramaSettlementParserTests()
    {
        _sut = new PazaramaSettlementParser(_loggerMock.Object);
    }

    [Fact]
    public void Platform_ShouldBePazarama()
    {
        _sut.Platform.Should().Be("Pazarama");
    }

    [Fact]
    public async Task ParseAsync_ValidJson_ShouldReturnBatch()
    {
        var json = """
        {
            "totalCount": 2,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "settlements": [
                {
                    "orderId": "PZR-001",
                    "productName": "Urun A",
                    "amount": 800.00,
                    "commission": 120.00,
                    "commissionRate": 0.15,
                    "cargoFee": 25.00,
                    "netPayout": 655.00,
                    "transactionDate": "2026-03-05",
                    "category": "Elektronik"
                },
                {
                    "orderId": "PZR-002",
                    "productName": "Urun B",
                    "amount": 400.00,
                    "commission": 60.00,
                    "commissionRate": 0.15,
                    "cargoFee": 15.00,
                    "netPayout": 325.00,
                    "transactionDate": "2026-03-10",
                    "category": "Giyim"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.Should().NotBeNull();
        batch.Platform.Should().Be("Pazarama");
        batch.TotalGross.Should().Be(1200m);
        batch.TotalCommission.Should().Be(180m);
        batch.TotalNet.Should().Be(980m);
    }

    [Fact]
    public async Task ParseAsync_EmptySettlements_ShouldReturnEmptyBatch()
    {
        var json = """
        {
            "totalCount": 0,
            "settlements": []
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
            "settlements": [
                {
                    "orderId": "PZR-010",
                    "productName": "Urun X",
                    "amount": 500.00,
                    "commission": 75.00,
                    "commissionRate": 0.15,
                    "cargoFee": 20.00,
                    "netPayout": 405.00,
                    "transactionDate": "2026-03-05",
                    "category": "Spor"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(1);
        lines[0].OrderId.Should().Be("PZR-010");
        lines[0].GrossAmount.Should().Be(500m);
        lines[0].CommissionAmount.Should().Be(75m);
        lines[0].ServiceFee.Should().Be(0m); // Pazarama has no separate service fee
        lines[0].CargoDeduction.Should().Be(20m);
        lines[0].NetAmount.Should().Be(405m);
    }

    [Fact]
    public async Task ParseLinesAsync_WithoutParseAsync_ShouldReturnEmpty()
    {
        var parser = new PazaramaSettlementParser(_loggerMock.Object);
        var batch = MesTech.Domain.Accounting.Entities.SettlementBatch.Create(
            Guid.Empty, "Pazarama", DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0);

        var lines = await parser.ParseLinesAsync(batch);
        lines.Should().BeEmpty();
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
            "settlements": [
                {
                    "orderId": "PZR-020",
                    "amount": 100.00,
                    "commission": 15.00,
                    "commissionRate": 0.15,
                    "cargoFee": 5.00,
                    "netPayout": 80.00,
                    "transactionDate": "2026-03-08"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.PeriodStart.Should().Be(new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc));
        batch.PeriodEnd.Should().Be(new DateTime(2026, 3, 8, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ParseAsync_PeriodDates_ShouldBeParsed()
    {
        var json = """
        {
            "totalCount": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-15",
            "settlements": [
                {
                    "orderId": "PZR-030",
                    "amount": 200.00,
                    "commission": 30.00,
                    "commissionRate": 0.15,
                    "cargoFee": 10.00,
                    "netPayout": 160.00
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.PeriodStart.Should().Be(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        batch.PeriodEnd.Should().Be(new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ParseLinesAsync_CommissionCreation_ShouldCreateForNonZero()
    {
        var json = """
        {
            "totalCount": 2,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "settlements": [
                {
                    "orderId": "PZR-040",
                    "amount": 100.00,
                    "commission": 15.00,
                    "commissionRate": 0.15,
                    "cargoFee": 5.00,
                    "netPayout": 80.00,
                    "category": "Kitap"
                },
                {
                    "orderId": "PZR-041",
                    "amount": 50.00,
                    "commission": 0.00,
                    "commissionRate": 0.00,
                    "cargoFee": 3.00,
                    "netPayout": 47.00,
                    "category": "Diger"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(2);
        lines[0].CommissionAmount.Should().Be(15m);
        lines[1].CommissionAmount.Should().Be(0m);
    }

    [Fact]
    public async Task ParseAsync_MultipleItems_SumsCorrectly()
    {
        var json = """
        {
            "totalCount": 3,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "settlements": [
                { "orderId": "PZ1", "amount": 100, "commission": 10, "commissionRate": 0.10, "cargoFee": 5, "netPayout": 85 },
                { "orderId": "PZ2", "amount": 200, "commission": 30, "commissionRate": 0.15, "cargoFee": 8, "netPayout": 162 },
                { "orderId": "PZ3", "amount": 300, "commission": 60, "commissionRate": 0.20, "cargoFee": 12, "netPayout": 228 }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.TotalGross.Should().Be(600m);
        batch.TotalCommission.Should().Be(100m);
        batch.TotalNet.Should().Be(475m);
    }

    [Fact]
    public async Task ParseLinesAsync_BatchLines_AreAddedToBatch()
    {
        var json = """
        {
            "totalCount": 2,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "settlements": [
                { "orderId": "PZ-A", "amount": 100, "commission": 10, "commissionRate": 0.10, "cargoFee": 5, "netPayout": 85 },
                { "orderId": "PZ-B", "amount": 200, "commission": 20, "commissionRate": 0.10, "cargoFee": 8, "netPayout": 172 }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        batch.Lines.Should().HaveCount(2);
        lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseAsync_StringAmounts_ShouldParse()
    {
        // NumberHandling allows reading from strings
        var json = """
        {
            "totalCount": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "settlements": [
                {
                    "orderId": "PZR-STR",
                    "amount": "750.25",
                    "commission": "112.54",
                    "commissionRate": "0.15",
                    "cargoFee": "18.00",
                    "netPayout": "619.71"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.TotalGross.Should().Be(750.25m);
        batch.TotalCommission.Should().Be(112.54m);
    }
}
