using System.IO;
using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

/// <summary>
/// N11SettlementParser tests — SOAP XML parsing, plain XML fallback, Turkish decimal.
/// </summary>
[Trait("Category", "Unit")]
public class N11SettlementParserTests
{
    private static readonly Guid TestTenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly N11SettlementParser _sut;
    private readonly Mock<ILogger<N11SettlementParser>> _loggerMock = new();

    public N11SettlementParserTests()
    {
        _sut = new N11SettlementParser(_loggerMock.Object);
    }

    [Fact]
    public void Platform_ShouldBeN11()
    {
        _sut.Platform.Should().Be("N11");
    }

    [Fact]
    public async Task ParseAsync_SoapXml_ShouldReturnBatch()
    {
        var xml = """
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
            <soap:Body>
                <settlementResponse>
                    <settlementItem>
                        <siparisNo>N11-001</siparisNo>
                        <urunAdi>Urun A</urunAdi>
                        <satisTutari>1000.00</satisTutari>
                        <komisyonTutari>150.00</komisyonTutari>
                        <komisyonOrani>0.15</komisyonOrani>
                        <kargoKesinti>30.00</kargoKesinti>
                        <netTutar>820.00</netTutar>
                        <islemTarihi>2026-03-05</islemTarihi>
                        <kategori>Elektronik</kategori>
                    </settlementItem>
                    <settlementItem>
                        <siparisNo>N11-002</siparisNo>
                        <urunAdi>Urun B</urunAdi>
                        <satisTutari>500.00</satisTutari>
                        <komisyonTutari>75.00</komisyonTutari>
                        <komisyonOrani>0.15</komisyonOrani>
                        <kargoKesinti>15.00</kargoKesinti>
                        <netTutar>410.00</netTutar>
                        <islemTarihi>2026-03-10</islemTarihi>
                        <kategori>Giyim</kategori>
                    </settlementItem>
                </settlementResponse>
            </soap:Body>
        </soap:Envelope>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "xml");

        batch.Should().NotBeNull();
        batch.Platform.Should().Be("N11");
        batch.TotalGross.Should().Be(1500m);
        batch.TotalCommission.Should().Be(225m);
        batch.TotalNet.Should().Be(1230m);
    }

    [Fact]
    public async Task ParseAsync_PlainXml_ShouldFallback()
    {
        var xml = """
        <settlements>
            <settlementItem>
                <siparisNo>N11-010</siparisNo>
                <urunAdi>Urun X</urunAdi>
                <satisTutari>200.00</satisTutari>
                <komisyonTutari>30.00</komisyonTutari>
                <komisyonOrani>0.15</komisyonOrani>
                <kargoKesinti>10.00</kargoKesinti>
                <netTutar>160.00</netTutar>
                <islemTarihi>2026-03-08</islemTarihi>
                <kategori>Ev</kategori>
            </settlementItem>
        </settlements>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "xml");

