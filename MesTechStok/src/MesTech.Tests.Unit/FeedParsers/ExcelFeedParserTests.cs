using System.IO;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.FeedParsers;

/// <summary>
/// DEV 5 — Dalga 7.5 Task 5.01: ExcelFeedParser contract tests.
/// Skip'd until DEV 3 implements ExcelFeedParser (Görev 3.01).
/// Excel test data is generated in-memory using ClosedXML/EPPlus.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "FeedParser")]
public class ExcelFeedParserTests : FeedParserTestBase
{
    protected override FeedFormat ExpectedFormat => FeedFormat.Excel;
    protected override string ValidFeedFileName => "sample-feed.xlsx";
    protected override string MalformedFeedFileName => "malformed-feed.xlsx";

    // DEV 3 Dalga 7.5: ExcelFeedParser implemented — skips removed

    protected override IFeedParserService CreateParser()
        => new MesTech.Infrastructure.Integration.FeedParsers.ExcelFeedParser();

    /// <summary>
    /// Creates an in-memory Excel stream with the given products.
    /// Uses EPPlus (available in test project).
    /// </summary>
    private static MemoryStream CreateExcelStream(
        string[] headers,
        object?[][] rows)
    {
        OfficeOpenXml.ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        var ms = new MemoryStream();
        using (var package = new OfficeOpenXml.ExcelPackage(ms))
        {
            var ws = package.Workbook.Worksheets.Add("Products");
            for (int c = 0; c < headers.Length; c++)
                ws.Cells[1, c + 1].Value = headers[c];

            for (int r = 0; r < rows.Length; r++)
                for (int c = 0; c < rows[r].Length; c++)
                    ws.Cells[r + 2, c + 1].Value = rows[r][c];

            package.Save();
        }
        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateValidExcelStream()
    {
        return CreateExcelStream(
            new[] { "sku", "barcode", "name", "price", "quantity", "category", "brand", "model" },
            new[]
            {
                new object?[] { "SKU-001", "8690001000011", "Bluetooth Kulaklık Pro", 249.90m, 150, "Elektronik/Kulaklık", "TechPro", "BT-PRO-500" },
                new object?[] { "SKU-002", "8690001000028", "USB-C Hub 7-in-1", 189.50m, 300, "Elektronik/Aksesuar", "ConnectPlus", "HUB-7IN1" },
                new object?[] { "SKU-003", "8690001000035", "Ergonomik Fare", 129.00m, 0, "Elektronik/Fare", "ErgoTech", "VM-600" }
            });
    }

    [Fact]
    public async Task Valid_Feed_Returns_Products()
    {
        var parser = CreateParser();
        using var stream = CreateValidExcelStream();
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(3);
        result.TotalParsed.Should().Be(3);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Empty_Feed_Returns_EmptyList()
    {
        var parser = CreateParser();
        using var stream = CreateExcelStream(
            new[] { "sku", "name", "price" },
            Array.Empty<object?[]>());
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.TotalParsed.Should().Be(0);
    }

    [Fact]
    public async Task Malformed_Feed_Returns_Errors()
    {
        var parser = CreateParser();
        using var stream = CreateExcelStream(
            new[] { "sku", "name", "price" },
            new[]
            {
                new object?[] { "VALID-001", "Good Product", 100.0m },
                new object?[] { null, null, "NOT_A_NUMBER" },
                new object?[] { "", "No SKU", 50.0m }
            });
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.SkippedCount.Should().BeGreaterOrEqualTo(1, "rows without SKU/barcode should be skipped");
    }

    [Fact]
    public async Task Missing_Required_Fields_Skips_Product()
    {
        var parser = CreateParser();
        using var stream = CreateExcelStream(
            new[] { "sku", "name", "price" },
            new[]
            {
                new object?[] { null, "No SKU Product", 10.0m },
                new object?[] { "HAS-SKU", "Valid Product", 20.0m }
            });
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().ContainSingle(p => p.SKU == "HAS-SKU");
        result.SkippedCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Large_Feed_Completes_In_Time()
    {
        var parser = CreateParser();
        var rows = Enumerable.Range(1, 10_000).Select(i =>
            new object?[] { $"SKU-{i:D5}", $"8690{i:D9}", $"Product {i}", i * 1.5m, i % 100 }).ToArray();
        using var stream = CreateExcelStream(
            new[] { "sku", "barcode", "name", "price", "quantity" }, rows);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await parser.ParseAsync(stream, DefaultMapping);
        sw.Stop();

        result.Products.Should().HaveCount(10_000);
        sw.Elapsed.TotalSeconds.Should().BeLessThan(10, "10K Excel rows should parse in <10s (ClosedXML DOM overhead)");
    }

    [Fact]
    public async Task Encoding_UTF8_Handles_Turkish()
    {
        var parser = CreateParser();
        using var stream = CreateExcelStream(
            new[] { "sku", "name", "brand", "price", "quantity" },
            new[]
            {
                new object?[] { "TR-001", "Çift Kişilik Nevresim Takımı", "Özdilek", 449.90m, 75 }
            });
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().ContainSingle();
        result.Products.First().Name.Should().Contain("Çift Kişilik");
    }

    [Fact]
    public async Task Column_Mapping_Resolves_Custom_Names()
    {
        var parser = CreateParser();
        using var stream = CreateExcelStream(
            new[] { "product_code", "ean", "title", "retail_price", "stock" },
            new[]
            {
                new object?[] { "CUSTOM-001", "8690001", "Custom Product", 199.90m, 42 }
            });
        var result = await parser.ParseAsync(stream, CustomMapping);

        var product = result.Products.Should().ContainSingle().Subject;
        product.SKU.Should().Be("CUSTOM-001");
        product.Barcode.Should().Be("8690001");
        product.Name.Should().Be("Custom Product");
    }

    [Fact]
    public async Task Validate_Returns_Format_And_Count()
    {
        var parser = CreateParser();
        using var stream = CreateValidExcelStream();
        var validation = await parser.ValidateAsync(stream);

        validation.IsValid.Should().BeTrue();
        validation.EstimatedProductCount.Should().Be(3);
    }
}
