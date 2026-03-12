using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.FeedParsers;

/// <summary>
/// DEV 5 — Dalga 7.5 Task 5.01: XmlFeedParser contract tests.
/// Skip'd until DEV 3 implements XmlFeedParser (Görev 3.01).
/// Remove Skip attribute once implementation is available.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "FeedParser")]
public class XmlFeedParserTests : FeedParserTestBase
{
    protected override FeedFormat ExpectedFormat => FeedFormat.Xml;
    protected override string ValidFeedFileName => "sample-feed.xml";
    protected override string MalformedFeedFileName => "malformed-feed.xml";

    // DEV 3 Dalga 7.5: XmlFeedParser implemented — skips removed

    protected override IFeedParserService CreateParser()
        => new MesTech.Infrastructure.Integration.FeedParsers.XmlFeedParser();

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
        using var stream = CreateStreamFromString("<?xml version=\"1.0\"?><products></products>");
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().BeEmpty();
        result.TotalParsed.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Malformed_Feed_Returns_Errors()
    {
        var parser = CreateParser();
        using var stream = GetTestDataStream(MalformedFeedFileName);
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Errors.Should().NotBeEmpty("malformed XML should produce parse errors");
    }

    [Fact]
    public async Task Missing_Required_Fields_Skips_Product()
    {
        var parser = CreateParser();
        var xml = @"<?xml version=""1.0""?>
<products>
  <product><name>No SKU</name><price>10</price></product>
  <product><sku>HAS-SKU</sku><name>Valid</name><price>20</price></product>
</products>";
        using var stream = CreateStreamFromString(xml);
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().ContainSingle(p => p.SKU == "HAS-SKU");
        result.SkippedCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Large_Feed_Completes_In_Time()
    {
        var parser = CreateParser();
        var xml = "<?xml version=\"1.0\"?><products>" +
            string.Concat(Enumerable.Range(1, 10_000).Select(i =>
                $"<product><sku>SKU-{i:D5}</sku><barcode>8690{i:D9}</barcode>" +
                $"<name>Product {i}</name><price>{i * 1.5m:F2}</price><quantity>{i % 100}</quantity></product>")) +
            "</products>";
        using var stream = CreateStreamFromString(xml);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await parser.ParseAsync(stream, DefaultMapping);
        sw.Stop();

        result.Products.Should().HaveCount(10_000);
        sw.Elapsed.TotalSeconds.Should().BeLessThan(3, "10K products should parse in <3s (streaming)");
    }

    [Fact]
    public async Task Encoding_UTF8_Handles_Turkish()
    {
        var parser = CreateParser();
        using var stream = GetTestDataStream("turkish-feed.xml");
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().HaveCountGreaterOrEqualTo(2);
        var first = result.Products.First();
        first.Name.Should().Contain("Çift Kişilik");
        first.Brand.Should().Contain("Özdilek");
    }

    [Fact]
    public async Task Price_Markup_Not_Applied_ByParser()
    {
        // Parser returns raw prices; markup is applied by SupplierFeedSyncJob
        var parser = CreateParser();
        using var stream = GetTestDataStream(ValidFeedFileName);
        var result = await parser.ParseAsync(stream, DefaultMapping);

        var first = result.Products.First();
        first.Price.Should().Be(249.90m, "parser should return raw price without markup");
    }

    [Fact]
    public async Task Validate_Returns_Format_And_Count()
    {
        var parser = CreateParser();
        using var stream = GetTestDataStream(ValidFeedFileName);
        var validation = await parser.ValidateAsync(stream);

        validation.IsValid.Should().BeTrue();
        validation.Format.Should().ContainEquivalentOf("xml");
        validation.EstimatedProductCount.Should().Be(3);
        validation.Errors.Should().BeEmpty();
    }
}