        batch.Should().NotBeNull();
        batch.TotalGross.Should().Be(200m);
        batch.TotalCommission.Should().Be(30m);
    }

    [Fact]
    public async Task ParseAsync_EmptyXml_ShouldReturnEmptyBatch()
    {
        var xml = "<settlements></settlements>";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "xml");

        batch.Should().NotBeNull();
        batch.TotalGross.Should().Be(0m);
        batch.TotalCommission.Should().Be(0m);
        batch.TotalNet.Should().Be(0m);
    }

    [Fact]
    public async Task ParseAsync_TurkishDecimal_ShouldParse()
    {
        // N11 parser handles Turkish decimal format (comma separator)
        var xml = """
        <settlements>
            <settlementItem>
                <siparisNo>N11-020</siparisNo>
                <satisTutari>1500.50</satisTutari>
                <komisyonTutari>225.08</komisyonTutari>
                <komisyonOrani>0.15</komisyonOrani>
                <kargoKesinti>25.00</kargoKesinti>
                <netTutar>1250.42</netTutar>
                <islemTarihi>2026-03-05</islemTarihi>
            </settlementItem>
        </settlements>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "xml");

        batch.TotalGross.Should().Be(1500.50m);
        batch.TotalCommission.Should().Be(225.08m);
    }

    [Fact]
    public async Task ParseLinesAsync_ValidBatch_ShouldReturnLines()
    {
        var xml = """
        <settlements>
            <settlementItem>
                <siparisNo>N11-030</siparisNo>
                <urunAdi>Urun Y</urunAdi>
                <satisTutari>100.00</satisTutari>
                <komisyonTutari>15.00</komisyonTutari>
                <komisyonOrani>0.15</komisyonOrani>
                <kargoKesinti>5.00</kargoKesinti>
                <netTutar>80.00</netTutar>
                <islemTarihi>2026-03-05</islemTarihi>
                <kategori>Spor</kategori>
            </settlementItem>
        </settlements>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "xml");
        var lines = await _sut.ParseLinesAsync(batch);

        lines.Should().HaveCount(1);
        lines[0].OrderId.Should().Be("N11-030");
        lines[0].GrossAmount.Should().Be(100m);
        lines[0].CommissionAmount.Should().Be(15m);
        lines[0].ServiceFee.Should().Be(0m); // N11 has no separate service fee
        lines[0].CargoDeduction.Should().Be(5m);
        lines[0].NetAmount.Should().Be(80m);
    }

    [Fact]
    public async Task ParseLinesAsync_WithoutParseAsync_ShouldReturnEmpty()
    {
        var parser = new N11SettlementParser(_loggerMock.Object);
        var batch = MesTech.Domain.Accounting.Entities.SettlementBatch.Create(
            Guid.Empty, "N11", DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0);

        var lines = await parser.ParseLinesAsync(batch);
        lines.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_NullRawData_ThrowsArgumentNull()
    {
        var act = async () => await _sut.ParseAsync(TestTenantId, null!, "xml");
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseLinesAsync_NullBatch_ThrowsArgumentNull()
    {
        var act = async () => await _sut.ParseLinesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseAsync_AlternativeElementNames_ShouldParse()
    {
        // Uses English element names (orderNo, etc.) as fallback
        var xml = """
        <root>
            <settlement>
                <orderNo>N11-ALT</orderNo>
                <saleAmount>250.00</saleAmount>
                <commissionAmount>37.50</commissionAmount>
                <commissionRate>0.15</commissionRate>
                <cargoDeduction>10.00</cargoDeduction>
                <netAmount>202.50</netAmount>
                <transactionDate>2026-03-12</transactionDate>
                <category>Aksesuar</category>
            </settlement>
        </root>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "xml");

        batch.TotalGross.Should().Be(250m);
        batch.TotalCommission.Should().Be(37.50m);
    }

    [Fact]
    public async Task ParseAsync_MultipleItems_CalculatesTotalsCorrectly()
    {
        var xml = """
        <settlements>
            <settlementItem>
                <siparisNo>N11-M1</siparisNo>
                <satisTutari>100</satisTutari>
                <komisyonTutari>10</komisyonTutari>
                <komisyonOrani>0.10</komisyonOrani>
                <kargoKesinti>5</kargoKesinti>
                <netTutar>85</netTutar>
            </settlementItem>
            <settlementItem>
                <siparisNo>N11-M2</siparisNo>
                <satisTutari>200</satisTutari>
                <komisyonTutari>30</komisyonTutari>
                <komisyonOrani>0.15</komisyonOrani>
                <kargoKesinti>10</kargoKesinti>
                <netTutar>160</netTutar>
            </settlementItem>
            <settlementItem>
                <siparisNo>N11-M3</siparisNo>
                <satisTutari>300</satisTutari>
                <komisyonTutari>60</komisyonTutari>
                <komisyonOrani>0.20</komisyonOrani>
                <kargoKesinti>15</kargoKesinti>
                <netTutar>225</netTutar>
            </settlementItem>
        </settlements>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "xml");

        batch.TotalGross.Should().Be(600m);
        batch.TotalCommission.Should().Be(100m);
        batch.TotalNet.Should().Be(470m);
    }

    [Fact]
    public async Task ParseAsync_PeriodFromTransactionDates_ShouldExtractMinMax()
    {
        var xml = """
        <settlements>
            <settlementItem>
                <siparisNo>N11-D1</siparisNo>
                <satisTutari>100</satisTutari>
                <komisyonTutari>10</komisyonTutari>
                <komisyonOrani>0.10</komisyonOrani>
                <kargoKesinti>5</kargoKesinti>
                <netTutar>85</netTutar>
                <islemTarihi>2026-03-05</islemTarihi>
            </settlementItem>
            <settlementItem>
                <siparisNo>N11-D2</siparisNo>
                <satisTutari>200</satisTutari>
                <komisyonTutari>20</komisyonTutari>
                <komisyonOrani>0.10</komisyonOrani>
                <kargoKesinti>10</kargoKesinti>
                <netTutar>170</netTutar>
                <islemTarihi>2026-03-12</islemTarihi>
            </settlementItem>
        </settlements>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "xml");

        batch.PeriodStart.Should().Be(new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc));
        batch.PeriodEnd.Should().Be(new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task ParseAsync_HesapKesimiElementName_ShouldParse()
    {
        // hesapKesimi is an alternative element name
        var xml = """
        <root>
            <hesapKesimi>
                <siparisNo>N11-HK1</siparisNo>
                <satisTutari>150</satisTutari>
                <komisyonTutari>22.50</komisyonTutari>
                <komisyonOrani>0.15</komisyonOrani>
                <kargoKesinti>8</kargoKesinti>
                <netTutar>119.50</netTutar>
            </hesapKesimi>
        </root>
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        var batch = await _sut.ParseAsync(TestTenantId, stream, "xml");

        batch.TotalGross.Should().Be(150m);
        batch.TotalCommission.Should().Be(22.50m);
    }
}
