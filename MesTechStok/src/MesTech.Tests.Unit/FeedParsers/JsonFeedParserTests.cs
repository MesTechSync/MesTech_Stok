using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.FeedParsers;

/// <summary>
/// DEV 5 — Dalga 7.5 Task 5.01: JsonFeedParser contract tests.
/// Skip'd until DEV 3 implements JsonFeedParser (Görev 3.01).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "FeedParser")]
public class JsonFeedParserTests : FeedParserTestBase
{
    protected override FeedFormat ExpectedFormat => FeedFormat.Json;
    protected override string ValidFeedFileName => "sample-feed.json";
    protected override string MalformedFeedFileName => "malformed-feed.json";

    // DEV 3 Dalga 7.5: JsonFeedParser implemented — skips removed

    protected override IFeedParserService CreateParser()
        => new MesTech.Infrastructure.Integration.FeedParsers.JsonFeedParser();

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
        using var stream = CreateStreamFromString("{\"products\":[]}");
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

        // "this is not a product object" should be caught as error
        result.Errors.Should().NotBeEmpty("malformed JSON entries should produce parse errors");
    }

    [Fact]
    public async Task Missing_Required_Fields_Skips_Product()
    {
        var parser = CreateParser();
        var json = "{\"products\":[{\"name\":\"No SKU\"},{\"sku\":\"HAS-SKU\",\"name\":\"Valid\",\"price\":20}]}";
        using var stream = CreateStreamFromString(json);
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().ContainSingle(p => p.SKU == "HAS-SKU");
        result.SkippedCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task Large_Feed_Completes_In_Time()
    {
        var parser = CreateParser();
        // Use InvariantCulture for price formatting — Turkish locale uses comma decimal separator
        // which breaks JSON structure (e.g. "price":1,50 instead of "price":1.50)
        var products = string.Join(",", Enumerable.Range(1, 10_000).Select(i =>
        {
            var price = (i * 1.5m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            return $"{{\"sku\":\"SKU-{i:D5}\",\"barcode\":\"8690{i:D9}\",\"name\":\"Product {i}\",\"price\":{price},\"quantity\":{i % 100}}}";
        }));
        var json = $"{{\"products\":[{products}]}}";
        using var stream = CreateStreamFromString(json);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await parser.ParseAsync(stream, DefaultMapping);
        sw.Stop();

        result.Products.Should().HaveCount(10_000);
        sw.Elapsed.TotalSeconds.Should().BeLessThan(5, "10K JSON products should parse in <5s");
    }

    [Fact]
    public async Task Encoding_UTF8_Handles_Turkish()
    {
        var parser = CreateParser();
        var json = "{\"products\":[{\"sku\":\"TR-001\",\"name\":\"Çift Kişilik Nevresim Takımı\",\"brand\":\"Özdilek\",\"price\":449.90,\"quantity\":75}]}";
        using var stream = CreateStreamFromString(json);
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.Should().ContainSingle();
        result.Products.First().Name.Should().Contain("Çift Kişilik");
        result.Products.First().Brand.Should().Contain("Özdilek");
    }

    [Fact]
    public async Task Price_AsString_ParsedCorrectly()
    {
        var parser = CreateParser();
        var json = "{\"products\":[{\"sku\":\"P1\",\"name\":\"Test\",\"price\":\"149.99\",\"quantity\":10}]}";
        using var stream = CreateStreamFromString(json);
        var result = await parser.ParseAsync(stream, DefaultMapping);

        result.Products.First().Price.Should().Be(149.99m);
    }

    [Fact]
    public async Task Validate_Returns_Format_And_Count()
    {
        var parser = CreateParser();
        using var stream = GetTestDataStream(ValidFeedFileName);
        var validation = await parser.ValidateAsync(stream);

        validation.IsValid.Should().BeTrue();
        validation.Format.Should().ContainEquivalentOf("json");
        validation.EstimatedProductCount.Should().Be(3);
    }
}
