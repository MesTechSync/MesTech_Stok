using FluentAssertions;
using MesTech.Application.Features.SocialFeed.Commands.RefreshSocialFeed;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MesTech.Tests.Unit.Application.SocialFeed;

[Trait("Category", "Unit")]
[Trait("Layer", "Application")]
public class RefreshSocialFeedHandlerTests
{
    private readonly Mock<ISocialFeedConfigurationRepository> _configRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Guid _configId = Guid.NewGuid();

    private RefreshSocialFeedHandler CreateSut(params ISocialFeedAdapter[] adapters) =>
        new(_configRepo.Object, adapters, _uow.Object,
            NullLogger<RefreshSocialFeedHandler>.Instance);

    [Fact]
    public async Task Handle_ConfigNotFound_ReturnsFailure()
    {
        _configRepo.Setup(r => r.GetByIdAsync(_configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SocialFeedConfiguration?)null);

        var result = await CreateSut().Handle(
            new RefreshSocialFeedCommand(_configId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_NoAdapterForPlatform_RecordsErrorAndReturnsFailure()
    {
        var config = SocialFeedConfiguration.Create(Guid.NewGuid(), SocialFeedPlatform.GoogleMerchant);
        _configRepo.Setup(r => r.GetByIdAsync(_configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        // No adapters registered → adapter lookup fails
        var result = await CreateSut().Handle(
            new RefreshSocialFeedCommand(_configId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("No adapter");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdapterGeneratesSuccessfully_ReturnsSuccessWithItemCount()
    {
        var config = SocialFeedConfiguration.Create(Guid.NewGuid(), SocialFeedPlatform.GoogleMerchant);
        _configRepo.Setup(r => r.GetByIdAsync(_configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var adapter = new Mock<ISocialFeedAdapter>();
        adapter.Setup(a => a.Platform).Returns(SocialFeedPlatform.GoogleMerchant);
        adapter.Setup(a => a.GenerateFeedAsync(It.IsAny<FeedGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedGenerationResult(true, "https://feed.example.com/gm.xml", 42, DateTime.UtcNow));

        var result = await CreateSut(adapter.Object).Handle(
            new RefreshSocialFeedCommand(_configId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.ItemCount.Should().Be(42);
        result.FeedUrl.Should().Be("https://feed.example.com/gm.xml");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AdapterReturnsFailure_RecordsErrorAndReturnsFailure()
    {
        var config = SocialFeedConfiguration.Create(Guid.NewGuid(), SocialFeedPlatform.FacebookShop);
        _configRepo.Setup(r => r.GetByIdAsync(_configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var adapter = new Mock<ISocialFeedAdapter>();
        adapter.Setup(a => a.Platform).Returns(SocialFeedPlatform.FacebookShop);
        adapter.Setup(a => a.GenerateFeedAsync(It.IsAny<FeedGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeedGenerationResult(false, null, 0, DateTime.UtcNow,
                new List<string> { "Invalid product data", "Missing GTIN" }));

        var result = await CreateSut(adapter.Object).Handle(
            new RefreshSocialFeedCommand(_configId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid product data");
        result.ErrorMessage.Should().Contain("Missing GTIN");
    }

    [Fact]
    public async Task Handle_AdapterThrowsException_CatchesAndReturnsFailure()
    {
        var config = SocialFeedConfiguration.Create(Guid.NewGuid(), SocialFeedPlatform.GoogleMerchant);
        _configRepo.Setup(r => r.GetByIdAsync(_configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        var adapter = new Mock<ISocialFeedAdapter>();
        adapter.Setup(a => a.Platform).Returns(SocialFeedPlatform.GoogleMerchant);
        adapter.Setup(a => a.GenerateFeedAsync(It.IsAny<FeedGenerationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API timeout"));

        var result = await CreateSut(adapter.Object).Handle(
            new RefreshSocialFeedCommand(_configId), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("API timeout");
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CategoryFilterParsedCorrectly()
    {
        var config = SocialFeedConfiguration.Create(Guid.NewGuid(), SocialFeedPlatform.GoogleMerchant,
            categoryFilter: "Elektronik, Giyim , Kozmetik");
        _configRepo.Setup(r => r.GetByIdAsync(_configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        FeedGenerationRequest? capturedRequest = null;
        var adapter = new Mock<ISocialFeedAdapter>();
        adapter.Setup(a => a.Platform).Returns(SocialFeedPlatform.GoogleMerchant);
        adapter.Setup(a => a.GenerateFeedAsync(It.IsAny<FeedGenerationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<FeedGenerationRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new FeedGenerationResult(true, "url", 10, DateTime.UtcNow));

        await CreateSut(adapter.Object).Handle(
            new RefreshSocialFeedCommand(_configId), CancellationToken.None);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.CategoryFilter.Should().BeEquivalentTo(new[] { "Elektronik", "Giyim", "Kozmetik" });
    }
}
