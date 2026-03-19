using FluentAssertions;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Features.Dropshipping.Commands.PreviewFeed;
using MesTech.Application.Interfaces;
using MesTech.Application.Interfaces.Dropshipping;
using MesTech.Domain.Dropshipping.Entities;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Dropshipping;

/// <summary>
/// PreviewFeedHandler unit testleri.
/// Feed parse (XML/CSV) ve urun onizleme senaryolari.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Dropshipping")]
public class PreviewFeedHandlerTests
{
    private readonly Mock<ISupplierFeedRepository> _feedRepoMock = new();
    private readonly Mock<IDropshipProductRepository> _productRepoMock = new();
    private readonly Mock<ILogger<PreviewFeedHandler>> _loggerMock = new();

    // ═══════════════════════════════════════════════════════════
    // 1. XML feed parse — urun listesi donmeli
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task PreviewFeed_NoParserAvailable_ReturnsWarning()
    {
        // Arrange: feed with a format that has no parser
        var feedId = Guid.NewGuid();
        var feed = new SupplierFeed
        {
            TenantId = Guid.NewGuid(),
            SupplierId = Guid.NewGuid(),
            Name = "Test Feed",
            FeedUrl = "https://example.com/feed.xml",
            Format = FeedFormat.Xml
        };
        typeof(SupplierFeed).BaseType!.GetProperty("Id")!.SetValue(feed, feedId);

        _feedRepoMock
            .Setup(r => r.GetByIdAsync(feedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feed);

        // No parsers registered — empty list
        var handler = new PreviewFeedHandler(
            _feedRepoMock.Object,
            _productRepoMock.Object,
            Enumerable.Empty<IFeedParserService>(),
            _loggerMock.Object);

        // Act
        var result = await handler.Handle(
            new PreviewFeedCommand(feedId), CancellationToken.None);

        // Assert
        result.TotalProductCount.Should().Be(0);
        result.Products.Should().BeEmpty();
        result.Warnings.Should().ContainMatch("*No parser available*");
    }

    // ═══════════════════════════════════════════════════════════
    // 2. Feed bulunamazsa hata firlatmali
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public async Task PreviewFeed_FeedNotFound_ThrowsInvalidOperation()
    {
        // Arrange
        var feedId = Guid.NewGuid();

        _feedRepoMock
            .Setup(r => r.GetByIdAsync(feedId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupplierFeed?)null);

        var handler = new PreviewFeedHandler(
            _feedRepoMock.Object,
            _productRepoMock.Object,
            Enumerable.Empty<IFeedParserService>(),
            _loggerMock.Object);

        // Act & Assert
        var act = () => handler.Handle(
            new PreviewFeedCommand(feedId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{feedId}*not found*");
    }
}
