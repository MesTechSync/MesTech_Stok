using FluentAssertions;
using MesTech.Application.Features.Dropshipping.Commands;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTech.Tests.Unit.Application.Handlers.Commands;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class CreateFeedSourceHandlerTests
{
    private readonly Mock<ISupplierFeedRepository> _feedRepoMock = new();
    private readonly Mock<ICurrentUserService> _currentUserMock = new();
    private readonly Mock<ITenantProvider> _tenantProviderMock = new();

    public CreateFeedSourceHandlerTests()
    {
        _tenantProviderMock.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        _currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
    }

    private CreateFeedSourceCommandHandler CreateHandler() =>
        new(_feedRepoMock.Object, _currentUserMock.Object, _tenantProviderMock.Object);

    private static CreateFeedSourceCommand CreateCommand() =>
        new(Guid.NewGuid(), "Test Feed", "https://example.com/feed.xml",
            FeedFormat.Xml, 10m, 0m, 60, null, false);

    [Fact]
    public async Task Handle_HappyPath_ShouldReturnFeedId()
    {
        var result = await CreateHandler().Handle(CreateCommand(), CancellationToken.None);

        result.Should().NotBeEmpty();
        _feedRepoMock.Verify(r => r.AddAsync(It.IsAny<SupplierFeed>(), It.IsAny<CancellationToken>()), Times.Once);
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
        capturedFeed.IsActive.Should().BeTrue();
        capturedFeed.SyncIntervalMinutes.Should().Be(60);
        capturedFeed.AutoDeactivateOnZeroStock.Should().BeFalse();
    }
}
