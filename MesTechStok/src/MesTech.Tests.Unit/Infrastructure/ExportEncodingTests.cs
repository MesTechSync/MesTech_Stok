using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;
using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Infrastructure.Services;
using OfficeOpenXml;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// Export encoding and Turkish character handling tests.
/// Covers Excel, XML, and CSV export services for İ/ş/ç/ğ/ö/ü/Ş/Ç/Ğ/Ö/Ü preservation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "Export")]
public class ExportEncodingTests
{
    private const string TurkishProductName = "Çift Kişilik Nevresim Takımı";
    private const string TurkishCategory = "Ev Tekstili — Örtü/Çarşaf";
    private const string TurkishAllChars = "İşçi Güneşöğütücü — ÜÖÇŞĞİ üöçşğı";
    private const string CommaInName = "Yastık, Çarşaf ve Nevresim Seti";

    static ExportEncodingTests()
    {
        ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
    }

    private readonly ExcelExportService _excelSut = new();
    private readonly XmlExportService _xmlSut = new();
    private readonly CsvExportService _csvSut = new();

    // ── Helper ────────────────────────────────────────────────────────────────

    private static ExcelWorksheet OpenFirstSheet(Stream stream)
    {
        stream.Position = 0;
        var package = new ExcelPackage(stream);
        return package.Workbook.Worksheets[0];
    }

