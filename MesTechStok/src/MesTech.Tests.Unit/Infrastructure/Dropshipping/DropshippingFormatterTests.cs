using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Formatters.Dropshipping;
using OfficeOpenXml;
using Xunit;

namespace MesTech.Tests.Unit.Infrastructure.Dropshipping;

/// <summary>
/// Export formatter unit testleri — tüm 7 formatter.
/// FormatAsync(IEnumerable&lt;PoolProductExportDto&gt;, ExportOptions) → Task&lt;byte[]&gt;
/// ENT-DROP-IMP-SPRINT-D — DEV 5 Task D-12
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Dropshipping")]
public class DropshippingFormatterTests
{
    // ─── Test veri fabrikaları ─────────────────────────────────────────

    private static PoolProductExportDto MakeProduct(
        string sku = "SKU-001",
        string name = "Test Ürün",
        decimal price = 100m,
        int stock = 5,
        string? barcode = "1234567890123",
        string? category = "Elektronik",
        string? brand = "TestMarka",
        string? imageUrl = "https://example.com/img.jpg",
        string? description = "Ürün açıklaması",
        string? supplierName = "Tedarikçi A") =>
        new(sku, name, barcode, price, stock,
            category, brand, imageUrl, description, supplierName);

    private static ExportOptions DefaultOptions => new();

    private static string Utf8(byte[] bytes) => Encoding.UTF8.GetString(bytes);

    // ════════════════════════════════════════════════════════════════════
    // TRENDYOL FORMATTER
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Trendyol_Platform_IsTrendyol()
    {
        new TrendyolDropshippingFormatter().Platform.Should().Be("Trendyol");
    }

    [Fact]
    public async Task Trendyol_FormatAsync_ReturnsNonEmptyBytes()
    {
        var formatter = new TrendyolDropshippingFormatter();
        var result = await formatter.FormatAsync(new[] { MakeProduct() }, DefaultOptions);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Trendyol_FormatAsync_ContainsSkuAsStockCode()
    {
        var formatter = new TrendyolDropshippingFormatter();
        var product = MakeProduct(sku: "SKU-TEST-42");

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);
        var json = Utf8(bytes);

        json.Should().Contain("SKU-TEST-42");
    }

