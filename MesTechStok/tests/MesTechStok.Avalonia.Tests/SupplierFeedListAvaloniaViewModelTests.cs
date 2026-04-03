using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Dropshipping.Queries;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Common;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class SupplierFeedListAvaloniaViewModelTests
{
    private static readonly List<FeedSourceDto> DemoFeeds =
    [
        new(Guid.NewGuid(), "Mega Elektronik XML", "https://mega.com/feed.xml", "XML", 15.0m, 60, true, "Success", new DateTime(2026, 3, 1, 10, 30, 0), null, 1200),
        new(Guid.NewGuid(), "Deniz Tech CSV", "https://deniz.com/export.csv", "CSV", 12.0m, 120, true, "Success", new DateTime(2026, 3, 1, 8, 0, 0), null, 850),
        new(Guid.NewGuid(), "ABC API Feed", "https://abc.com/api/products", "API", 10.0m, 30, true, "Failed", new DateTime(2026, 2, 28, 14, 0, 0), "Timeout", 430),
        new(Guid.NewGuid(), "Star Excel Import", "https://star.com/products.xlsx", "Excel", 18.0m, 1440, true, "Success", new DateTime(2026, 2, 27, 9, 0, 0), null, 320),
        new(Guid.NewGuid(), "Eski Tedarikci Feed", "https://eski.com/feed.xml", "XML", 20.0m, 60, false, "Success", null, null, 0),
    ];

    private static SupplierFeedListAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock
            .Setup(m => m.Send(It.IsAny<GetFeedSourcesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(PagedResult<FeedSourceDto>.Create(DemoFeeds, DemoFeeds.Count, 1, 50));
        return new SupplierFeedListAvaloniaViewModel(mediatorMock.Object);
    }

    // ── 3-State: Default ──

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
        sut.Feeds.Should().BeEmpty();
    }

    // ── 3-State: Loading → Loaded ──

    [Fact]
    public async Task LoadAsync_ShouldPopulateFeedsAndSetCounts()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Feeds.Should().HaveCount(5);
        sut.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task LoadAsync_FeedData_ShouldContainMultipleFeedTypes()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — demo data has XML, CSV, API, Excel feed types
        var feedTypes = sut.Feeds.Select(f => f.FeedType).Distinct().ToList();
        feedTypes.Should().Contain("XML");
        feedTypes.Should().Contain("CSV");
        feedTypes.Should().Contain("API");
        feedTypes.Should().Contain("Excel");
    }

    [Fact]
    public async Task LoadAsync_FeedData_ShouldContainMultipleStatuses()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — demo data has Aktif, Hatali, Pasif statuses
        var statuses = sut.Feeds.Select(f => f.Status).Distinct().ToList();
        statuses.Should().Contain("Aktif");
        statuses.Should().Contain("Hatali");
        statuses.Should().Contain("Pasif");
    }

    // ── 3-State: Refresh ──

    [Fact]
    public async Task RefreshCommand_ShouldReloadFeeds()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        await sut.RefreshCommand.ExecuteAsync(null);

        // Assert
        sut.Feeds.Should().HaveCount(5);
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}
