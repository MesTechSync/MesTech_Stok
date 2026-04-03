using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class CreateFeedSourceHandlerTests
{
    private readonly Mock<ISupplierFeedRepository> _feedRepoMock = new();
    private readonly Mock<IUnitOfWork> _uowMock = new();
    private readonly Mock<ILogger<CreateFeedSourceHandler>> _loggerMock = new();

    private CreateFeedSourceHandler CreateHandler() =>
        new(_feedRepoMock.Object, _uowMock.Object, _loggerMock.Object);

    private static CreateFeedSourceCommand CreateCommand() =>
        new(Guid.NewGuid(), "Test Feed", "https://example.com/feed.xml",
            FeedFormat.Xml, 10m, 0m, 60, null, false);

    [Fact]
    public async Task Handle_HappyPath_ShouldReturnFeedId()
    {
        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.Should().NotBeEmpty();
        _feedRepoMock.Verify(r => r.AddAsync(It.IsAny<SupplierFeed>(), It.IsAny<CancellationToken>()), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSetCorrectProperties()
    {
        SupplierFeed? capturedFeed = null;
        _feedRepoMock.Setup(r => r.AddAsync(It.IsAny<SupplierFeed>(), It.IsAny<CancellationToken>()))
            .Callback<SupplierFeed, CancellationToken>((f, _) => capturedFeed = f);

        var command = CreateCommand();
        await CreateHandler().Handle(command, CancellationToken.None);

        capturedFeed.Should().NotBeNull();
        capturedFeed!.Name.Should().Be("Test Feed");
        capturedFeed.FeedUrl.Should().Be("https://example.com/feed.xml");
        capturedFeed.Format.Should().Be(FeedFormat.Xml);
        capturedFeed.PriceMarkupPercent.Should().Be(10m);
        capturedFeed.SupplierId.Should().Be(command.SupplierId);
    }
}