    [Fact]
    public async Task Trendyol_FormatAsync_HasItemsArray()
    {
        var formatter = new TrendyolDropshippingFormatter();
        var bytes = await formatter.FormatAsync(new[] { MakeProduct() }, DefaultOptions);
        var json = Utf8(bytes);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("items", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Trendyol_FormatAsync_PriceMarkup_Applied()
    {
        var formatter = new TrendyolDropshippingFormatter();
        var product = MakeProduct(price: 100m);
        var options = new ExportOptions(PriceMarkupPercent: 10m);

        var bytes = await formatter.FormatAsync(new[] { product }, options);
        var json = Utf8(bytes);

        // 100 * 1.10 = 110.00
        json.Should().Contain("110.00");
    }

    [Fact]
    public async Task Trendyol_FormatAsync_ZeroMarkup_OriginalPrice()
    {
        var formatter = new TrendyolDropshippingFormatter();
        var product = MakeProduct(price: 200m);
        var options = new ExportOptions(PriceMarkupPercent: 0m);

        var bytes = await formatter.FormatAsync(new[] { product }, options);
        var json = Utf8(bytes);

        json.Should().Contain("200.00");
    }

    [Fact]
    public async Task Trendyol_FormatAsync_ZeroStockExcluded_ByDefault()
    {
        var formatter = new TrendyolDropshippingFormatter();
        var zeroStock = MakeProduct(sku: "ZERO-STOCK", stock: 0);
        var withStock = MakeProduct(sku: "HAS-STOCK", stock: 3);

        var bytes = await formatter.FormatAsync(new[] { zeroStock, withStock }, DefaultOptions);
        var json = Utf8(bytes);

        json.Should().NotContain("ZERO-STOCK");
        json.Should().Contain("HAS-STOCK");
    }

    [Fact]
    public async Task Trendyol_FormatAsync_IncludeZeroStock_IncludesZeroStockProduct()
    {
        var formatter = new TrendyolDropshippingFormatter();
        var zeroStock = MakeProduct(sku: "ZERO-SKU", stock: 0);
        var options = new ExportOptions(IncludeZeroStock: true);

        var bytes = await formatter.FormatAsync(new[] { zeroStock }, options);
        var json = Utf8(bytes);

        json.Should().Contain("ZERO-SKU");
    }

    [Fact]
    public async Task Trendyol_FormatAsync_EmptyList_ReturnsEmptyItemsArray()
    {
        var formatter = new TrendyolDropshippingFormatter();
        var bytes = await formatter.FormatAsync(Enumerable.Empty<PoolProductExportDto>(), DefaultOptions);
        var json = Utf8(bytes);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Trendyol_FormatAsync_NoImageUrl_EmptyImagesArray()
    {
        var formatter = new TrendyolDropshippingFormatter();
        var product = MakeProduct(imageUrl: null);

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);
        var json = Utf8(bytes);

        // "images":[] when no image URL
        json.Should().Contain("\"images\":[]");
    }

    [Fact]
    public async Task Trendyol_FormatAsync_Currency_InOutput()
    {
        var formatter = new TrendyolDropshippingFormatter();
        var options = new ExportOptions(Currency: "USD");

        var bytes = await formatter.FormatAsync(new[] { MakeProduct() }, options);
        var json = Utf8(bytes);

        json.Should().Contain("USD");
    }

    // ════════════════════════════════════════════════════════════════════
    // HEPSISELLER FORMATTER
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void HepsiSeller_Platform_IsHepsiSeller()
    {
        new HepsisellerDropshippingFormatter().Platform.Should().Be("HepsiSeller");
    }

    [Fact]
    public async Task HepsiSeller_FormatAsync_HasProductListArray()
    {
        var formatter = new HepsisellerDropshippingFormatter();
        var bytes = await formatter.FormatAsync(new[] { MakeProduct() }, DefaultOptions);
        var json = Utf8(bytes);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("productList", out _).Should().BeTrue();
    }

    [Fact]
    public async Task HepsiSeller_FormatAsync_ContainsMerchantSku()
    {
        var formatter = new HepsisellerDropshippingFormatter();
        var product = MakeProduct(sku: "HS-SKU-77");

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);
        var json = Utf8(bytes);

        json.Should().Contain("HS-SKU-77");
    }

    [Fact]
    public async Task HepsiSeller_FormatAsync_PriceMarkup_Applied()
    {
        var formatter = new HepsisellerDropshippingFormatter();
        var product = MakeProduct(price: 50m);
        var options = new ExportOptions(PriceMarkupPercent: 20m);

        var bytes = await formatter.FormatAsync(new[] { product }, options);
        var json = Utf8(bytes);

        // 50 * 1.20 = 60.00
        json.Should().Contain("60.00");
    }

    [Fact]
    public async Task HepsiSeller_FormatAsync_ZeroStockExcluded()
    {
        var formatter = new HepsisellerDropshippingFormatter();
        var product = MakeProduct(sku: "HS-ZERO", stock: 0);

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);
        var json = Utf8(bytes);

        json.Should().NotContain("HS-ZERO");
    }

    // ════════════════════════════════════════════════════════════════════
    // N11 FORMATTER
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void N11_Platform_IsN11()
    {
        new N11DropshippingFormatter().Platform.Should().Be("N11");
    }

    [Fact]
    public async Task N11_FormatAsync_IsXmlWithProductSaveRequestRoot()
    {
        var formatter = new N11DropshippingFormatter();
        var bytes = await formatter.FormatAsync(new[] { MakeProduct() }, DefaultOptions);
        var xml = Utf8(bytes);

        xml.Should().Contain("<productSaveRequest>");
        xml.Should().Contain("<ProductRequest>");
    }

