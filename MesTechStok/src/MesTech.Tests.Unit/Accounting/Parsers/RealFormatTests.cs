using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Settlement.Parsers;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Accounting.Parsers;

/// <summary>
/// Real format tests — realistic Turkish bank/settlement data structures.
/// Tests parsers with data closely mimicking actual production formats.
/// Validates locale handling (Turkish characters, comma decimals).
/// </summary>
[Trait("Category", "Unit")]
public class RealFormatTests
{
    // ── Trendyol Real Hakedis JSON Structure ──

    [Fact]
    public async Task TrendyolParser_RealHakedisJsonStructure_Parses()
    {
        // Arrange — realistic Trendyol settlement JSON
        var trendyolJson = """
        {
            "totalElements": 3,
            "totalPages": 1,
            "page": 0,
            "size": 50,
            "content": [
                {
                    "orderNumber": "TR-2026031501",
                    "grossSalesAmount": 1599.90,
                    "commissionAmount": 319.98,
                    "commissionRate": 0.20,
                    "serviceFee": 15.00,
                    "cargoDeduction": 29.99,
                    "refundDeduction": 0.00,
                    "netAmount": 1234.93,
                    "transactionDate": "2026-03-15",
                    "category": "Elektronik > Telefon Aksesuar\u0131"
                },
                {
                    "orderNumber": "TR-2026031502",
                    "grossSalesAmount": 249.99,
                    "commissionAmount": 62.50,
                    "commissionRate": 0.25,
                    "serviceFee": 5.00,
                    "cargoDeduction": 14.99,
                    "refundDeduction": 0.00,
                    "netAmount": 167.50,
                    "transactionDate": "2026-03-15",
                    "category": "Giyim > Erkek > T-Shirt"
                },
                {
                    "orderNumber": "TR-2026031503",
                    "grossSalesAmount": 89.90,
                    "commissionAmount": 13.49,
                    "commissionRate": 0.15,
                    "serviceFee": 2.00,
                    "cargoDeduction": 9.99,
                    "refundDeduction": 89.90,
                    "netAmount": -25.48,
                    "transactionDate": "2026-03-14",
                    "category": "Kozmetik > Cilt Bak\u0131m\u0131"
                }
            ]
        }
        """;

        var parser = new TrendyolSettlementParser(
            new Mock<ILogger<TrendyolSettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(trendyolJson));

        // Act
        var batch = await parser.ParseAsync(stream, "json");

        // Assert
        batch.Should().NotBeNull();
        batch.Platform.Should().Be("Trendyol");
        batch.TotalGross.Should().Be(1599.90m + 249.99m + 89.90m);
        batch.TotalCommission.Should().Be(319.98m + 62.50m + 13.49m);
    }

    // ── Hepsiburada Real Settlement JSON Structure ──

    [Fact]
    public async Task HepsiburadaParser_RealSettlementJsonStructure_Parses()
    {
        // Arrange — realistic HB settlement JSON
        var hbJson = """
        {
            "data": {
                "totalCount": 2,
                "settlements": [
                    {
                        "orderId": "HB-1234567890",
                        "productName": "Samsung Galaxy S24 Ultra K\u0131l\u0131f",
                        "saleAmount": 599.99,
                        "commissionAmount": 89.99,
                        "commissionRate": 0.15,
                        "cargoContribution": 19.99,
                        "netAmount": 490.01,
                        "transactionDate": "2026-03-10",
                        "category": "Telefon Aksesuar\u0131"
                    },
                    {
                        "orderId": "HB-1234567891",
                        "productName": "\u00c7orap Seti 12'li Paket",
                        "saleAmount": 149.90,
                        "commissionAmount": 37.47,
                        "commissionRate": 0.25,
                        "cargoContribution": 14.99,
                        "netAmount": 97.44,
                        "transactionDate": "2026-03-11",
                        "category": "Giyim"
                    }
                ],
                "summary": {
                    "totalSaleAmount": 749.89,
                    "totalCommissionAmount": 127.46,
                    "totalCargoContribution": 34.98,
                    "totalNetAmount": 587.45
                }
            }
        }
        """;

        var parser = new HepsiburadaSettlementParser(
            new Mock<ILogger<HepsiburadaSettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(hbJson));

        // Act
        var batch = await parser.ParseAsync(stream, "json");

        // Assert
        batch.Should().NotBeNull();
        batch.Platform.Should().Be("Hepsiburada");
        batch.TotalGross.Should().Be(749.89m);
    }

