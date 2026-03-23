using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class PlatformSyncStatusAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private PlatformSyncStatusAvaloniaViewModel CreateSut() => new(_mediatorMock.Object);

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
    }

    [Fact]
    public async Task LoadAsync_ShouldContainHealthyAndPassivePlatforms()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        var healthy = sut.Platforms.Where(p => p.HealthStatus == "Saglikli").ToList();
        var passive = sut.Platforms.Where(p => p.HealthStatus == "Pasif").ToList();
        var warning = sut.Platforms.Where(p => p.HealthStatus == "Uyari").ToList();

        healthy.Should().HaveCountGreaterThan(0);
        passive.Should().HaveCountGreaterThan(0);
        warning.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task LoadAsync_HealthColorsShouldMatchStatus()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        var trendyol = sut.Platforms.First(p => p.Platform == "Trendyol");
        trendyol.HealthStatus.Should().Be("Saglikli");
        trendyol.HealthColor.Should().Be("#4CAF50");

        var ebay = sut.Platforms.First(p => p.Platform == "eBay");
        ebay.HealthStatus.Should().Be("Pasif");
        ebay.HealthColor.Should().Be("#9E9E9E");

        var ciceksepeti = sut.Platforms.First(p => p.Platform == "Ciceksepeti");
        ciceksepeti.HealthStatus.Should().Be("Uyari");
        ciceksepeti.HealthColor.Should().Be("#FF9800");
    }

    [Fact]
    public async Task SyncPlatformCommand_PassivePlatform_ShouldNotSync()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        var ebay = sut.Platforms.First(p => p.Platform == "eBay");
        var originalStatus = ebay.HealthStatus;

        // Act — sync passive platform should be a no-op
        await sut.SyncPlatformCommand.ExecuteAsync(ebay);

        // Assert — status should remain Pasif (command returns early)
        ebay.HealthStatus.Should().Be("Pasif");
    }
}
