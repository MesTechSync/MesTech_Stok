using System.Text;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Invoice;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// HH-DEV5-021: UBL-TR 1.2.1 schema validation tests.
/// Tests UblTrXmlValidator against GİB mandatory field requirements.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "UblTrValidation")]
[Trait("Phase", "Dalga15")]
public class UblTrXmlValidatorTests
{
    private readonly UblTrXmlValidator _sut = new();

    private static byte[] ToBytes(string xml) => Encoding.UTF8.GetBytes(xml);

    /// <summary>Helper: minimal valid UBL-TR XML with all GİB mandatory fields.</summary>
    private static string BuildValidUblTrXml(
        string ublVersion = "2.1",
        string customizationId = "TR1.2.1",
        string profileId = "TEMELFATURA",
        string id = "MES2026000000001",
        string? uuid = null,
        string issueDate = "2026-04-06",
        string issueTime = "10:00:00",
        string invoiceTypeCode = "SATIS",
        string currencyCode = "TRY",
        string supplierVkn = "1234567890",
        string supplierName = "MesTech A.Ş.",
        string customerVkn = "9876543210",
        string customerName = "Test Müşteri Ltd.",
        bool includeLines = true,
        bool includeTaxTotal = true,
        bool includeMonetary = true)
    {
        uuid ??= Guid.NewGuid().ToString();

        var linesXml = includeLines ? @"
    <cac:InvoiceLine>
        <cbc:ID>1</cbc:ID>
        <cbc:InvoicedQuantity unitCode=""C62"">2</cbc:InvoicedQuantity>
        <cbc:LineExtensionAmount currencyID=""TRY"">200.00</cbc:LineExtensionAmount>
        <cac:Item><cbc:Name>Test Ürün</cbc:Name></cac:Item>
        <cac:Price><cbc:PriceAmount currencyID=""TRY"">100.00</cbc:PriceAmount></cac:Price>
    </cac:InvoiceLine>" : "";

        var taxXml = includeTaxTotal ? @"
    <cac:TaxTotal>
        <cbc:TaxAmount currencyID=""TRY"">36.00</cbc:TaxAmount>
        <cac:TaxSubtotal>
            <cbc:TaxableAmount currencyID=""TRY"">200.00</cbc:TaxableAmount>
            <cbc:TaxAmount currencyID=""TRY"">36.00</cbc:TaxAmount>
            <cac:TaxCategory>
                <cac:TaxScheme>
                    <cbc:TaxTypeCode>0015</cbc:TaxTypeCode>
                    <cbc:Name>KDV</cbc:Name>
                </cac:TaxScheme>
            </cac:TaxCategory>
        </cac:TaxSubtotal>
    </cac:TaxTotal>" : "";

        var monetaryXml = includeMonetary ? @"
    <cac:LegalMonetaryTotal>
        <cbc:LineExtensionAmount currencyID=""TRY"">200.00</cbc:LineExtensionAmount>
        <cbc:TaxExclusiveAmount currencyID=""TRY"">200.00</cbc:TaxExclusiveAmount>
        <cbc:TaxInclusiveAmount currencyID=""TRY"">236.00</cbc:TaxInclusiveAmount>
        <cbc:PayableAmount currencyID=""TRY"">236.00</cbc:PayableAmount>
    </cac:LegalMonetaryTotal>" : "";

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Invoice xmlns=""urn:oasis:names:specification:ubl:schema:xsd:Invoice-2""
         xmlns:cac=""urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2""
         xmlns:cbc=""urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"">
    <cbc:UBLVersionID>{ublVersion}</cbc:UBLVersionID>
    <cbc:CustomizationID>{customizationId}</cbc:CustomizationID>
    <cbc:ProfileID>{profileId}</cbc:ProfileID>
    <cbc:ID>{id}</cbc:ID>
    <cbc:UUID>{uuid}</cbc:UUID>
    <cbc:IssueDate>{issueDate}</cbc:IssueDate>
    <cbc:IssueTime>{issueTime}</cbc:IssueTime>
    <cbc:InvoiceTypeCode>{invoiceTypeCode}</cbc:InvoiceTypeCode>
    <cbc:DocumentCurrencyCode>{currencyCode}</cbc:DocumentCurrencyCode>
    <cac:AccountingSupplierParty>
        <cac:Party>
            <cac:PartyIdentification><cbc:ID>{supplierVkn}</cbc:ID></cac:PartyIdentification>
            <cac:PartyName><cbc:Name>{supplierName}</cbc:Name></cac:PartyName>
        </cac:Party>
    </cac:AccountingSupplierParty>
    <cac:AccountingCustomerParty>
        <cac:Party>
            <cac:PartyIdentification><cbc:ID>{customerVkn}</cbc:ID></cac:PartyIdentification>
            <cac:PartyName><cbc:Name>{customerName}</cbc:Name></cac:PartyName>
        </cac:Party>
    </cac:AccountingCustomerParty>{taxXml}{monetaryXml}{linesXml}
</Invoice>";
    }