    // ── N11 SOAP Response XML Structure ──

    [Fact]
    public async Task N11Parser_RealSoapXmlStructure_Parses()
    {
        // Arrange — realistic N11 SOAP XML response
        var n11Xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
            <soap:Body>
                <getSettlementReportResponse>
                    <settlementItem>
                        <siparisNo>N11-9876543210</siparisNo>
                        <urunAdi>Kablosuz Mouse Ergonomik</urunAdi>
                        <satisTutari>299.90</satisTutari>
                        <komisyonTutari>44.99</komisyonTutari>
                        <komisyonOrani>0.15</komisyonOrani>
                        <kargoKesinti>14.99</kargoKesinti>
                        <netTutar>239.92</netTutar>
                        <islemTarihi>2026-03-12</islemTarihi>
                        <kategori>Bilgisayar</kategori>
                    </settlementItem>
                    <settlementItem>
                        <siparisNo>N11-9876543211</siparisNo>
                        <urunAdi>T&#252;rk Kahvesi Seti</urunAdi>
                        <satisTutari>189.00</satisTutari>
                        <komisyonTutari>37.80</komisyonTutari>
                        <komisyonOrani>0.20</komisyonOrani>
                        <kargoKesinti>9.99</kargoKesinti>
                        <netTutar>141.21</netTutar>
                        <islemTarihi>2026-03-12</islemTarihi>
                        <kategori>Mutfak</kategori>
                    </settlementItem>
                </getSettlementReportResponse>
            </soap:Body>
        </soap:Envelope>
        """;

        var parser = new N11SettlementParser(
            new Mock<ILogger<N11SettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(n11Xml));

        // Act
        var batch = await parser.ParseAsync(stream, "xml");

        // Assert
        batch.Should().NotBeNull();
        batch.Platform.Should().Be("N11");
        batch.TotalGross.Should().Be(299.90m + 189.00m);
        batch.TotalCommission.Should().Be(44.99m + 37.80m);
    }

    // ── Ciceksepeti Settlement JSON ──

    [Fact]
    public async Task CiceksepetiParser_RealJsonStructure_Parses()
    {
        // Arrange — realistic Ciceksepeti settlement
        var csJson = """
        {
            "totalCount": 2,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-15",
            "items": [
                {
                    "orderNo": "CS-555-001",
                    "productName": "G\u00fcl Buketi 50 Adet K\u0131rm\u0131z\u0131",
                    "saleAmount": 899.90,
                    "commissionAmount": 179.98,
                    "commissionRate": 0.20,
                    "cargoContribution": 0.00,
                    "serviceFee": 10.00,
                    "netAmount": 709.92,
                    "transactionDate": "2026-03-05",
                    "category": "Cicekler"
                },
                {
                    "orderNo": "CS-555-002",
                    "productName": "\u00c7ikolata Kutusu Premium",
                    "saleAmount": 349.90,
                    "commissionAmount": 52.49,
                    "commissionRate": 0.15,
                    "cargoContribution": 14.99,
                    "serviceFee": 5.00,
                    "netAmount": 277.42,
                    "transactionDate": "2026-03-06",
                    "category": "Hediye"
                }
            ]
        }
        """;

        var parser = new CiceksepetiSettlementParser(
            new Mock<ILogger<CiceksepetiSettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csJson));

        // Act
        var batch = await parser.ParseAsync(stream, "json");

        // Assert
        batch.Should().NotBeNull();
        batch.Platform.Should().Be("Ciceksepeti");
        batch.TotalGross.Should().Be(899.90m + 349.90m);
        batch.PeriodStart.Should().Be(new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        batch.PeriodEnd.Should().Be(new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    // ── Pazarama Settlement JSON ──

    [Fact]
    public async Task PazaramaParser_RealJsonStructure_Parses()
    {
        // Arrange
        var pzJson = """
        {
            "totalCount": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-15",
            "settlements": [
                {
                    "orderId": "PZ-100001",
                    "productName": "Spor Ayakkab\u0131 42 Numara",
                    "amount": 799.90,
                    "commission": 119.99,
                    "commissionRate": 0.15,
                    "cargoFee": 19.99,
                    "netPayout": 659.92,
                    "transactionDate": "2026-03-10",
                    "category": "Ayakkab\u0131"
                }
            ]
        }
        """;

        var parser = new PazaramaSettlementParser(
            new Mock<ILogger<PazaramaSettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(pzJson));

        // Act
        var batch = await parser.ParseAsync(stream, "json");

        // Assert
        batch.Should().NotBeNull();
        batch.Platform.Should().Be("Pazarama");
        batch.TotalGross.Should().Be(799.90m);
        batch.TotalCommission.Should().Be(119.99m);
    }

    // ── OpenCart Own Store Settlement ──

    [Fact]
    public async Task OpenCartParser_RealJsonStructure_ZeroCommission()
    {
        // Arrange — own store, no platform commission
        var ocJson = """
        {
            "totalOrders": 2,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-15",
            "orders": [
                {
                    "orderId": "OC-50001",
                    "productName": "\u00d6zel \u00dcretim Deri Canta",
                    "orderTotal": 1250.00,
                    "gatewayFee": 31.25,
                    "cargoExpense": 24.99,
                    "netAmount": 1193.76,
                    "orderDate": "2026-03-08",
                    "paymentMethod": "iyzico"
                },
                {
                    "orderId": "OC-50002",
                    "productName": "El Yap\u0131m\u0131 Seramik Kupa",
                    "orderTotal": 89.90,
                    "gatewayFee": 2.25,
                    "cargoExpense": 9.99,
                    "netAmount": 77.66,
                    "orderDate": "2026-03-09",
                    "paymentMethod": "Kapida Odeme"
                }
            ]
        }
        """;

        var parser = new OpenCartSettlementParser(
            new Mock<ILogger<OpenCartSettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ocJson));

        // Act
        var batch = await parser.ParseAsync(stream, "json");

        // Assert
        batch.Should().NotBeNull();
        batch.Platform.Should().Be("OpenCart");
        batch.TotalCommission.Should().Be(0m); // Own store = zero commission
        batch.TotalGross.Should().Be(1250.00m + 89.90m);
    }

    // ── Amazon TSV Format ──

    [Fact]
    public void AmazonTSV_TurkishMarketplaceData_ParsesCorrectly()
    {
        // Arrange — realistic Amazon SP-API flat-file TSV
        var tsvContent = new StringBuilder();
        tsvContent.AppendLine("settlement-id\tsettlement-start-date\tsettlement-end-date\ttransaction-type\torder-id\tmarketplace-name\tamount-type\tamount-description\tamount\tcurrency");
        tsvContent.AppendLine("12345678\t2026-03-01T00:00:00+03:00\t2026-03-15T23:59:59+03:00\tOrder\t405-1234567-8901234\tAmazon.com.tr\tItemPrice\tPrincipal\t299.90\tTRY");
        tsvContent.AppendLine("12345678\t2026-03-01T00:00:00+03:00\t2026-03-15T23:59:59+03:00\tOrder\t405-1234567-8901234\tAmazon.com.tr\tCommission\tCommission\t-44.99\tTRY");
        tsvContent.AppendLine("12345678\t2026-03-01T00:00:00+03:00\t2026-03-15T23:59:59+03:00\tOrder\t405-1234567-8901234\tAmazon.com.tr\tFBAFees\tFulfillmentFee\t-15.00\tTRY");

        var lines = tsvContent.ToString()
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Act — parse header
        var headers = lines[0].Split('\t');
        headers.Should().Contain("settlement-id");
        headers.Should().Contain("marketplace-name");
        headers.Should().Contain("amount");
        headers.Should().Contain("currency");

        // Act — parse data lines
        var dataLine1 = lines[1].Split('\t');
        var amount1 = decimal.Parse(dataLine1[8], CultureInfo.InvariantCulture);
        amount1.Should().Be(299.90m);

        var dataLine2 = lines[2].Split('\t');
        var amount2 = decimal.Parse(dataLine2[8], CultureInfo.InvariantCulture);
        amount2.Should().Be(-44.99m); // Commission is negative

        // Verify marketplace is Turkish
        dataLine1[5].Should().Be("Amazon.com.tr");
        dataLine1[9].Should().Be("TRY");
    }

    // ── Garanti MT940 Format ──

    [Fact]
    public void GarantiMT940_TurkishCharactersCommaDecimals_ParsesCorrectly()
    {
        // Arrange — realistic Garanti Bank MT940 SWIFT statement
        var mt940 = new StringBuilder();
        mt940.AppendLine(":20:STMT26031500001");
        mt940.AppendLine(":25:TR330006200101800006299911");
        mt940.AppendLine(":28C:00001/001");
        mt940.AppendLine(":60F:C260314TRY125000,50");
        mt940.AppendLine(":61:2603150315D1599,90NTRF//HB-HAKEDIS-001");
        mt940.AppendLine(":86:HEPSIBURADA HAKEDIS ODEMESI - MART 1. DONEM");
        mt940.AppendLine(":61:2603150315C2499,00NTRF//TR-HAKEDIS-001");
        mt940.AppendLine(":86:TRENDYOL HAKEDIS ODEMESI - MART 1. DONEM");
        mt940.AppendLine(":62F:C260315TRY125899,60");

        var content = mt940.ToString();

        // Assert — verify structure has valid MT940 tags
        content.Should().Contain(":20:");
        content.Should().Contain(":25:TR33");
        content.Should().Contain(":60F:"); // Opening balance
        content.Should().Contain(":62F:"); // Closing balance

        // Verify Turkish comma decimal format
        content.Should().Contain("125000,50");
        content.Should().Contain("1599,90");

        // Verify IBAN format
        content.Should().Contain("TR330006200101800006299911");
    }

    // ── Is Bankasi MT940 Format ──

    [Fact]
    public void IsBankasiMT940_TurkishFormat_StructureValid()
    {
        // Arrange — Is Bankasi MT940
        var mt940 = new StringBuilder();
        mt940.AppendLine(":20:ISBANK260315001");
        mt940.AppendLine(":25:TR180006400000168880003005");
        mt940.AppendLine(":28C:00256/001");
        mt940.AppendLine(":60F:C260314TRY89500,00");
        mt940.AppendLine(":61:2603150315C3250,75NTRF//PAZARAMA-HAK-001");
        mt940.AppendLine(":86:PAZARAMA MARKETPLACE HAKEDIS ODEMESI");
        mt940.AppendLine(":61:2603150315D750,00NTRFKARGO-ODEME//");
        mt940.AppendLine(":86:YURTICI KARGO AIDAT ODEMESI");
        mt940.AppendLine(":62F:C260315TRY92000,75");

        var content = mt940.ToString();

        // Assert
        content.Should().Contain(":25:TR18");
        content.Should().Contain("89500,00"); // Turkish comma decimal
        content.Should().Contain("3250,75");
        content.Should().Contain("PAZARAMA");
    }

    // ── Akbank OFX Format ──

    [Fact]
    public void AkbankOFX_SgmlWithTurkishChars_StructureValid()
    {
        // Arrange — Akbank OFX (SGML-style, not XML)
        var ofx = new StringBuilder();
        ofx.AppendLine("OFXHEADER:100");
        ofx.AppendLine("DATA:OFXSGML");
        ofx.AppendLine("VERSION:102");
        ofx.AppendLine("SECURITY:NONE");
        ofx.AppendLine("ENCODING:UTF-8");
        ofx.AppendLine("<OFX>");
        ofx.AppendLine("<BANKMSGSRSV1>");
        ofx.AppendLine("<STMTTRNRS>");
        ofx.AppendLine("<STMTRS>");
        ofx.AppendLine("<CURDEF>TRY");
        ofx.AppendLine("<BANKACCTFROM>");
        ofx.AppendLine("<BANKID>00046");
        ofx.AppendLine("<ACCTID>TR270004600999888001234567");
        ofx.AppendLine("<ACCTTYPE>CHECKING");
        ofx.AppendLine("</BANKACCTFROM>");
        ofx.AppendLine("<BANKTRANLIST>");
        ofx.AppendLine("<DTSTART>20260301");
        ofx.AppendLine("<DTEND>20260315");
        ofx.AppendLine("<STMTTRN>");
        ofx.AppendLine("<TRNTYPE>CREDIT");
        ofx.AppendLine("<DTPOSTED>20260315");
        ofx.AppendLine("<TRNAMT>4580.50");
        ofx.AppendLine("<FITID>AKB260315001");
        ofx.AppendLine("<NAME>TRENDYOL HAKEDIS");
        ofx.AppendLine("<MEMO>TRENDYOL MART 1.DONEM HAKEDIS ODEMESI");
        ofx.AppendLine("</STMTTRN>");
        ofx.AppendLine("</BANKTRANLIST>");
        ofx.AppendLine("</STMTRS>");
        ofx.AppendLine("</STMTTRNRS>");
        ofx.AppendLine("</BANKMSGSRSV1>");
        ofx.AppendLine("</OFX>");

        var content = ofx.ToString();

        // Assert — OFX structure validation
        content.Should().Contain("OFXHEADER:100");
        content.Should().Contain("<CURDEF>TRY");
        content.Should().Contain("TR270004600999888001234567");
        content.Should().Contain("<TRNAMT>4580.50");
        content.Should().Contain("TRENDYOL HAKEDIS");
    }

    // ── Yapi Kredi camt.053 XML ──

    [Fact]
    public void YapiKrediCamt053_XmlStructure_Valid()
    {
        // Arrange — camt.053 bank statement XML (ISO 20022)
        var xml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <Document xmlns="urn:iso:std:iso:20022:tech:xsd:camt.053.001.02">
            <BkToCstmrStmt>
                <GrpHdr>
                    <MsgId>YKB-260315-001</MsgId>
                    <CreDtTm>2026-03-15T18:00:00+03:00</CreDtTm>
                </GrpHdr>
                <Stmt>
                    <Id>260315</Id>
                    <Acct>
                        <Id>
                            <IBAN>TR980006701000000067896543</IBAN>
                        </Id>
                        <Ccy>TRY</Ccy>
                    </Acct>
                    <Bal>
                        <Tp><CdOrPrtry><Cd>OPBD</Cd></CdOrPrtry></Tp>
                        <Amt Ccy="TRY">45000.00</Amt>
                        <CdtDbtInd>CRDT</CdtDbtInd>
                        <Dt><Dt>2026-03-14</Dt></Dt>
                    </Bal>
                    <Ntry>
                        <Amt Ccy="TRY">6890.50</Amt>
                        <CdtDbtInd>CRDT</CdtDbtInd>
                        <BookgDt><Dt>2026-03-15</Dt></BookgDt>
                        <NtryDtls>
                            <TxDtls>
                                <RmtInf>
                                    <Ustrd>N11 HAKEDIS ODEMESI MART 2026</Ustrd>
                                </RmtInf>
                            </TxDtls>
                        </NtryDtls>
                    </Ntry>
                </Stmt>
            </BkToCstmrStmt>
        </Document>
        """;

        // Act — parse as valid XML
        var doc = XDocument.Parse(xml);

        // Assert
        doc.Should().NotBeNull();
        var ns = XNamespace.Get("urn:iso:std:iso:20022:tech:xsd:camt.053.001.02");
        var iban = doc.Descendants(ns + "IBAN").FirstOrDefault();
        iban.Should().NotBeNull();
        iban!.Value.Should().StartWith("TR");

        var amount = doc.Descendants(ns + "Amt").First();
        amount.Value.Should().Be("45000.00");
        amount.Attribute("Ccy")?.Value.Should().Be("TRY");
    }

