using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class PlatformListAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private PlatformListAvaloniaViewModel CreateSut() => new(_mediatorMock.Object, Mock.Of<MesTech.Domain.Interfaces.ICurrentUserService>());

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
    public async Task LoadAsync_ShouldPopulate13Platforms()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Platforms.Should().HaveCount(13);
        sut.TotalCount.Should().Be(13);
        sut.IsEmpty.Should().BeFalse();
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetIsLoadingFalseAfterCompletion()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — 3-state: loading done, no error, not empty
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_PlatformsShouldContainTrendyolAsFirst()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Platforms[0].Name.Should().Be("Trendyol");
        sut.Platforms[0].Color.Should().Be("#FF6F00");
        sut.Platforms[0].IsActive.Should().BeTrue();
        sut.Platforms[0].StoreCount.Should().Be(2);
        sut.Platforms[0].StatusText.Should().Be("Aktif");
    }

    [Fact]
    public async Task LoadAsync_InactivePlatformsShouldHaveStatusPasif()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        var inactive = sut.Platforms.Where(p => !p.IsActive).ToList();
        inactive.Should().HaveCountGreaterThan(0);
        inactive.Should().AllSatisfy(p => p.StatusText.Should().Be("Pasif"));
        inactive.Should().AllSatisfy(p => p.StoreCount.Should().Be(0));
    }
}
