using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Banking.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

[Trait("Category", "Unit")]
public class OFXParserTests
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly OFXParser _sut;
    private readonly Mock<ILogger<OFXParser>> _loggerMock = new();
    private readonly Guid _bankAccountId = Guid.NewGuid();

    public OFXParserTests()
    {
        _sut = new OFXParser(_loggerMock.Object);
    }

    [Fact]
    public void Format_ShouldBeOFX()
    {
        _sut.Format.Should().Be("OFX");
    }

    [Fact]
    public async Task ParseAsync_ValidOFX_ShouldReturnTransactions()
    {
        var ofx = """
        <OFX>
        <BANKMSGSRSV1>
        <STMTTRNRS>
        <STMTRS>
        <BANKTRANLIST>
        <STMTTRN>
        <TRNTYPE>CREDIT
        <DTPOSTED>20260315120000
        <TRNAMT>1500.50
        <FITID>FIT001
        <NAME>TRENDYOL HAVALE
        <MEMO>Platform odemesi
        </STMTTRN>
        <STMTTRN>
        <TRNTYPE>DEBIT
        <DTPOSTED>20260314120000
        <TRNAMT>-250.00
        <FITID>FIT002
        <NAME>KARGO ODEMESI
        </STMTTRN>
        </BANKTRANLIST>
        </STMTRS>
        </STMTTRNRS>
        </BANKMSGSRSV1>
        </OFX>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(2);
        transactions[0].Amount.Should().Be(1500.50m);
        transactions[0].Description.Should().Contain("TRENDYOL HAVALE");
        transactions[1].Amount.Should().Be(-250.00m);
    }

    [Fact]
    public async Task ParseAsync_DateParsing_yyyyMMddHHmmss()
    {
        var ofx = """
        <STMTTRN>
        <DTPOSTED>20260315143000
        <TRNAMT>100.00
        <NAME>Test
        </STMTTRN>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(1);
        transactions[0].TransactionDate.Year.Should().Be(2026);
        transactions[0].TransactionDate.Month.Should().Be(3);
        transactions[0].TransactionDate.Day.Should().Be(15);
    }

    [Fact]
    public async Task ParseAsync_DateParsing_yyyyMMdd()
    {
        var ofx = """
        <STMTTRN>
        <DTPOSTED>20260315
        <TRNAMT>100.00
        <NAME>Test
        </STMTTRN>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(1);
        transactions[0].TransactionDate.Date.Should().Be(new DateTime(2026, 3, 15));
    }

    [Fact]
    public async Task ParseAsync_CreditAndDebitAmounts_ShouldPreserveSign()
    {
        var ofx = """
        <STMTTRN>
        <DTPOSTED>20260315
        <TRNAMT>1000.00
        <NAME>Gelir
        </STMTTRN>
        <STMTTRN>
        <DTPOSTED>20260315
        <TRNAMT>-500.00
        <NAME>Gider
        </STMTTRN>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].Amount.Should().BePositive();
        transactions[1].Amount.Should().BeNegative();
    }

    [Fact]
    public async Task ParseAsync_IdempotencyKey_ShouldBeGenerated()
    {
        var ofx = """
        <STMTTRN>
        <DTPOSTED>20260315
        <TRNAMT>100.00
        <FITID>UNIQ001
        <NAME>Test
        </STMTTRN>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].IdempotencyKey.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ParseAsync_EmptyFile_ShouldReturnEmpty()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_NoSTMTTRN_ShouldReturnEmpty()
    {
        var ofx = "<OFX><BANKMSGSRSV1></BANKMSGSRSV1></OFX>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));

        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_MissingDTPOSTED_ShouldSkipTransaction()
    {
        var ofx = """
        <STMTTRN>
        <TRNAMT>100.00
        <NAME>No date
        </STMTTRN>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_MissingTRNAMT_ShouldSkipTransaction()
    {
        var ofx = """
        <STMTTRN>
        <DTPOSTED>20260315
        <NAME>No amount
        </STMTTRN>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_WithMemo_ShouldConcatenateDescription()
    {
        var ofx = """
        <STMTTRN>
        <DTPOSTED>20260315
        <TRNAMT>100.00
        <NAME>MARKET
        <MEMO>Alisveris
        </STMTTRN>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].Description.Should().Contain("MARKET");
        transactions[0].Description.Should().Contain("Alisveris");
    }

    [Fact]
    public async Task ParseAsync_WithFITID_ShouldSetReferenceNumber()
    {
        var ofx = """
        <STMTTRN>
        <DTPOSTED>20260315
        <TRNAMT>100.00
        <FITID>REF-XYZ-123
        <NAME>Test
        </STMTTRN>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].ReferenceNumber.Should().Be("REF-XYZ-123");
    }

    [Fact]
    public async Task ParseAsync_DateWithTimezone_ShouldParse()
    {
        var ofx = """
        <STMTTRN>
        <DTPOSTED>20260315120000[0:GMT]
        <TRNAMT>100.00
        <NAME>TZ Test
        </STMTTRN>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(1);
    }

    [Fact]
    public void ExtractTagValue_ShouldExtractCorrectly()
    {
        var content = "<NAME>MARKET XYZ\n<MEMO>Payment";
        var result = OFXParser.ExtractTagValue(content, "NAME");

        result.Should().Be("MARKET XYZ");
    }

    [Fact]
    public void ExtractTagValue_MissingTag_ShouldReturnNull()
    {
        var content = "<NAME>Test";
        var result = OFXParser.ExtractTagValue(content, "MEMO");

        result.Should().BeNull();
    }

    [Fact]
    public void ExtractBlocks_ShouldExtractAllBlocks()
    {
        var content = "<STMTTRN>Block1</STMTTRN><STMTTRN>Block2</STMTTRN>";
        var blocks = OFXParser.ExtractBlocks(content, "STMTTRN");

        blocks.Should().HaveCount(2);
    }

    [Fact]
    public void ComputeIdempotencyKey_ShouldBeDeterministic()
    {
        var key1 = OFXParser.ComputeIdempotencyKey(_bankAccountId, "20260315", "100.00", "FIT001");
        var key2 = OFXParser.ComputeIdempotencyKey(_bankAccountId, "20260315", "100.00", "FIT001");

        key1.Should().Be(key2);
    }

    [Fact]
    public void ComputeIdempotencyKey_DifferentInputs_ShouldDiffer()
    {
        var key1 = OFXParser.ComputeIdempotencyKey(_bankAccountId, "20260315", "100.00", "FIT001");
        var key2 = OFXParser.ComputeIdempotencyKey(_bankAccountId, "20260315", "200.00", "FIT002");

        key1.Should().NotBe(key2);
    }
}
