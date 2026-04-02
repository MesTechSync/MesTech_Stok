using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

/// <summary>
/// Bitrix24SettlementParser tests — JSON deal parsing, commission allocation.
/// DEV3 TUR7-FULL: G10788 test gap kapatma.
/// </summary>
[Trait("Category", "Unit")]
public class Bitrix24SettlementParserTests
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Bitrix24SettlementParser _sut;
    private readonly Mock<ILogger<Bitrix24SettlementParser>> _loggerMock = new();

    public Bitrix24SettlementParserTests()
    {
        _sut = new Bitrix24SettlementParser(_loggerMock.Object);
    }

    [Fact]
    public void Platform_ShouldBeBitrix24()
    {
        _sut.Platform.Should().Be("Bitrix24");
    }

    [Fact]
    public async Task ParseAsync_ValidJson_ShouldReturnBatch()
    {
        var json = """
        {
            "total": 2,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-15",
            "deals": [
                {
                    "dealId": "D-001",
                    "title": "Test Deal 1",
                    "opportunity": 1000.00,
                    "commissionAmount": 50.00,
                    "cargoAmount": 25.00,
                    "netAmount": 925.00,
                    "closeDate": "2026-03-05",
                    "currencyId": "TRY"
                },
                {
                    "dealId": "D-002",
                    "title": "Test Deal 2",
                    "opportunity": 500.00,
                    "commissionAmount": 25.00,
                    "cargoAmount": 15.00,
                    "netAmount": 460.00,
                    "closeDate": "2026-03-10",
                    "currencyId": "TRY"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");

        batch.Should().NotBeNull();
        batch.Platform.Should().Be("Bitrix24");
        batch.TenantId.Should().Be(TestTenantId);
        batch.TotalGross.Should().Be(1500m);
        batch.TotalCommission.Should().Be(75m);
        batch.TotalNet.Should().Be(1385m);
    }

    [Fact]
    public async Task ParseAsync_EmptyDeals_ShouldReturnEmptyBatch()
    {
        var json = """{"total":0,"deals":[]}""";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");

        batch.Should().NotBeNull();
        batch.TotalGross.Should().Be(0m);
        batch.TotalCommission.Should().Be(0m);
    }

    [Fact]
    public async Task ParseLinesAsync_ValidBatch_ShouldCreateLines()
    {
        var json = """
        {
            "total": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-15",
            "deals": [
                {
                    "dealId": "D-100",
                    "title": "Single Deal",
                    "opportunity": 750.00,
                    "commissionAmount": 37.50,
                    "cargoAmount": 20.00,
                    "netAmount": 692.50,
                    "closeDate": "2026-03-08",
                    "currencyId": "TRY"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(1);
        lines[0].OrderId.Should().Be("D-100");
        lines[0].GrossAmount.Should().Be(750m);
        lines[0].CommissionAmount.Should().Be(37.50m);
        lines[0].CargoDeduction.Should().Be(20m);
        lines[0].NetAmount.Should().Be(692.50m);
    }

    [Fact]
    public async Task ParseLinesAsync_WithoutPriorParse_ShouldReturnEmpty()
    {
        var parser = new Bitrix24SettlementParser(_loggerMock.Object);
        var batch = MesTech.Domain.Accounting.Entities.SettlementBatch.Create(
            TestTenantId, "Bitrix24", DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0);

        var lines = await parser.ParseLinesAsync(batch);
        lines.Should().BeEmpty();
    }

    [Fact]
    public void ParseAsync_ObsoleteOverload_ShouldThrow()
    {
#pragma warning disable CS0618
        var act = async () =>
        {
            using var stream = new MemoryStream();
            await _sut.ParseAsync(stream, "json");
        };
        act.Should().ThrowAsync<ArgumentException>();
#pragma warning restore CS0618
    }
}