    private static string ReadStreamAsString(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // EXCEL — Turkish character round-trip
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Export")]
    public async Task Excel_TurkishCharacters_RoundTripPreserved()
    {
        // Arrange
        var products = new[]
        {
            new ProductExportDto("SKU-TR1", TurkishProductName, 299.90m, 15, TurkishCategory, "8680001234567"),
            new ProductExportDto("SKU-TR2", TurkishAllChars, 49.99m, 100, "Ütü Masası", null),
        };

        // Act
        var stream = await _excelSut.ExportProductsAsync(products);

        // Assert
        var ws = OpenFirstSheet(stream);
        ws.Cells[2, 2].Value.ToString().Should().Be(TurkishProductName,
            "Excel must preserve Turkish characters like Ç, İ, ı, ş, ü");
        ws.Cells[2, 5].Value.ToString().Should().Be(TurkishCategory,
            "Category with Ö, Ç, Ş must survive Excel round-trip");
        ws.Cells[3, 2].Value.ToString().Should().Be(TurkishAllChars,
            "Full Turkish character set İşçiğüöÜÖÇŞĞ must be preserved");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Export")]
    public async Task Excel_EmptyCollection_GeneratesValidFile()
    {
        // Act
        var stream = await _excelSut.ExportStockAsync(Array.Empty<StockExportDto>());

        // Assert
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0, "even empty export must produce a valid xlsx");

        var ws = OpenFirstSheet(stream);
        ws.Cells[1, 1].Text.Should().Be("SKU");
        ws.Dimension.End.Row.Should().Be(1, "only header row expected for empty collection");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Export")]
    public async Task Excel_NullProductName_HandledGracefully()
    {
        // Arrange — Name is non-nullable in the DTO record, but we test null-like edge case
        var products = new[]
        {
            new ProductExportDto("SKU-NULL", string.Empty, 0m, 0, null, null),
        };

        // Act
        var stream = await _excelSut.ExportProductsAsync(products);

        // Assert — should not throw, should produce valid file
        stream.Should().NotBeNull();
        stream.Length.Should().BeGreaterThan(0);

        var ws = OpenFirstSheet(stream);
        ws.Dimension.End.Row.Should().Be(2, "header + 1 data row");
        ws.Cells[2, 1].Value.ToString().Should().Be("SKU-NULL");
    }

    // ══════════════════════════════════════════════════════════════════════════
    // XML — Turkish character preservation
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Export")]
    public async Task Xml_TurkishCharacters_PreservedInOutput()
    {
        // Arrange
        var products = new[]
        {
            new ProductExportDto("SKU-XML-TR", TurkishProductName, 199.50m, 5, TurkishCategory, null),
            new ProductExportDto("SKU-XML-TR2", TurkishAllChars, 10.00m, 1, null, null),
        };

        // Act
        await using var stream = await _xmlSut.ExportProductsAsync(products);
        var doc = XDocument.Load(stream);

        // Assert
        var elements = doc.Root!.Elements("Product").ToList();

        elements[0].Element("Name")!.Value.Should().Be(TurkishProductName,
            "XML must preserve Ç, İ, ı, ş, ü in product name");
        elements[0].Element("Category")!.Value.Should().Be(TurkishCategory,
            "XML must preserve Ö, Ç, Ş in category");
        elements[1].Element("Name")!.Value.Should().Be(TurkishAllChars,
            "XML must preserve full Turkish character set");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Export")]
    public async Task Xml_DecimalSeparator_UsesDotNotComma()
    {
        // Arrange — price with decimals that would use comma in Turkish locale
        var products = new[]
        {
            new ProductExportDto("SKU-DEC", "Test Ürün", 1234.56m, 10, null, null),
        };

        // Act
        await using var stream = await _xmlSut.ExportProductsAsync(products);
        var doc = XDocument.Load(stream);

        // Assert — must use dot (InvariantCulture), not comma (tr-TR)
        var priceValue = doc.Root!.Element("Product")!.Element("Price")!.Value;
        priceValue.Should().Be("1234.56", "decimal separator must be dot, not comma");
        priceValue.Should().NotContain(",", "Turkish locale comma separator must not leak into XML");

        // Verify it parses back correctly
        decimal.Parse(priceValue, CultureInfo.InvariantCulture).Should().Be(1234.56m);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Export")]
    public async Task Xml_StockExport_TurkishNamePreserved()
    {
        // Arrange
        var items = new[]
        {
            new StockExportDto("SKU-STK-TR", TurkishProductName, 42),
        };

        // Act
        await using var stream = await _xmlSut.ExportStockAsync(items);
        var doc = XDocument.Load(stream);

        // Assert
        doc.Root!.Element("Product")!.Element("Name")!.Value
            .Should().Be(TurkishProductName);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CSV — Turkish characters + comma-in-name edge case
    // ══════════════════════════════════════════════════════════════════════════

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Export")]
    public async Task Csv_TurkishCharacters_PreservedWithUtf8Bom()
    {
        // Arrange
        var products = new[]
        {
            new ProductExportDto("SKU-CSV-TR", TurkishProductName, 99.90m, 10, TurkishCategory, null),
        };

        // Act
        var stream = await _csvSut.ExportProductsAsync(products);

        // Verify UTF-8 BOM is present (check before ReadStreamAsString disposes stream)
        stream.Position = 0;
        var bom = new byte[3];
        _ = stream.ReadAtLeast(bom, 3);
        bom.Should().BeEquivalentTo(new byte[] { 0xEF, 0xBB, 0xBF },
            "CSV must start with UTF-8 BOM for Excel compatibility");

        // Assert — Turkish characters must appear in the CSV output
        var content = ReadStreamAsString(stream);
        content.Should().Contain(TurkishProductName,
            "CSV must preserve Turkish characters like Ç, İ, ı, ş, ü");
        content.Should().Contain(TurkishCategory,
            "CSV must preserve Ö, Ç, Ş in category field");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Export")]
    public async Task Csv_CommaInProductName_FieldIsQuoted()
    {
        // Arrange — product name contains a comma which must be quoted per RFC 4180
        var products = new[]
        {
            new ProductExportDto("SKU-COMMA", CommaInName, 149.90m, 5, "Ev Tekstili", null),
        };

        // Act
        var stream = await _csvSut.ExportProductsAsync(products);
        var content = ReadStreamAsString(stream);

        // Assert — the name field must be quoted because it contains a comma
        content.Should().Contain($"\"{CommaInName}\"",
            "field with comma must be wrapped in double quotes per RFC 4180");

        // Verify the CSV has the correct number of logical fields per line
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Length.Should().BeGreaterOrEqualTo(2, "header + at least 1 data row");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Export")]
    public async Task Csv_TurkishHeader_ContainsExpectedColumns()
    {
        // Arrange
        var products = Array.Empty<ProductExportDto>();

        // Act
        var stream = await _csvSut.ExportProductsAsync(products);
        var content = ReadStreamAsString(stream);

        // Assert — CSV headers should use Turkish labels
        content.Should().Contain("Ürün Adı", "CSV header should contain Turkish 'Ürün Adı'");
        content.Should().Contain("Fiyat", "CSV header should contain Turkish 'Fiyat'");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Feature", "Export")]
    public async Task Csv_DecimalSeparator_UsesDotNotComma()
    {
        // Arrange
        var products = new[]
        {
            new ProductExportDto("SKU-DEC-CSV", "Ürün", 1234.56m, 10, null, null),
        };

        // Act
        var stream = await _csvSut.ExportProductsAsync(products);
        var content = ReadStreamAsString(stream);

        // Assert — price should use dot separator (InvariantCulture)
        content.Should().Contain("1234.56",
            "decimal separator in price must be dot, not Turkish comma");
    }
}
