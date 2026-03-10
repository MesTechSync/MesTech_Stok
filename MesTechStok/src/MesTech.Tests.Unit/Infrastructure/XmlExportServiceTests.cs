using System.Globalization;
using System.Xml.Linq;
using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Infrastructure.Services;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// Unit tests for XmlExportService — G4 C-02.
/// Verifies XML structure, field inclusion rules, decimal formatting and round-trip compat with XmlImportService.
/// </summary>
[Trait("Category", "Unit")]
public class XmlExportServiceTests
{
    private readonly XmlExportService _sut = new();

    // ── ExportProducts ──────────────────────────────────────────────────────

    [Fact]
    public async Task ExportProductsAsync_ThreeItems_ProducesCorrectXmlStructure()
    {
        // Arrange
        var products = new[]
        {
            new ProductExportDto("SKU-001", "Widget Alpha", 19.99m, 100, "Electronics", "BAR001"),
            new ProductExportDto("SKU-002", "Widget Beta",  9.50m,  50, null,           null),
            new ProductExportDto("SKU-003", "Widget Gamma", 0.99m,   0, "Toys",         "BAR003"),
        };

        // Act
        await using var stream = await _sut.ExportProductsAsync(products);
        var doc = XDocument.Load(stream);

        // Assert root
        doc.Root!.Name.LocalName.Should().Be("Products");

        var elements = doc.Root.Elements("Product").ToList();
        elements.Should().HaveCount(3);

        // First element — all fields present
        var first = elements[0];
        first.Element("SKU")!.Value.Should().Be("SKU-001");
        first.Element("Name")!.Value.Should().Be("Widget Alpha");
        first.Element("Price")!.Value.Should().Be("19.99");
        first.Element("Stock")!.Value.Should().Be("100");
        first.Element("Category")!.Value.Should().Be("Electronics");
        first.Element("Barcode")!.Value.Should().Be("BAR001");

        // Second element — no Category / Barcode
        var second = elements[1];
        second.Element("SKU")!.Value.Should().Be("SKU-002");
        second.Element("Category").Should().BeNull();
        second.Element("Barcode").Should().BeNull();
    }

    [Fact]
    public async Task ExportProductsAsync_PriceFormattedWithInvariantCulture()
    {
        // Arrange — price with more than 2 decimal places to confirm "F2" rounding
        var products = new[] { new ProductExportDto("SKU-X", "Item", 1234.5m, 10, null, null) };

        // Act
        await using var stream = await _sut.ExportProductsAsync(products);
        var doc = XDocument.Load(stream);

        // Assert — must use dot as decimal separator and exactly 2 decimal places
        doc.Root!.Element("Product")!.Element("Price")!.Value.Should().Be("1234.50");
    }

    // ── ExportStock ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportStockAsync_StockPresentPriceAbsent()
    {
        // Arrange
        var items = new[]
        {
            new StockExportDto("SKU-S1", "Stock Item One",   42),
            new StockExportDto("SKU-S2", "Stock Item Two",    0),
        };

        // Act
        await using var stream = await _sut.ExportStockAsync(items);
        var doc = XDocument.Load(stream);

        doc.Root!.Name.LocalName.Should().Be("Products");
        var elements = doc.Root.Elements("Product").ToList();
        elements.Should().HaveCount(2);

        var first = elements[0];
        first.Element("SKU")!.Value.Should().Be("SKU-S1");
        first.Element("Name")!.Value.Should().Be("Stock Item One");
        first.Element("Stock")!.Value.Should().Be("42");

        // Price must NOT be present in stock export
        first.Element("Price").Should().BeNull();
    }

    // ── ExportPrices ────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportPricesAsync_PricePresentStockAbsent()
    {
        // Arrange
        var items = new[]
        {
            new PriceExportDto("SKU-P1", "Price Item One", 99.00m),
            new PriceExportDto("SKU-P2", "Price Item Two",  0.01m),
        };

        // Act
        await using var stream = await _sut.ExportPricesAsync(items);
        var doc = XDocument.Load(stream);

        doc.Root!.Name.LocalName.Should().Be("Products");
        var elements = doc.Root.Elements("Product").ToList();
        elements.Should().HaveCount(2);

        var first = elements[0];
        first.Element("SKU")!.Value.Should().Be("SKU-P1");
        first.Element("Name")!.Value.Should().Be("Price Item One");
        first.Element("Price")!.Value.Should().Be("99.00");

        // Stock must NOT be present in price export
        first.Element("Stock").Should().BeNull();
    }

    // ── Empty collection ────────────────────────────────────────────────────

    [Fact]
    public async Task ExportProductsAsync_EmptyCollection_ReturnsValidXmlWithEmptyRoot()
    {
        // Act
        await using var stream = await _sut.ExportProductsAsync([]);
        var doc = XDocument.Load(stream);

        doc.Root!.Name.LocalName.Should().Be("Products");
        doc.Root.Elements("Product").Should().BeEmpty();
    }

    // ── CancellationToken ───────────────────────────────────────────────────

    [Fact]
    public async Task ExportProductsAsync_CancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var products = new[] { new ProductExportDto("SKU-C", "Cancelled", 1m, 1, null, null) };

        Func<Task> act = () => _sut.ExportProductsAsync(products, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    // ── Null guard ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportProductsAsync_NullArgument_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _sut.ExportProductsAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ── Round-trip compatibility with XmlImportService ──────────────────────

    [Fact]
    public async Task ExportThenImport_Products_RoundTripSucceeds()
    {
        // Arrange
        var importService = new XmlImportService();
        var products = new[]
        {
            new ProductExportDto("RT-001", "Round Trip One",   49.99m, 10, "Books",    null),
            new ProductExportDto("RT-002", "Round Trip Two",    5.00m,  0, null,       "RT-BAR"),
        };

        // Act — export then import back
        await using var exportedStream = await _sut.ExportProductsAsync(products);
        var importResult = await importService.ImportProductsAsync(exportedStream);

        // Assert — the import service must parse all rows successfully (no errors)
        importResult.TotalRows.Should().Be(2);
        importResult.SuccessCount.Should().Be(2);
        importResult.FailedCount.Should().Be(0);
        importResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExportThenImport_Stock_RoundTripSucceeds()
    {
        // Arrange
        var importService = new XmlImportService();
        var items = new[]
        {
            new StockExportDto("ST-001", "Stock RoundTrip One", 100),
            new StockExportDto("ST-002", "Stock RoundTrip Two",   0),
        };

        // Act
        await using var exportedStream = await _sut.ExportStockAsync(items);
        var importResult = await importService.ImportStockAsync(exportedStream);

        // Assert
        importResult.TotalRows.Should().Be(2);
        importResult.SuccessCount.Should().Be(2);
        importResult.FailedCount.Should().Be(0);
        importResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExportThenImport_Prices_RoundTripSucceeds()
    {
        // Arrange
        var importService = new XmlImportService();
        var items = new[]
        {
            new PriceExportDto("PR-001", "Price RoundTrip One",  12.50m),
            new PriceExportDto("PR-002", "Price RoundTrip Two", 999.99m),
        };

        // Act
        await using var exportedStream = await _sut.ExportPricesAsync(items);
        var importResult = await importService.ImportPricesAsync(exportedStream);

        // Assert
        importResult.TotalRows.Should().Be(2);
        importResult.SuccessCount.Should().Be(2);
        importResult.FailedCount.Should().Be(0);
        importResult.Errors.Should().BeEmpty();
    }
}
