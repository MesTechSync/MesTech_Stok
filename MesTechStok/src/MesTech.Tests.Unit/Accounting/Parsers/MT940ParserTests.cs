using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Banking.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

[Trait("Category", "Unit")]
public class MT940ParserTests
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly MT940Parser _sut;
    private readonly Mock<ILogger<MT940Parser>> _loggerMock = new();
    private readonly Guid _bankAccountId = Guid.NewGuid();

    public MT940ParserTests()
    {
        _sut = new MT940Parser(_loggerMock.Object);
    }

    [Fact]
    public void Format_ShouldBeMT940()
    {
        _sut.Format.Should().Be("MT940");
    }

    [Fact]
    public async Task ParseAsync_ValidMT940_ShouldReturnTransactions()
    {
        var mt940 = """
        :20:STMT260315
        :25:IBAN12345
        :28C:1/1
        :60F:C260314TRY10000,00
        :61:260315C1500,50NTRFREF001//BANKREF001
        :86:TRENDYOL ODEME
        :61:260315D250,00NTRF//BANKREF002
        :86:KARGO ODEMESI
        :62F:C260315TRY11250,50
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(2);
        transactions[0].Amount.Should().Be(1500.50m);
        transactions[0].Description.Should().Contain("TRENDYOL ODEME");
        transactions[1].Amount.Should().Be(-250.00m);
    }

    [Fact]
    public async Task ParseAsync_CreditIndicator_ShouldBePositive()
    {
        var mt940 = """
        :20:STMT001
        :61:260315C1000,00NTRFREF001
        :86:Credit transaction
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(1);
        transactions[0].Amount.Should().BePositive();
    }

    [Fact]
    public async Task ParseAsync_DebitIndicator_ShouldBeNegative()
    {
        var mt940 = """
        :20:STMT001
        :61:260315D500,00NTRFREF001
        :86:Debit transaction
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(1);
        transactions[0].Amount.Should().BeNegative();
    }

    [Fact]
    public async Task ParseAsync_ReversalCredit_ShouldBePositive()
    {
        var mt940 = """
        :20:STMT001
        :61:260315RC500,00NTRFREF001
        :86:Reversal credit
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(1);
        transactions[0].Amount.Should().BePositive();
    }

    [Fact]
    public async Task ParseAsync_ReversalDebit_ShouldBeNegative()
    {
        var mt940 = """
        :20:STMT001
        :61:260315RD500,00NTRFREF001
        :86:Reversal debit
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(1);
        transactions[0].Amount.Should().BeNegative();
    }

    [Fact]
    public async Task ParseAsync_EuropeanDecimalComma_ShouldParseCorrectly()
    {
        var mt940 = """
        :20:STMT001
        :61:260315C1234,56NTRFREF001
        :86:European decimal
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].Amount.Should().Be(1234.56m);
    }

    [Fact]
    public async Task ParseAsync_MultiLine86_ShouldConcatenate()
    {
        var mt940 = """
        :20:STMT001
        :61:260315C500,00NTRFREF001
        :86:First line description
        second line continuation
        third line continuation
        :62F:C260315TRY10500,00
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].Description.Should().Contain("First line description");
        transactions[0].Description.Should().Contain("second line continuation");
        transactions[0].Description.Should().Contain("third line continuation");
    }

    [Fact]
    public async Task ParseAsync_Without86Tag_ShouldUseDefaultDescription()
    {
        var mt940 = """
        :20:STMT001
        :61:260315C500,00NTRFREF001
        :62F:C260315TRY10500,00
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ParseAsync_TransactionReference_ShouldBeExtracted()
    {
        var mt940 = """
        :20:MYREF123
        :61:260315C500,00NTRFREF001//BANKREF
        :86:Test
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].ReferenceNumber.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ParseAsync_EmptyFile_ShouldReturnEmpty()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(""));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_NoTransactions_ShouldReturnEmpty()
    {
        var mt940 = """
        :20:STMT001
        :25:IBAN12345
        :28C:1/1
        :60F:C260314TRY10000,00
        :62F:C260315TRY10000,00
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_IdempotencyKey_ShouldBeDeterministic()
    {
        var mt940 = """
        :20:STMT001
        :61:260315C500,00NTRFREF001
        :86:Test
        """;

        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var tx1 = await _sut.ParseAsync(stream1, _bankAccountId);

        var parser2 = new MT940Parser(_loggerMock.Object);
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var tx2 = await parser2.ParseAsync(stream2, _bankAccountId);

        tx1[0].IdempotencyKey.Should().Be(tx2[0].IdempotencyKey);
    }

    [Fact]
    public void ComputeIdempotencyKey_ShouldBeDeterministic()
    {
        var key1 = MT940Parser.ComputeIdempotencyKey(_bankAccountId, "260315", "500,00", "REF001");
        var key2 = MT940Parser.ComputeIdempotencyKey(_bankAccountId, "260315", "500,00", "REF001");

        key1.Should().Be(key2);
    }

    [Fact]
    public void ComputeIdempotencyKey_DifferentInputs_ShouldDiffer()
    {
        var key1 = MT940Parser.ComputeIdempotencyKey(_bankAccountId, "260315", "500,00", "REF001");
        var key2 = MT940Parser.ComputeIdempotencyKey(_bankAccountId, "260315", "600,00", "REF002");

        key1.Should().NotBe(key2);
    }

    [Fact]
    public async Task ParseAsync_WithEntryDate_ShouldParseCorrectly()
    {
        // :61: with optional entry date MMDD after value date YYMMDD
        var mt940 = """
        :20:STMT001
        :61:2603150316C1000,00NTRFREF001
        :86:With entry date
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(mt940));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(1);
        transactions[0].Amount.Should().Be(1000m);
    }
}