    // ═══════════════════════════════════════════
    // Valid XML Tests
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_ValidXml_ReturnsNoErrors()
    {
        var xml = BuildValidUblTrXml();

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().BeEmpty("fully valid UBL-TR XML should pass all checks");
    }

    [Theory]
    [InlineData("TEMELFATURA")]
    [InlineData("TICARIFATURA")]
    [InlineData("EARSIVFATURA")]
    public async Task ValidateAsync_ValidProfileIds_ReturnsNoErrors(string profileId)
    {
        var xml = BuildValidUblTrXml(profileId: profileId);

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().BeEmpty($"ProfileID '{profileId}' is a valid GİB value");
    }

    [Theory]
    [InlineData("SATIS")]
    [InlineData("IADE")]
    [InlineData("TEVKIFAT")]
    [InlineData("ISTISNA")]
    public async Task ValidateAsync_ValidInvoiceTypeCodes_ReturnsNoErrors(string typeCode)
    {
        var xml = BuildValidUblTrXml(invoiceTypeCode: typeCode);

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().BeEmpty($"InvoiceTypeCode '{typeCode}' is a valid GİB value");
    }

    // ═══════════════════════════════════════════
    // UBLVersionID Validation
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_WrongUblVersion_ReturnsError()
    {
        var xml = BuildValidUblTrXml(ublVersion: "3.0");

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().Contain(e => e.Contains("UBLVersionID") && e.Contains("2.1"));
    }

    // ═══════════════════════════════════════════
    // CustomizationID Validation
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_InvalidCustomizationId_ReturnsError()
    {
        var xml = BuildValidUblTrXml(customizationId: "EN1.0");

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().Contain(e => e.Contains("CustomizationID") && e.Contains("TR1.2"));
    }

    // ═══════════════════════════════════════════
    // ProfileID Validation
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_InvalidProfileId_ReturnsError()
    {
        var xml = BuildValidUblTrXml(profileId: "UNKNOWN");

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().Contain(e => e.Contains("ProfileID") && e.Contains("geçersiz"));
    }

    // ═══════════════════════════════════════════
    // UUID Validation
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_InvalidUuid_ReturnsError()
    {
        var xml = BuildValidUblTrXml(uuid: "NOT-A-GUID");

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().Contain(e => e.Contains("UUID") && e.Contains("GUID"));
    }

    // ═══════════════════════════════════════════
    // InvoiceTypeCode Validation
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_InvalidInvoiceTypeCode_ReturnsError()
    {
        var xml = BuildValidUblTrXml(invoiceTypeCode: "INVALID");

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().Contain(e => e.Contains("InvoiceTypeCode") && e.Contains("geçersiz"));
    }

    // ═══════════════════════════════════════════
    // VKN/TCKN Validation
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_InvalidSupplierVkn_ReturnsError()
    {
        var xml = BuildValidUblTrXml(supplierVkn: "123"); // Too short

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().Contain(e => e.Contains("Satıcı") && e.Contains("VKN"));
    }

    [Fact]
    public async Task ValidateAsync_TcknWith11Digits_ReturnsNoError()
    {
        var xml = BuildValidUblTrXml(customerVkn: "12345678901"); // 11-digit TCKN

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().NotContain(e => e.Contains("Alıcı") && e.Contains("VKN"));
    }

    // ═══════════════════════════════════════════
    // Missing Sections
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_NoTaxTotal_ReturnsError()
    {
        var xml = BuildValidUblTrXml(includeTaxTotal: false);

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().Contain(e => e.Contains("TaxTotal"));
    }

    [Fact]
    public async Task ValidateAsync_NoLegalMonetaryTotal_ReturnsError()
    {
        var xml = BuildValidUblTrXml(includeMonetary: false);

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().Contain(e => e.Contains("LegalMonetaryTotal"));
    }

    [Fact]
    public async Task ValidateAsync_NoInvoiceLines_ReturnsError()
    {
        var xml = BuildValidUblTrXml(includeLines: false);

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().Contain(e => e.Contains("InvoiceLine"));
    }

    // ═══════════════════════════════════════════
    // Malformed XML
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_MalformedXml_ReturnsParseError()
    {
        var xml = "<not-valid-xml><unclosed>";

        var errors = await _sut.ValidateAsync(ToBytes(xml));

        errors.Should().Contain(e => e.Contains("XML parse"));
    }

    [Fact]
    public async Task ValidateAsync_EmptyXml_ReturnsParseError()
    {
        var errors = await _sut.ValidateAsync(Array.Empty<byte>());

        errors.Should().NotBeEmpty("empty bytes should produce error");
    }
}
