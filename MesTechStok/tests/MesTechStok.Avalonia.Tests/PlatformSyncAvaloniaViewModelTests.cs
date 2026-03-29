using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class PlatformSyncAvaloniaViewModelTests
{
    private static PlatformSyncAvaloniaViewModel CreateSut() => new(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>());

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
        sut.SearchText.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
        sut.Platforms.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulate10Platforms()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Platforms.Should().HaveCount(10);
        sut.TotalCount.Should().Be(10);
        sut.IsEmpty.Should().BeFalse();
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_FirstPlatformShouldBeTrendyol()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        var first = sut.Platforms[0];
        first.Platform.Should().Be("Trendyol");
        first.Status.Should().Be("Basarili");
        first.ProductCount.Should().Be(1245);
        first.OrderCount.Should().Be(89);
    }

    [Fact]
    public async Task SearchText_ShouldFilterPlatforms()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act — search requires >= 2 chars
        sut.SearchText = "Tr";

        // Assert
        sut.Platforms.Should().HaveCount(1);
        sut.Platforms[0].Platform.Should().Be("Trendyol");
        sut.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task SearchText_NoMatch_ShouldSetIsEmpty()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SearchText = "ZZZZ";

        // Assert
        sut.Platforms.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
        sut.TotalCount.Should().Be(0);
    }
}
