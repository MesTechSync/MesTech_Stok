using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs.Platform;
using MesTech.Application.Features.Platform.Queries.GetPlatformSyncStatus;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class PlatformSyncStatusAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private PlatformSyncStatusAvaloniaViewModel CreateSut()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPlatformSyncStatusQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PlatformSyncStatusDto>());
        return new(_mediatorMock.Object, Mock.Of<MesTech.Domain.Interfaces.ICurrentUserService>());
    }

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
    public async Task LoadAsync_ShouldCompleteWithoutError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenEmpty_ShouldSetEmptyState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Platforms.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
        sut.TotalCount.Should().Be(0);
    }

    [Fact]
    public void HealthColor_ShouldMatchStatus()
    {
        // Assert — verify HealthColor logic via DTO directly
        var healthy = new PlatformSyncStatusItemDto { HealthStatus = "Saglikli" };
        healthy.HealthColor.Should().Be("#4CAF50");

        var passive = new PlatformSyncStatusItemDto { HealthStatus = "Pasif" };
        passive.HealthColor.Should().Be("#9E9E9E");

        var warning = new PlatformSyncStatusItemDto { HealthStatus = "Uyari" };
        warning.HealthColor.Should().Be("#FF9800");
    }

    [Fact]
    public async Task SyncPlatformCommand_PassivePlatform_ShouldNotSync()
    {
        // Arrange — manually add a passive platform item
        var sut = CreateSut();
        await sut.LoadAsync();
        var ebay = new PlatformSyncStatusItemDto { Platform = "eBay", HealthStatus = "Pasif" };
        sut.Platforms.Add(ebay);

        // Act — sync passive platform should be a no-op
        await sut.SyncPlatformCommand.ExecuteAsync(ebay);

        // Assert — status should remain Pasif (command returns early)
        ebay.HealthStatus.Should().Be("Pasif");
    }
}
