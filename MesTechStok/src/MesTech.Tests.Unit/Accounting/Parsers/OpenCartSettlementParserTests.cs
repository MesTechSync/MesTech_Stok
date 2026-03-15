using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

/// <summary>
/// OpenCartSettlementParser tests — JSON parsing, Commission=0, gateway fee in ServiceFee, cargo expense.
/// </summary>
[Trait("Category", "Unit")]
public class OpenCartSettlementParserTests
{
    private readonly OpenCartSettlementParser _sut;
    private readonly Mock<ILogger<OpenCartSettlementParser>> _loggerMock = new();

    public OpenCartSettlementParserTests()
    {
        _sut = new OpenCartSettlementParser(_loggerMock.Object);
    }

    [Fact]
    public void Platform_ShouldBeOpenCart()
    {
        _sut.Platform.Should().Be("OpenCart");
    }

    [Fact]
    public async Task ParseAsync_ValidJson_ShouldReturnBatch()
    {
        var json = """
        {
            "totalOrders": 2,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "orders": [
                {
                    "orderId": "OC-001",
                    "productName": "Urun A",
                    "orderTotal": 500.00,
                    "gatewayFee": 15.00,
                    "cargoExpense": 20.00,
                    "netAmount": 465.00,
                    "orderDate": "2026-03-05",
                    "paymentMethod": "iyzico"
                },
                {
                    "orderId": "OC-002",
                    "productName": "Urun B",
                    "orderTotal": 300.00,
                    "gatewayFee": 9.00,
                    "cargoExpense": 15.00,
                    "netAmount": 276.00,
                    "orderDate": "2026-03-10",
                    "paymentMethod": "PayTR"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.Should().NotBeNull();
        batch.Platform.Should().Be("OpenCart");
        batch.TotalGross.Should().Be(800m);
        batch.TotalCommission.Should().Be(0m); // Own store — zero commission
        batch.TotalNet.Should().Be(741m);
    }

    [Fact]
    public async Task ParseAsync_CommissionAlwaysZero_OwnStore()
    {
        var json = """
        {
            "totalOrders": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "orders": [
                {
                    "orderId": "OC-010",
                    "orderTotal": 1000.00,
                    "gatewayFee": 30.00,
                    "cargoExpense": 25.00,
                    "netAmount": 945.00,
                    "orderDate": "2026-03-05"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");

        batch.TotalCommission.Should().Be(0m);
    }

    [Fact]
    public async Task ParseLinesAsync_GatewayFeeInServiceFee()
    {
        var json = """
        {
            "totalOrders": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "orders": [
                {
                    "orderId": "OC-020",
                    "orderTotal": 200.00,
                    "gatewayFee": 6.00,
                    "cargoExpense": 10.00,
                    "netAmount": 184.00,
                    "orderDate": "2026-03-05",
                    "paymentMethod": "iyzico"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(1);
        lines[0].OrderId.Should().Be("OC-020");
        lines[0].CommissionAmount.Should().Be(0m);
        lines[0].ServiceFee.Should().Be(6m); // gatewayFee mapped to ServiceFee
        lines[0].CargoDeduction.Should().Be(10m);
        lines[0].NetAmount.Should().Be(184m);
    }

    [Fact]
    public async Task ParseAsync_EmptyOrders_ShouldReturnEmptyBatch()
    {
        var json = """
        {
            "totalOrders": 0,
            "orders": []
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
    public async Task ParseLinesAsync_WithoutParseAsync_ShouldReturnEmpty()
    {
        var parser = new OpenCartSettlementParser(_loggerMock.Object);
        var batch = MesTech.Domain.Accounting.Entities.SettlementBatch.Create(
            Guid.Empty, "OpenCart", DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0);

        var lines = await parser.ParseLinesAsync(batch);
        lines.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_PeriodDates_ShouldBeParsed()
    {
        var json = """
        {
            "totalOrders": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-15",
            "orders": [
                {
                    "orderId": "OC-030",
                    "orderTotal": 100.00,
                    "gatewayFee": 3.00,
                    "cargoExpense": 5.00,
                    "netAmount": 92.00,
                    "orderDate": "2026-03-10"
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
    public async Task ParseAsync_CargoExpense_ShouldBeTracked()
    {
        var json = """
        {
            "totalOrders": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-14",
            "orders": [
                {
                    "orderId": "OC-040",
                    "orderTotal": 150.00,
                    "gatewayFee": 0.00,
                    "cargoExpense": 35.00,
                    "netAmount": 115.00,
                    "orderDate": "2026-03-05"
                }
            ]
        }
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var batch = await _sut.ParseAsync(stream, "json");
        var lines = await _sut.ParseLinesAsync(batch);

        lines[0].CargoDeduction.Should().Be(35m);
        lines[0].ServiceFee.Should().Be(0m);
    }
}
