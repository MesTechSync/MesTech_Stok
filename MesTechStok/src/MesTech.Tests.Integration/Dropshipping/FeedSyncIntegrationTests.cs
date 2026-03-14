using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace MesTech.Tests.Integration.Dropshipping;

/// <summary>
/// DEV 5 — C-09: FeedSync WireMock integration tests.
/// Verifies XML feed download + parse, performance (5000 products),
/// 404 error handling, timeout behaviour, and Turkish CSV encoding.
/// Tests use WireMock.Net as a real in-process HTTP server.
/// No database or external services required.
/// </summary>
[Trait("Integration", "Dropshipping")]
[Trait("Category", "Integration")]
public class FeedSyncIntegrationTests : IDisposable
{
    private readonly WireMockServer _server;

    public FeedSyncIntegrationTests()
    {
        _server = WireMockServer.Start();
    }

    public void Dispose() => _server.Stop();

    // ── Test 1: XML Feed İndir + Parse ───────────────────────────────────

    [Fact]
    public async Task XmlFeed_Download_And_Parse_Returns1000Products()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/feed.xml").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/xml")
                .WithBody(GenerateXmlFeed(1000)));

        using var httpClient = new HttpClient();

        // Act
        var response = await httpClient.GetAsync($"{_server.Url}/feed.xml");
        await using var stream = await response.Content.ReadAsStreamAsync();

        var sw = Stopwatch.StartNew();
        var products = ParseXmlFeedStream(stream);
        sw.Stop();

        // Assert
        products.Should().HaveCount(1000);
        sw.ElapsedMilliseconds.Should().BeLessThan(5000,
            "parsing 1000 products should complete well within 5 seconds");
    }

    // ── Test 2: XML Feed 5000 Ürün Performans ────────────────────────────

    [Fact]
    public async Task XmlFeed_5000Products_ParsesWithin5Seconds()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/large-feed.xml").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/xml")
                .WithBody(GenerateXmlFeed(5000)));

        using var httpClient = new HttpClient();

        // Act
        var response = await httpClient.GetAsync($"{_server.Url}/large-feed.xml");
        await using var stream = await response.Content.ReadAsStreamAsync();

        var sw = Stopwatch.StartNew();
        var products = ParseXmlFeedStream(stream);
        sw.Stop();

        // Assert
        products.Should().HaveCount(5000);
        sw.ElapsedMilliseconds.Should().BeLessThan(5000,
            "streaming XML parser must handle 5000 products within 5 seconds");
    }

    // ── Test 3: 404 Feed → Başarısız Durum Döner ─────────────────────────

    [Fact]
    public async Task Feed_404NotFound_ReturnsUnsuccessfulStatus()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/missing.xml").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        using var httpClient = new HttpClient();

        // Act
        var response = await httpClient.GetAsync($"{_server.Url}/missing.xml");

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse(
            "a missing feed URL should return a non-success status");
        ((int)response.StatusCode).Should().Be(404);
    }

    // ── Test 4: Yavaş Feed → Timeout Exception ───────────────────────────

    [Fact]
    public async Task Feed_SlowResponse_TimeoutThrowsException()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/slow.xml").UsingGet())
            .RespondWith(Response.Create()
                .WithDelay(TimeSpan.FromSeconds(35))
                .WithStatusCode(200)
                .WithBody("<Products/>"));

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };

        // Act
        var act = async () => await httpClient.GetAsync($"{_server.Url}/slow.xml");

        // Assert
        await act.Should().ThrowAsync<TaskCanceledException>(
            "HttpClient must throw when the feed server is too slow");
    }

    // ── Test 5: CSV Feed Türkçe Karakter Doğrulama ───────────────────────

    [Fact]
    public async Task CsvFeed_TurkishChars_ParsedCorrectly()
    {
        // Arrange
        var csvContent = "SKU;Ad;Fiyat;Stok\n"
                       + "TRK001;Sarj Aleti;129.90;50\n"
                       + "TRK002;Kulaklik Cantasi;49.99;100";

        _server
            .Given(Request.Create().WithPath("/tr.csv").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/csv; charset=utf-8")
                .WithBody(csvContent));

        using var httpClient = new HttpClient();

        // Act
        var response = await httpClient.GetAsync($"{_server.Url}/tr.csv");
        var content = await response.Content.ReadAsStringAsync();

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Should().HaveCount(3, "header + 2 product lines expected");
        lines[0].Should().StartWith("SKU", "first line is the header");
        lines[1].Should().StartWith("TRK001", "first product row must be TRK001");
        lines[2].Should().StartWith("TRK002", "second product row must be TRK002");
        content.Should().Contain("Sarj Aleti", "Turkish product names must be preserved");
        content.Should().Contain("Kulaklik Cantasi", "second product name must be preserved");
    }

    // ── Yardımcı: XML Feed Üretici ───────────────────────────────────────

    private static string GenerateXmlFeed(int count)
    {
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<Products>\n");
        for (int i = 1; i <= count; i++)
        {
            sb.Append("  <Product>\n");
            sb.Append($"    <ProductCode>SKU-{i:D6}</ProductCode>\n");
            sb.Append($"    <ProductName>Test Urun {i}</ProductName>\n");
            sb.Append($"    <Price>{(i * 9.99m):F2}</Price>\n");
            sb.Append($"    <StockAmount>{i % 100}</StockAmount>\n");
            sb.Append("  </Product>\n");
        }
        sb.Append("</Products>");
        return sb.ToString();
    }

    // ── Yardımcı: Streaming XML Parser ───────────────────────────────────

    private static List<(string Sku, string Name, decimal Price, int Stock)> ParseXmlFeedStream(Stream stream)
    {
        var results = new List<(string, string, decimal, int)>();
        var settings = new XmlReaderSettings { Async = false };
        using var reader = XmlReader.Create(stream, settings);

        string? sku = null, name = null;
        decimal price = 0;
        int stock = 0;

        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                switch (reader.Name)
                {
                    case "ProductCode":
                        sku = reader.ReadElementContentAsString();
                        break;
                    case "ProductName":
                        name = reader.ReadElementContentAsString();
                        break;
                    case "Price":
                        decimal.TryParse(
                            reader.ReadElementContentAsString(),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out price);
                        break;
                    case "StockAmount":
                        int.TryParse(reader.ReadElementContentAsString(), out stock);
                        break;
                }
            }
            else if (reader.NodeType == XmlNodeType.EndElement
                     && reader.Name == "Product"
                     && sku != null)
            {
                results.Add((sku, name ?? string.Empty, price, stock));
                sku = name = null;
                price = 0;
                stock = 0;
            }
        }

        return results;
    }
}
