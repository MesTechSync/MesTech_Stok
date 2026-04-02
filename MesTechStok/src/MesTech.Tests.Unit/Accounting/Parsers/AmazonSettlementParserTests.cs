using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

[Trait("Category", "Unit")]
public class AmazonSettlementParserTests
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly AmazonSettlementParser _sut;
    private readonly Mock<ILogger<AmazonSettlementParser>> _loggerMock = new();

    public AmazonSettlementParserTests()
    {
        _sut = new AmazonSettlementParser(_loggerMock.Object);
    }

    [Fact]
    public void Platform_ShouldBeAmazon()
    {
        _sut.Platform.Should().Be("Amazon");
    }

    private static string BuildTsv(params string[] dataLines)
    {
        var header = "settlement-id\tsettlement-start-date\tsettlement-end-date\tdeposit-date\ttotal-amount\tcurrency\ttransaction-type\torder-id\tmerchant-order-id\tadjustment-id\tshipment-id\tmarketplace-name\tamount-type\tamount-description\tamount\tfulfillment-id\tposted-date\tposted-date-time\torder-item-code\tmerchant-order-item-id\tmerchant-adjustment-item-id\tsku\tquantity-purchased\tpromotion-id";
        var sb = new StringBuilder();
        sb.AppendLine(header);
        foreach (var line in dataLines)
            sb.AppendLine(line);
        return sb.ToString();
    }

    private static string MakeDataLine(
        string settlementId = "S1",
        string orderId = "ORD-001",
        string amountType = "ItemPrice",
        string amount = "100.00",
        string currency = "TRY",
        string transactionType = "Order")
    {
        return $"{settlementId}\t2026-03-01\t2026-03-15\t2026-03-20\t1000\t{currency}\t{transactionType}\t{orderId}\t\t\t\tAmazon.com.tr\t{amountType}\tProduct price\t{amount}\t\t2026-03-10\t\t\t\t\tSKU001\t1\t";
    }

    [Fact]
    public async Task ParseAsync_ValidTsv_ShouldReturnBatch()
    {
        var tsv = BuildTsv(
            MakeDataLine(amountType: "ItemPrice", amount: "500"),
            MakeDataLine(amountType: "Commission", amount: "-75")
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(tsv));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "tsv");

        batch.Should().NotBeNull();
        batch.Platform.Should().Be("Amazon");
    }

    [Fact]
    public async Task ParseAsync_EmptyTsv_ShouldReturnEmptyBatch()
    {
        var tsv = "settlement-id\tamount\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(tsv));

        var batch = await _sut.ParseAsync(TestTenantId, stream, "tsv");

        batch.Should().NotBeNull();
        batch.TotalGross.Should().Be(0m);
    }

    [Fact]
    public async Task ParseAsync_HeaderOnly_ShouldReturnEmptyBatch()
    {
        var tsv = BuildTsv();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(tsv));

        var batch = await _sut.ParseAsync(TestTenantId, stream, "tsv");

        batch.TotalGross.Should().Be(0m);
        batch.TotalNet.Should().Be(0m);
    }

    [Fact]
    public async Task ParseAsync_MultiCurrency_ShouldLogWarning()
    {
        var tsv = BuildTsv(
            MakeDataLine(currency: "TRY", amount: "100"),
            MakeDataLine(currency: "USD", amount: "50")
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(tsv));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "tsv");

        batch.Should().NotBeNull();
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ParseLinesAsync_ShouldGroupByOrderId()
    {
        var tsv = BuildTsv(
            MakeDataLine(orderId: "ORD-001", amountType: "ItemPrice", amount: "500"),
            MakeDataLine(orderId: "ORD-001", amountType: "Commission", amount: "-75"),
            MakeDataLine(orderId: "ORD-002", amountType: "ItemPrice", amount: "300"),
            MakeDataLine(orderId: "ORD-002", amountType: "Commission", amount: "-45")
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(tsv));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "tsv");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task ParseLinesAsync_WithoutPriorParse_ShouldReturnEmpty()
    {
        var parser = new AmazonSettlementParser(_loggerMock.Object);
        var batch = MesTech.Domain.Accounting.Entities.SettlementBatch.Create(
            Guid.Empty, "Amazon",
            DateTime.UtcNow, DateTime.UtcNow,
            0m, 0m, 0m);

        var lines = await parser.ParseLinesAsync(batch);

        lines.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_WithMissingFields_ShouldHandleGracefully()
    {
        var tsv = BuildTsv(
            MakeDataLine(orderId: "", amountType: "ItemPrice", amount: "100")
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(tsv));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "tsv");

        batch.Should().NotBeNull();
    }

    [Fact]
    public async Task ParseAsync_WithNullStream_ShouldThrow()
    {
        var act = async () => await _sut.ParseAsync(TestTenantId, null!, "tsv");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseLinesAsync_WithNullBatch_ShouldThrow()
    {
        var act = async () => await _sut.ParseLinesAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseAsync_RefundTransactions_ShouldBeHandled()
    {
        var tsv = BuildTsv(
            MakeDataLine(orderId: "ORD-001", amountType: "ItemPrice", amount: "500", transactionType: "Order"),
            MakeDataLine(orderId: "ORD-001", amountType: "ItemPrice", amount: "-500", transactionType: "Refund")
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(tsv));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "tsv");

        batch.Should().NotBeNull();
        batch.TotalNet.Should().Be(0m);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var act = () => new AmazonSettlementParser(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseAsync_ShouldSetPeriodDates()
    {
        var tsv = BuildTsv(
            MakeDataLine(amountType: "ItemPrice", amount: "100")
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(tsv));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "tsv");

        batch.PeriodStart.Should().NotBe(default);
    }

    [Fact]
    public async Task ParseAsync_CommissionTypeLines_ShouldBeAbsoluteValue()
    {
        var tsv = BuildTsv(
            MakeDataLine(orderId: "ORD-001", amountType: "ItemPrice", amount: "500"),
            MakeDataLine(orderId: "ORD-001", amountType: "Commission", amount: "-75")
        );

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(tsv));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "tsv");

        batch.TotalCommission.Should().BeGreaterOrEqualTo(0m);
    }

    [Fact]
    public async Task ParseAsync_EmptyStream_ShouldReturnEmptyBatch()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "tsv");

        batch.TotalGross.Should().Be(0m);
    }
}
