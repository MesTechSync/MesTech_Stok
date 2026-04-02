using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Banking.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

[Trait("Category", "Unit")]
public class Camt053ParserTests
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly Camt053Parser _sut;
    private readonly Mock<ILogger<Camt053Parser>> _loggerMock = new();
    private readonly Guid _bankAccountId = Guid.NewGuid();

    public Camt053ParserTests()
    {
        _sut = new Camt053Parser(_loggerMock.Object);
    }

    [Fact]
    public void Format_ShouldBeCAMT053()
    {
        _sut.Format.Should().Be("CAMT053");
    }

    [Fact]
    public async Task ParseAsync_ValidXml_ShouldReturnTransactions()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
          <BkToCstmrStmt>
            <Stmt>
              <Ntry>
                <Amt Ccy="TRY">1500.50</Amt>
                <CdtDbtInd>CRDT</CdtDbtInd>
                <ValDt><Dt>2026-03-15</Dt></ValDt>
                <AcctSvcrRef>REF001</AcctSvcrRef>
                <RmtInf><Ustrd>TRENDYOL ODEME</Ustrd></RmtInf>
              </Ntry>
              <Ntry>
                <Amt Ccy="TRY">250.00</Amt>
                <CdtDbtInd>DBIT</CdtDbtInd>
                <ValDt><Dt>2026-03-14</Dt></ValDt>
                <AcctSvcrRef>REF002</AcctSvcrRef>
                <RmtInf><Ustrd>KARGO ODEMESI</Ustrd></RmtInf>
              </Ntry>
            </Stmt>
          </BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(2);
        transactions[0].Amount.Should().Be(1500.50m);
        transactions[0].Description.Should().Contain("TRENDYOL ODEME");
        transactions[1].Amount.Should().Be(-250.00m);
    }

    [Fact]
    public async Task ParseAsync_CreditDirection_ShouldBePositive()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
          <BkToCstmrStmt><Stmt>
            <Ntry>
              <Amt Ccy="TRY">1000</Amt>
              <CdtDbtInd>CRDT</CdtDbtInd>
              <RmtInf><Ustrd>Credit</Ustrd></RmtInf>
            </Ntry>
          </Stmt></BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].Amount.Should().BePositive();
    }

    [Fact]
    public async Task ParseAsync_DebitDirection_ShouldBeNegative()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
          <BkToCstmrStmt><Stmt>
            <Ntry>
              <Amt Ccy="TRY">500</Amt>
              <CdtDbtInd>DBIT</CdtDbtInd>
              <RmtInf><Ustrd>Debit</Ustrd></RmtInf>
            </Ntry>
          </Stmt></BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].Amount.Should().BeNegative();
    }

    [Fact]
    public async Task ParseAsync_WithNamespace_ShouldParseCorrectly()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
          <BkToCstmrStmt><Stmt>
            <Ntry>
              <Amt Ccy="TRY">100</Amt>
              <CdtDbtInd>CRDT</CdtDbtInd>
              <RmtInf><Ustrd>Namespaced</Ustrd></RmtInf>
            </Ntry>
          </Stmt></BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task ParseAsync_WithoutNamespace_ShouldParseCorrectly()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document>
          <BkToCstmrStmt><Stmt>
            <Ntry>
              <Amt Ccy="TRY">100</Amt>
              <CdtDbtInd>CRDT</CdtDbtInd>
              <RmtInf><Ustrd>No namespace</Ustrd></RmtInf>
            </Ntry>
          </Stmt></BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(1);
    }

    [Fact]
    public async Task ParseAsync_MissingOptionalFields_ShouldHandleGracefully()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
          <BkToCstmrStmt><Stmt>
            <Ntry>
              <Amt Ccy="TRY">100</Amt>
              <CdtDbtInd>CRDT</CdtDbtInd>
            </Ntry>
          </Stmt></BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(1);
        transactions[0].Description.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ParseAsync_MultipleNtryElements_ShouldParseAll()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
          <BkToCstmrStmt><Stmt>
            <Ntry><Amt Ccy="TRY">100</Amt><CdtDbtInd>CRDT</CdtDbtInd><RmtInf><Ustrd>TX1</Ustrd></RmtInf></Ntry>
            <Ntry><Amt Ccy="TRY">200</Amt><CdtDbtInd>CRDT</CdtDbtInd><RmtInf><Ustrd>TX2</Ustrd></RmtInf></Ntry>
            <Ntry><Amt Ccy="TRY">300</Amt><CdtDbtInd>DBIT</CdtDbtInd><RmtInf><Ustrd>TX3</Ustrd></RmtInf></Ntry>
          </Stmt></BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().HaveCount(3);
    }

    [Fact]
    public async Task ParseAsync_NoNtryElements_ShouldReturnEmpty()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
          <BkToCstmrStmt><Stmt></Stmt></BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_WithAcctSvcrRef_ShouldSetReferenceNumber()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
          <BkToCstmrStmt><Stmt>
            <Ntry>
              <Amt Ccy="TRY">100</Amt>
              <CdtDbtInd>CRDT</CdtDbtInd>
              <AcctSvcrRef>SVCREF-123</AcctSvcrRef>
              <RmtInf><Ustrd>With ref</Ustrd></RmtInf>
            </Ntry>
          </Stmt></BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].ReferenceNumber.Should().Be("SVCREF-123");
    }

    [Fact]
    public async Task ParseAsync_WithBookingDateFallback_ShouldUseBookingDate()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
          <BkToCstmrStmt><Stmt>
            <Ntry>
              <Amt Ccy="TRY">100</Amt>
              <CdtDbtInd>CRDT</CdtDbtInd>
              <BookgDt><Dt>2026-03-10</Dt></BookgDt>
              <RmtInf><Ustrd>Booking date only</Ustrd></RmtInf>
            </Ntry>
          </Stmt></BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].TransactionDate.Date.Should().Be(new DateTime(2026, 3, 10));
    }

    [Fact]
    public async Task ParseAsync_WithAddtlNtryInf_ShouldUseAsDescription()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
          <BkToCstmrStmt><Stmt>
            <Ntry>
              <Amt Ccy="TRY">100</Amt>
              <CdtDbtInd>CRDT</CdtDbtInd>
              <AddtlNtryInf>Additional info here</AddtlNtryInf>
            </Ntry>
          </Stmt></BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].Description.Should().Be("Additional info here");
    }

    [Fact]
    public void ComputeIdempotencyKey_Reference_ShouldBeDeterministic()
    {
        var key1 = Camt053Parser.ComputeIdempotencyKey(_bankAccountId, "REF001");
        var key2 = Camt053Parser.ComputeIdempotencyKey(_bankAccountId, "REF001");

        key1.Should().Be(key2);
    }

    [Fact]
    public void ComputeIdempotencyKey_Hash_ShouldBeDeterministic()
    {
        var key1 = Camt053Parser.ComputeIdempotencyKey(_bankAccountId, "20260315", "100", "Test");
        var key2 = Camt053Parser.ComputeIdempotencyKey(_bankAccountId, "20260315", "100", "Test");

        key1.Should().Be(key2);
    }

    [Fact]
    public async Task ParseAsync_IdempotencyKey_ShouldBePopulated()
    {
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.08">
          <BkToCstmrStmt><Stmt>
            <Ntry>
              <Amt Ccy="TRY">100</Amt>
              <CdtDbtInd>CRDT</CdtDbtInd>
              <AcctSvcrRef>REF123</AcctSvcrRef>
            </Ntry>
          </Stmt></BkToCstmrStmt>
        </Document>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var transactions = await _sut.ParseAsync(stream, _bankAccountId);

        transactions[0].IdempotencyKey.Should().NotBeNullOrWhiteSpace();
    }
}
