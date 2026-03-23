using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class SupplierFeedListAvaloniaViewModelTests
{
    private static SupplierFeedListAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
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
