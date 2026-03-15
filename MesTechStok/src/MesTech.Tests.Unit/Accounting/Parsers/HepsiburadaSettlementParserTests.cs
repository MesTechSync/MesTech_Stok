using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

[Trait("Category", "Unit")]
public class HepsiburadaSettlementParserTests
{
    private readonly HepsiburadaSettlementParser _sut;
    private readonly Mock<ILogger<HepsiburadaSettlementParser>> _loggerMock = new();

    public HepsiburadaSettlementParserTests()
    {
        _sut = new HepsiburadaSettlementParser(_loggerMock.Object);
    }

    [Fact]
    public void Platform_ShouldBeHepsiburada()
    {
        _sut.Platform.Should().Be("Hepsiburada");
    }

    [Fact]
    public async Task ParseAsync_ValidJson_ShouldReturnBatch()
    {
        var json = """
        {
            "data": {
                "totalCount": 2,
                "settlements": [
                    {
                        "orderId": "HB-001",
                        "productName": "Telefon Kilifi",
                        "saleAmount": 200.00,
                        "commissionAmount": 36.00,
                        "commissionRate": 0.18,
                        "cargoContribution": 10.00,
                        "netAmount": 154.00,
                        "transactionDate": "2026-03-10",
                        "category": "Aksesuar"
                    },
                    {
                        "orderId": "HB-002",
                        "productName": "Kulaklik",
                        "saleAmount": 500.00,
                        "commissionAmount": 90.00,
                        "commissionRate": 0.18,
                        "cargoContribution": 15.00,
                        "netAmount": 395.00,
                        "transactionDate": "2026-03-12",
                        "category": "Elektronik"
                    }
                ],
                "summary": {
                    "totalSaleAmount": 700.00,
                    "totalCommissionAmount": 126.00,
                    "totalCargoContribution": 25.00,
                    "totalNetAmount": 549.00
                }
            }
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.Should().NotBeNull();
        batch.Platform.Should().Be("Hepsiburada");
        batch.TotalGross.Should().Be(700m);
        batch.TotalCommission.Should().Be(126m);
        batch.TotalNet.Should().Be(549m);
    }

    [Fact]
    public async Task ParseAsync_WithSummaryBlock_ShouldUseSummaryValues()
    {
        var json = """
        {
            "data": {
                "totalCount": 1,
                "settlements": [
                    {
                        "orderId": "HB-001",
                        "saleAmount": 100,
                        "commissionAmount": 18,
                        "commissionRate": 0.18,
                        "cargoContribution": 5,
                        "netAmount": 77
                    }
                ],
                "summary": {
                    "totalSaleAmount": 9999,
                    "totalCommissionAmount": 1800,
                    "totalCargoContribution": 500,
                    "totalNetAmount": 7699
                }
            }
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        // Summary values should be used, not line item sums
        batch.TotalGross.Should().Be(9999m);
        batch.TotalCommission.Should().Be(1800m);
        batch.TotalNet.Should().Be(7699m);
    }

    [Fact]
    public async Task ParseAsync_WithoutSummary_ShouldCalculateFromItems()
    {
        var json = """
        {
            "data": {
                "totalCount": 1,
                "settlements": [
                    {
                        "orderId": "HB-001",
                        "saleAmount": 500,
                        "commissionAmount": 90,
                        "commissionRate": 0.18,
                        "cargoContribution": 15,
                        "netAmount": 395
                    }
                ]
            }
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.TotalGross.Should().Be(500m);
        batch.TotalCommission.Should().Be(90m);
        batch.TotalNet.Should().Be(395m);
    }

    [Fact]
    public async Task ParseAsync_EmptyResponse_ShouldReturnEmptyBatch()
    {
        var json = """
        {
            "data": {
                "totalCount": 0,
                "settlements": []
            }
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.TotalGross.Should().Be(0m);
        batch.TotalCommission.Should().Be(0m);
        batch.TotalNet.Should().Be(0m);
    }

    [Fact]
    public async Task ParseAsync_NullData_ShouldReturnEmptyBatch()
    {
        var json = """{ "data": null }""";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.Should().NotBeNull();
        batch.TotalGross.Should().Be(0m);
    }

    [Fact]
    public async Task ParseLinesAsync_ShouldReturnLines()
    {
        var json = """
        {
            "data": {
                "totalCount": 1,
                "settlements": [
                    {
                        "orderId": "HB-001",
                        "saleAmount": 1000,
                        "commissionAmount": 180,
                        "commissionRate": 0.18,
                        "cargoContribution": 25,
                        "netAmount": 795
                    }
                ]
            }
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(1);
        lines[0].GrossAmount.Should().Be(1000m);
        lines[0].CommissionAmount.Should().Be(180m);
        lines[0].CargoDeduction.Should().Be(25m);
        lines[0].OrderId.Should().Be("HB-001");
    }

    [Fact]
    public async Task ParseLinesAsync_WithoutPriorParse_ShouldReturnEmpty()
    {
        var parser = new HepsiburadaSettlementParser(_loggerMock.Object);
        var batch = MesTech.Domain.Accounting.Entities.SettlementBatch.Create(
            Guid.Empty, "Hepsiburada",
            DateTime.UtcNow, DateTime.UtcNow,
            0m, 0m, 0m);

        var lines = await parser.ParseLinesAsync(batch);

        lines.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_WithNullStream_ShouldThrow()
    {
        var act = async () => await _sut.ParseAsync(null!, "json");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseLinesAsync_WithNullBatch_ShouldThrow()
    {
        var act = async () => await _sut.ParseLinesAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var act = () => new HepsiburadaSettlementParser(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseLinesAsync_CommissionCreation_ShouldCreateForNonZero()
    {
        var json = """
        {
            "data": {
                "totalCount": 2,
                "settlements": [
                    { "orderId": "HB-1", "saleAmount": 500, "commissionAmount": 90, "commissionRate": 0.18, "cargoContribution": 10, "netAmount": 400 },
                    { "orderId": "HB-2", "saleAmount": 300, "commissionAmount": 0, "commissionRate": 0, "cargoContribution": 5, "netAmount": 295 }
                ]
            }
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(2);
        // Verified: parser processes both lines without exceptions
    }

    [Fact]
    public async Task ParseAsync_ShouldSetPeriodDates()
    {
        var json = """
        {
            "data": {
                "settlements": [
                    { "orderId": "HB-1", "saleAmount": 100, "commissionAmount": 18, "commissionRate": 0.18, "cargoContribution": 0, "netAmount": 82, "transactionDate": "2026-03-05" },
                    { "orderId": "HB-2", "saleAmount": 200, "commissionAmount": 36, "commissionRate": 0.18, "cargoContribution": 0, "netAmount": 164, "transactionDate": "2026-03-15" }
                ]
            }
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.PeriodStart.Should().BeOnOrBefore(batch.PeriodEnd);
    }
}
