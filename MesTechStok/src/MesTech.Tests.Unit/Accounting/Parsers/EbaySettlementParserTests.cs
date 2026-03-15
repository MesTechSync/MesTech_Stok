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
    public async Task ParseAsync_ThrowsNotImplementedException()
    {
        using var stream = new MemoryStream();
        var act = async () => await _sut.ParseAsync(stream, "json");
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task ParseLinesAsync_ThrowsNotImplementedException()
    {
        var batch = MesTech.Domain.Accounting.Entities.SettlementBatch.Create(
            Guid.Empty, "eBay", DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0);

        var act = async () => await _sut.ParseLinesAsync(batch);
        await act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task ParseAsync_NotImplemented_MessageContainsEbay()
    {
        using var stream = new MemoryStream();
        var act = async () => await _sut.ParseAsync(stream, "json");
        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*eBay*");
    }

    [Fact]
    public async Task ParseLinesAsync_NotImplemented_MessageContainsEbay()
    {
        var batch = MesTech.Domain.Accounting.Entities.SettlementBatch.Create(
            Guid.Empty, "eBay", DateTime.UtcNow, DateTime.UtcNow, 0, 0, 0);

        var act = async () => await _sut.ParseLinesAsync(batch);
        await act.Should().ThrowAsync<NotImplementedException>()
            .WithMessage("*eBay*");
    }

    [Fact]
    public void EbayParser_ImplementsISettlementParser()
    {
        _sut.Should().BeAssignableTo<MesTech.Application.Interfaces.Accounting.ISettlementParser>();
    }
}
