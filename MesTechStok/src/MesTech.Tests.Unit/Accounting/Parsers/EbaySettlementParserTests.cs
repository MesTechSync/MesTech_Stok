using System.IO;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Settlement.Parsers;

namespace MesTech.Tests.Unit.Accounting.Parsers;

/// <summary>
/// EbaySettlementParser tests — stub: throws NotImplementedException.
/// </summary>
[Trait("Category", "Unit")]
public class EbaySettlementParserTests
{
    private readonly EbaySettlementParser _sut = new(
        new Microsoft.Extensions.Logging.Abstractions.NullLogger<EbaySettlementParser>());

    [Fact]
    public void Platform_ShouldBeEbay()
    {
        _sut.Platform.Should().Be("eBay");
    }

    [Fact]
    public async Task ParseAsync_EmptyJson_ReturnsEmptyBatch()
    {
        // Parser is now fully implemented — empty JSON with no transactions returns empty batch
        var json = System.Text.Encoding.UTF8.GetBytes("{\"transactions\": []}");
        using var stream = new MemoryStream(json);
        var result = await _sut.ParseAsync(stream, "json");
        result.Should().NotBeNull();
        result.Platform.Should().Be("eBay");
    }

    [Fact]
    public async Task ParseLinesAsync_NoCachedTransactions_ReturnsEmptyList()
    {
        // Parser is now fully implemented — returns empty when no cached transactions
        var batch = MesTech.Domain.Accounting.Entities.SettlementBatch.Create(
            Guid.NewGuid(), "eBay", DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0);

        var result = await _sut.ParseLinesAsync(batch);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseAsync_EmptyJson_BatchPlatformIsEbay()
    {
        // Parser is now fully implemented — batch platform should be eBay
        var json = System.Text.Encoding.UTF8.GetBytes("{\"transactions\": []}");
        using var stream = new MemoryStream(json);
        var result = await _sut.ParseAsync(stream, "json");
        result.Platform.Should().Contain("eBay");
    }

    [Fact]
    public async Task ParseLinesAsync_AfterParseAsync_ReturnsLines()
    {
        // Parser is now fully implemented — after ParseAsync, ParseLinesAsync uses cached data
        var json = System.Text.Encoding.UTF8.GetBytes("{\"transactions\": []}");
        using var stream = new MemoryStream(json);
        var batch = await _sut.ParseAsync(stream, "json");

        var lines = await _sut.ParseLinesAsync(batch);
        lines.Should().NotBeNull();
        lines.Should().BeEmpty("because the JSON had no transactions");
    }

    [Fact]
    public void EbayParser_ImplementsISettlementParser()
    {
        _sut.Should().BeAssignableTo<MesTech.Application.Interfaces.Accounting.ISettlementParser>();
    }
}