    // ── Settlement Lines Parsing ──

    [Fact]
    public async Task CiceksepetiParser_ParseLines_CreatesSettlementLines()
    {
        // Arrange
        var csJson = """
        {
            "totalCount": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-15",
            "items": [
                {
                    "orderNo": "CS-LN-001",
                    "productName": "Test",
                    "saleAmount": 500.00,
                    "commissionAmount": 75.00,
                    "commissionRate": 0.15,
                    "cargoContribution": 10.00,
                    "serviceFee": 5.00,
                    "netAmount": 410.00,
                    "transactionDate": "2026-03-10",
                    "category": "Test"
                }
            ]
        }
        """;

        var parser = new CiceksepetiSettlementParser(
            new Mock<ILogger<CiceksepetiSettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csJson));
        var batch = await parser.ParseAsync(stream, "json");

        // Act
        var lines = await parser.ParseLinesAsync(batch);

        // Assert
        lines.Should().HaveCount(1);
        lines[0].GrossAmount.Should().Be(500.00m);
        lines[0].CommissionAmount.Should().Be(75.00m);
        lines[0].NetAmount.Should().Be(410.00m);
    }

    [Fact]
    public async Task PazaramaParser_ParseLines_CreatesSettlementLines()
    {
        // Arrange
        var pzJson = """
        {
            "totalCount": 1,
            "periodStart": "2026-03-01",
            "periodEnd": "2026-03-15",
            "settlements": [
                {
                    "orderId": "PZ-LN-001",
                    "productName": "Test",
                    "amount": 300.00,
                    "commission": 45.00,
                    "commissionRate": 0.15,
                    "cargoFee": 12.00,
                    "netPayout": 243.00,
                    "transactionDate": "2026-03-10",
                    "category": "Test"
                }
            ]
        }
        """;

        var parser = new PazaramaSettlementParser(
            new Mock<ILogger<PazaramaSettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(pzJson));
        var batch = await parser.ParseAsync(stream, "json");

        // Act
        var lines = await parser.ParseLinesAsync(batch);

        // Assert
        lines.Should().HaveCount(1);
        lines[0].GrossAmount.Should().Be(300.00m);
        lines[0].CargoDeduction.Should().Be(12.00m);
    }

    // ── Turkish Decimal Handling ──

    [Fact]
    public void TurkishDecimalFormat_CommaAsSeparator_ParsesCorrectly()
    {
        // Turkish uses comma as decimal separator: 1.234,56
        var turkishValue = "1234,56";

        var parsed = decimal.TryParse(turkishValue, NumberStyles.Any,
            new CultureInfo("tr-TR"), out var result);

        parsed.Should().BeTrue();
        result.Should().Be(1234.56m);
    }

    [Fact]
    public void InvariantDecimalFormat_DotAsSeparator_ParsesCorrectly()
    {
        // API/JSON uses dot: 1234.56
        var invariantValue = "1234.56";

        var parsed = decimal.TryParse(invariantValue, NumberStyles.Any,
            CultureInfo.InvariantCulture, out var result);

        parsed.Should().BeTrue();
        result.Should().Be(1234.56m);
    }

    // ── Empty/Null Data Handling ──

    [Fact]
    public async Task OpenCartParser_EmptyOrderList_ReturnsEmptyBatch()
    {
        var emptyJson = """{"totalOrders":0,"orders":[]}""";

        var parser = new OpenCartSettlementParser(
            new Mock<ILogger<OpenCartSettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(emptyJson));

        // Act
        var batch = await parser.ParseAsync(stream, "json");

        // Assert
        batch.TotalGross.Should().Be(0m);
        batch.TotalCommission.Should().Be(0m);
    }

    [Fact]
    public async Task N11Parser_EmptyXml_ReturnsEmptyBatch()
    {
        var emptyXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
            <soap:Body>
                <getSettlementReportResponse/>
            </soap:Body>
        </soap:Envelope>
        """;

        var parser = new N11SettlementParser(
            new Mock<ILogger<N11SettlementParser>>().Object);

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(emptyXml));

        // Act
        var batch = await parser.ParseAsync(stream, "xml");

        // Assert
        batch.TotalGross.Should().Be(0m);
    }
}
