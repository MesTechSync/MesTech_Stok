using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.FeedParsers;

/// <summary>
/// DEV 5 — Dalga 7.5 Task 5.01: CsvFeedParser contract tests.
/// Skip'd until DEV 3 implements CsvFeedParser (Görev 3.01).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "FeedParser")]
public class CsvFeedParserTests : FeedParserTestBase
{
    protected override FeedFormat ExpectedFormat => FeedFormat.Csv;
    protected override string ValidFeedFileName => "sample-feed.csv";
    protected override string MalformedFeedFileName => "malformed-feed.csv";

    // DEV 3 Dalga 7.5: CsvFeedParser implemented — skips removed

    protected override IFeedParserService CreateParser()
        => new MesTech.Infrastructure.Integration.FeedParsers.CsvFeedParser();

    [Fact]
    public async Task Valid_Feed_Returns_Products()
    {
        var parser = CreateParser();
        using var stream = GetTestDataStream(ValidFeedFileName);
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCount(3);
        result.TotalParsed.Should().Be(3);
        result.SkippedCount.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Empty_Feed_Returns_EmptyList()
    {
        var parser = CreateParser();
        using var stream = CreateStreamFromString("sku,barcode,name,price,quantity\n");
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.TotalParsed.Should().Be(0);
    }

    [Fact]
    public async Task Malformed_Feed_Returns_Errors()
    {
        var parser = CreateParser();
        using var stream = GetTestDataStream(MalformedFeedFileName);
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Errors.Should().NotBeEmpty("malformed CSV rows should produce parse errors");
    }

    [Fact]
    public async Task Missing_Required_Fields_Skips_Product()
    {
        var parser = CreateParser();
        var csv = "sku,name,price\n,No SKU Product,10\nHAS-SKU,Valid Product,20\n";
        using var stream = CreateStreamFromString(csv);
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().ContainSingle(p => p.SKU == "HAS-SKU");
        result.SkippedCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Large_Feed_Completes_In_Time()
    {
        var parser = CreateParser();
        var header = "sku,barcode,name,price,quantity\n";
        var rows = string.Concat(Enumerable.Range(1, 10_000).Select(i =>
            $"SKU-{i:D5},8690{i:D9},Product {i},{i * 1.5m:F2},{i % 100}\n"));
        using var stream = CreateStreamFromString(header + rows);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await parser.ParseAsync(stream, DefaultMapping);
        sw.Stop();

        result.Products.Should().HaveCount(10_000);
        sw.Elapsed.TotalSeconds.Should().BeLessThan(3, "10K CSV rows should parse in <3s");
    }

    [Fact]
    public async Task Encoding_UTF8_Handles_Turkish()
    {
        var parser = CreateParser();
        var csv = "sku,name,brand,price,quantity\nTR-001,Çift Kişilik Nevresim Takımı,Özdilek,449.90,75\n";
        using var stream = CreateStreamFromString(csv);
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().ContainSingle();
        result.Products.First().Name.Should().Contain("Çift Kişilik");
    }

    [Fact]
    public async Task Header_AutoDetect_Maps_Fields()
    {
        var parser = CreateParser();
        var csv = "sku,barcode,name,price,quantity\nSKU-001,8690001,Test Product,99.90,50\n";
        using var stream = CreateStreamFromString(csv);
        var result = await parser.ParseAsync(stream, DefaultMapping);

        var product = result.Products.Should().ContainSingle().Subject;
        product.SKU.Should().Be("SKU-001");
        product.Price.Should().Be(99.90m);
    }

    [Fact]
    public async Task Validate_Returns_Format_And_Count()
    {
        var parser = CreateParser();
        using var stream = GetTestDataStream(ValidFeedFileName);
        var validation = await parser.ValidateAsync(stream);

        validation.IsValid.Should().BeTrue();
        validation.EstimatedProductCount.Should().Be(3);
        validation.Errors.Should().BeEmpty();
    }
}