    [Fact]
    public async Task N11_FormatAsync_ContainsSellerCode()
    {
        var formatter = new N11DropshippingFormatter();
        var product = MakeProduct(sku: "N11-SKU-33");

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);
        var xml = Utf8(bytes);

        xml.Should().Contain("N11-SKU-33");
    }

    [Fact]
    public async Task N11_FormatAsync_TRY_MapsTo1()
    {
        var formatter = new N11DropshippingFormatter();
        var options = new ExportOptions(Currency: "TRY");

        var bytes = await formatter.FormatAsync(new[] { MakeProduct() }, options);
        var xml = Utf8(bytes);

        xml.Should().Contain("<currencyType>1</currencyType>");
    }

    [Fact]
    public async Task N11_FormatAsync_ZeroStockExcluded()
    {
        var formatter = new N11DropshippingFormatter();
        var product = MakeProduct(sku: "N11-ZERO", stock: 0);

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);
        var xml = Utf8(bytes);

        xml.Should().NotContain("N11-ZERO");
    }

    // ════════════════════════════════════════════════════════════════════
    // CSV FORMATTER
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Csv_Platform_IsCSV()
    {
        new CsvDropshippingFormatter().Platform.Should().Be("CSV");
    }

    [Fact]
    public async Task Csv_FormatAsync_HasHeaderRow()
    {
        var formatter = new CsvDropshippingFormatter();
        var bytes = await formatter.FormatAsync(new[] { MakeProduct() }, DefaultOptions);

        // UTF-8 BOM prefix — skip first 3 bytes (EF BB BF)
        var text = bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF
            ? Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3)
            : Utf8(bytes);

        text.Should().StartWith("SKU,");
    }

    [Fact]
    public async Task Csv_FormatAsync_ContainsSku()
    {
        var formatter = new CsvDropshippingFormatter();
        var product = MakeProduct(sku: "CSV-SKU-99");

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);
        var text = Utf8(bytes);

        text.Should().Contain("CSV-SKU-99");
    }

    [Fact]
    public async Task Csv_FormatAsync_PriceMarkup_InRow()
    {
        var formatter = new CsvDropshippingFormatter();
        var product = MakeProduct(price: 75m);
        var options = new ExportOptions(PriceMarkupPercent: 4m);

        var bytes = await formatter.FormatAsync(new[] { product }, options);
        var text = Utf8(bytes);

        // 75 * 1.04 = 78.00
        text.Should().Contain("78.00");
    }

    [Fact]
    public async Task Csv_FormatAsync_ZeroStockExcluded()
    {
        var formatter = new CsvDropshippingFormatter();
        var product = MakeProduct(sku: "CSV-ZERO", stock: 0);

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);
        var text = Utf8(bytes);

        text.Should().NotContain("CSV-ZERO");
    }

    [Fact]
    public async Task Csv_FormatAsync_EmptyList_OnlyHeader()
    {
        var formatter = new CsvDropshippingFormatter();
        var bytes = await formatter.FormatAsync(Enumerable.Empty<PoolProductExportDto>(), DefaultOptions);
        var text = Utf8(bytes).TrimStart('\uFEFF').Trim(); // strip BOM

        // Only header line, no data rows
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1);
    }

    // ════════════════════════════════════════════════════════════════════
    // EXCEL FORMATTER
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Excel_Platform_IsExcel()
    {
        new ExcelDropshippingFormatter().Platform.Should().Be("Excel");
    }

    [Fact]
    public async Task Excel_FormatAsync_ReturnsXlsxMagicBytes()
    {
        var formatter = new ExcelDropshippingFormatter();
        var bytes = await formatter.FormatAsync(new[] { MakeProduct() }, DefaultOptions);

        bytes.Should().NotBeNullOrEmpty();
        // XLSX = ZIP = PK magic bytes 50 4B 03 04
        bytes[0].Should().Be(0x50);
        bytes[1].Should().Be(0x4B);
    }

    [Fact]
    public async Task Excel_FormatAsync_ContainsSku_InWorksheet()
    {
        var formatter = new ExcelDropshippingFormatter();
        var product = MakeProduct(sku: "XLS-SKU-55");

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);

        using var package = new ExcelPackage(new MemoryStream(bytes));
        var ws = package.Workbook.Worksheets[0];

        // Row 1 = headers, Row 2 = first data row, Col 1 = SKU
        ws.Cells[2, 1].GetValue<string>().Should().Be("XLS-SKU-55");
    }

    [Fact]
    public async Task Excel_FormatAsync_PriceMarkup_InWorksheet()
    {
        var formatter = new ExcelDropshippingFormatter();
        var product = MakeProduct(price: 200m);
        var options = new ExportOptions(PriceMarkupPercent: 5m);

        var bytes = await formatter.FormatAsync(new[] { product }, options);

        using var package = new ExcelPackage(new MemoryStream(bytes));
        var ws = package.Workbook.Worksheets[0];

        // 200 * 1.05 = 210.00 — Col 4 = Price
        ws.Cells[2, 4].GetValue<double>().Should().BeApproximately(210.0, 0.01);
    }

    [Fact]
    public async Task Excel_FormatAsync_ZeroStockExcluded()
    {
        var formatter = new ExcelDropshippingFormatter();
        var zero = MakeProduct(sku: "XLS-ZERO", stock: 0);
        var valid = MakeProduct(sku: "XLS-VALID", stock: 2);

        var bytes = await formatter.FormatAsync(new[] { zero, valid }, DefaultOptions);

        using var package = new ExcelPackage(new MemoryStream(bytes));
        var ws = package.Workbook.Worksheets[0];

        // Row 2 should be the valid product
        ws.Cells[2, 1].GetValue<string>().Should().Be("XLS-VALID");
        // Row 3 should be empty (no more products)
        ws.Cells[3, 1].Value.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════
    // XML FORMATTER
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Xml_Platform_IsXML()
    {
        new XmlDropshippingFormatter().Platform.Should().Be("XML");
    }

    [Fact]
    public async Task Xml_FormatAsync_HasProductsRoot()
    {
        var formatter = new XmlDropshippingFormatter();
        var bytes = await formatter.FormatAsync(new[] { MakeProduct() }, DefaultOptions);
        var xml = Utf8(bytes);

        xml.Should().Contain("<Products>");
        xml.Should().Contain("<Product>");
    }

    [Fact]
    public async Task Xml_FormatAsync_ContainsSku()
    {
        var formatter = new XmlDropshippingFormatter();
        var product = MakeProduct(sku: "XML-SKU-11");

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);
        var xml = Utf8(bytes);

        xml.Should().Contain("<SKU>XML-SKU-11</SKU>");
    }

    [Fact]
    public async Task Xml_FormatAsync_HideSupplierInfo_ExcludesSupplierName()
    {
        var formatter = new XmlDropshippingFormatter();
        var product = MakeProduct(supplierName: "GizliTedarikci");
        var options = new ExportOptions(HideSupplierInfo: true);

        var bytes = await formatter.FormatAsync(new[] { product }, options);
        var xml = Utf8(bytes);

        xml.Should().NotContain("GizliTedarikci");
    }

    [Fact]
    public async Task Xml_FormatAsync_ShowSupplierInfo_IncludesSupplierName()
    {
        var formatter = new XmlDropshippingFormatter();
        var product = MakeProduct(supplierName: "AcikTedarikci");
        var options = new ExportOptions(HideSupplierInfo: false);

        var bytes = await formatter.FormatAsync(new[] { product }, options);
        var xml = Utf8(bytes);

        xml.Should().Contain("AcikTedarikci");
    }

    [Fact]
    public async Task Xml_FormatAsync_IsValidXml()
    {
        var formatter = new XmlDropshippingFormatter();
        var bytes = await formatter.FormatAsync(new[] { MakeProduct() }, DefaultOptions);

        // Should parse without exception
        var act = () => XDocument.Load(new MemoryStream(bytes));
        act.Should().NotThrow();
    }

    // ════════════════════════════════════════════════════════════════════
    // OZON FORMATTER
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Ozon_Platform_IsOzon()
    {
        new OzonDropshippingFormatter().Platform.Should().Be("Ozon");
    }

    [Fact]
    public async Task Ozon_FormatAsync_HasItemsArray()
    {
        var formatter = new OzonDropshippingFormatter();
        var bytes = await formatter.FormatAsync(new[] { MakeProduct() }, DefaultOptions);
        var json = Utf8(bytes);

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("items", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Ozon_FormatAsync_ContainsOfferId()
    {
        var formatter = new OzonDropshippingFormatter();
        var product = MakeProduct(sku: "OZON-SKU-88");

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);
        var json = Utf8(bytes);

        // Ozon uses snake_case: offer_id
        json.Should().Contain("\"offer_id\":\"OZON-SKU-88\"");
    }

    [Fact]
    public async Task Ozon_FormatAsync_PriceMarkup_Applied()
    {
        var formatter = new OzonDropshippingFormatter();
        var product = MakeProduct(price: 80m);
        var options = new ExportOptions(PriceMarkupPercent: 25m);

        var bytes = await formatter.FormatAsync(new[] { product }, options);
        var json = Utf8(bytes);

        // 80 * 1.25 = 100.00
        json.Should().Contain("100.00");
    }

    [Fact]
    public async Task Ozon_FormatAsync_ZeroStockExcluded()
    {
        var formatter = new OzonDropshippingFormatter();
        var product = MakeProduct(sku: "OZON-ZERO", stock: 0);

        var bytes = await formatter.FormatAsync(new[] { product }, DefaultOptions);
        var json = Utf8(bytes);

        json.Should().NotContain("OZON-ZERO");
    }

    // ════════════════════════════════════════════════════════════════════
    // ÇAPRAZ FORMATTER TESTLERİ
    // ════════════════════════════════════════════════════════════════════

    public static IEnumerable<object[]> AllFormatters()
    {
        yield return new object[] { new TrendyolDropshippingFormatter(),     "Trendyol"   };
        yield return new object[] { new HepsisellerDropshippingFormatter(),  "HepsiSeller" };
        yield return new object[] { new N11DropshippingFormatter(),          "N11"         };
        yield return new object[] { new CsvDropshippingFormatter(),          "CSV"         };
        yield return new object[] { new ExcelDropshippingFormatter(),        "Excel"       };
        yield return new object[] { new XmlDropshippingFormatter(),          "XML"         };
        yield return new object[] { new OzonDropshippingFormatter(),         "Ozon"        };
    }

    [Theory]
    [MemberData(nameof(AllFormatters))]
    public void AllFormatters_Platform_IsNonEmpty(IDropshippingExportFormatter formatter, string expectedPlatform)
    {
        formatter.Platform.Should().Be(expectedPlatform);
    }

    [Theory]
    [MemberData(nameof(AllFormatters))]
    public async Task AllFormatters_WithOneProduct_ReturnsNonEmptyBytes(
        IDropshippingExportFormatter formatter, string _)
    {
        var bytes = await formatter.FormatAsync(new[] { MakeProduct() }, DefaultOptions);
        bytes.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [MemberData(nameof(AllFormatters))]
    public async Task AllFormatters_EmptyList_ReturnsBytes(
        IDropshippingExportFormatter formatter, string _)
    {
        var bytes = await formatter.FormatAsync(Enumerable.Empty<PoolProductExportDto>(), DefaultOptions);
        bytes.Should().NotBeNull();
        bytes.Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [MemberData(nameof(AllFormatters))]
    public async Task AllFormatters_MultipleProducts_AllProcessed(
        IDropshippingExportFormatter formatter, string _)
    {
        var products = Enumerable.Range(1, 5)
            .Select(i => MakeProduct(sku: $"SKU-{i:000}", stock: i))
            .ToList();

        var bytes = await formatter.FormatAsync(products, DefaultOptions);
        bytes.Should().NotBeNullOrEmpty();
    }
}
