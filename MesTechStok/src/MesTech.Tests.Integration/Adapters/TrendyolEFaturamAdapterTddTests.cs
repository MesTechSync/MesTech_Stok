using FluentAssertions;
using MesTech.Application.Interfaces;

namespace MesTech.Tests.Integration.Adapters;

/// <summary>
/// TrendyolEFaturamAdapter TDD kontrakt testleri — RED.
/// DEV3 H24'te TrendyolEFaturamAdapter implement edilince SKIP kaldırılacak.
/// Bu testler implementasyonun uyması gereken davranış sözleşmesini tanımlar.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Feature", "TrendyolEFaturam")]
[Trait("Phase", "Dalga5")]
public class TrendyolEFaturamAdapterTddTests
{
    // TrendyolEFaturamAdapter DEV3 H24'te implement edilince inject edilecek:
    // private readonly IInvoiceProvider _adapter;
    // public TrendyolEFaturamAdapterTddTests() { _adapter = new TrendyolEFaturamAdapter(...); }

    [Fact(Skip = "TrendyolEFaturamAdapter DEV3 H24 teslim bekliyor")]
    public async Task CreateEFatura_ValidInvoice_ReturnsSuccessWithGibId()
    {
        // Arrange
        var invoice = BuildInvoice("INV-001", "Test Firma A.Ş.", "1234567890");
        IInvoiceProvider adapter = null!; // DEV3 implement edince yerine koyacak

        // Act
        var result = await adapter.CreateEFaturaAsync(invoice);

        // Assert
        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact(Skip = "TrendyolEFaturamAdapter DEV3 H24 teslim bekliyor")]
    public async Task CreateEFatura_InvalidTaxNumber_ReturnsError()
    {
        var invoice = BuildInvoice("INV-002", "Geçersiz Firma", "000");
        IInvoiceProvider adapter = null!;

        var result = await adapter.CreateEFaturaAsync(invoice);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.GibInvoiceId.Should().BeNull();
    }

    [Fact(Skip = "TrendyolEFaturamAdapter DEV3 H24 teslim bekliyor")]
    public async Task CreateEArsiv_ValidInvoice_ReturnsSuccess()
    {
        var invoice = BuildInvoice("INV-003", "Bireysel Müşteri", null);
        IInvoiceProvider adapter = null!;

        var result = await adapter.CreateEArsivAsync(invoice);

        result.Success.Should().BeTrue();
        result.GibInvoiceId.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "TrendyolEFaturamAdapter DEV3 H24 teslim bekliyor")]
    public async Task CheckStatus_ExistingInvoice_ReturnsStatusResult()
    {
        const string gibId = "TRENDYOL-2026-001";
        IInvoiceProvider adapter = null!;

        var result = await adapter.CheckStatusAsync(gibId);

        result.GibInvoiceId.Should().Be(gibId);
        result.Status.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "TrendyolEFaturamAdapter DEV3 H24 teslim bekliyor")]
    public async Task GetPdf_ExistingInvoice_ReturnsNonEmptyByteArray()
    {
        const string gibId = "TRENDYOL-2026-002";
        IInvoiceProvider adapter = null!;

        var pdf = await adapter.GetPdfAsync(gibId);

        pdf.Should().NotBeEmpty();
        // PDF magic bytes: %PDF
        pdf[0].Should().Be(0x25); // '%'
    }

    [Fact(Skip = "TrendyolEFaturamAdapter DEV3 H24 teslim bekliyor")]
    public async Task IsEInvoiceTaxpayer_RegisteredTaxNumber_ReturnsTrue()
    {
        const string registeredTaxNo = "1234567890"; // GİB e-fatura mükellefi
        IInvoiceProvider adapter = null!;

        var result = await adapter.IsEInvoiceTaxpayerAsync(registeredTaxNo);

        result.Should().BeTrue();
    }

    [Fact(Skip = "TrendyolEFaturamAdapter DEV3 H24 teslim bekliyor")]
    public async Task IsEInvoiceTaxpayer_UnregisteredTaxNumber_ReturnsFalse()
    {
        const string unregisteredTaxNo = "9999999999"; // e-arşiv mükellefi
        IInvoiceProvider adapter = null!;

        var result = await adapter.IsEInvoiceTaxpayerAsync(unregisteredTaxNo);

        result.Should().BeFalse();
    }

    [Fact(Skip = "TrendyolEFaturamAdapter DEV3 H24 teslim bekliyor")]
    public async Task CancelInvoice_ValidInvoice_ReturnsSuccess()
    {
        const string gibId = "TRENDYOL-2026-003";
        IInvoiceProvider adapter = null!;

        var result = await adapter.CancelInvoiceAsync(gibId);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    private static InvoiceDto BuildInvoice(string number, string customerName, string? taxNo)
    {
        return new InvoiceDto(
            InvoiceNumber: number,
            CustomerName: customerName,
            CustomerTaxNumber: taxNo,
            CustomerTaxOffice: taxNo is not null ? "Kadıköy" : null,
            CustomerAddress: "Test Mah. No:1 İstanbul",
            SubTotal: 100m,
            TaxTotal: 18m,
            GrandTotal: 118m,
            Lines: new[]
            {
                new InvoiceLineDto(
                    ProductName: "Test Ürün",
                    SKU: "SKU-001",
                    Quantity: 1,
                    UnitPrice: 100m,
                    TaxRate: 18m,
                    TaxAmount: 18m,
                    LineTotal: 118m)
            });
    }
}
