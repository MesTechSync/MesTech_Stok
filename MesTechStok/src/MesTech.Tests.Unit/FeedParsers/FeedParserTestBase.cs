using System.IO;
using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;

namespace MesTech.Tests.Unit.FeedParsers;

/// <summary>
/// Base class for feed parser tests. Provides common test data paths,
/// default field mappings, and shared assertion helpers.
/// DEV 3 implementations will be tested via these contracts.
/// </summary>
public abstract class FeedParserTestBase
{
    protected abstract IFeedParserService CreateParser();
    protected abstract FeedFormat ExpectedFormat { get; }
    protected abstract string ValidFeedFileName { get; }
    protected abstract string MalformedFeedFileName { get; }

    protected static readonly FeedFieldMapping DefaultMapping = new(
        SkuField: "sku",
        BarcodeField: "barcode",
        NameField: "name",
        PriceField: "price",
        QuantityField: "quantity",
        CategoryField: "category",
        ImageField: "image",
        DescriptionField: "description");

    protected static readonly FeedFieldMapping CustomMapping = new(
        SkuField: "product_code",
        BarcodeField: "ean",
        NameField: "title",
        PriceField: "retail_price",
        QuantityField: "stock",
        CategoryField: "cat",
        ImageField: "photo_url",
        DescriptionField: "desc");

    protected static string GetTestDataPath(string fileName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return Path.Combine(baseDir, "FeedParsers", "FeedParserTestData", fileName);
    }

    protected static Stream GetTestDataStream(string fileName)
    {
        var path = GetTestDataPath(fileName);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Test data file not found: {path}");
        return File.OpenRead(path);
    }

    protected static MemoryStream CreateEmptyStream() => new();

    protected static MemoryStream CreateStreamFromString(string content)
    {
        var ms = new MemoryStream();
        var writer = new StreamWriter(ms);
        writer.Write(content);
        writer.Flush();
        ms.Position = 0;
        return ms;
    }
}
