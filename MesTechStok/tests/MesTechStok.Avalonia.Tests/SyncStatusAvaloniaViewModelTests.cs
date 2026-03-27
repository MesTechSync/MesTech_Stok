using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class SyncStatusAvaloniaViewModelTests
{
    private readonly SyncStatusAvaloniaViewModel _sut;

    public SyncStatusAvaloniaViewModelTests()
    {
        _sut = new SyncStatusAvaloniaViewModel(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateItems()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Items.Should().HaveCount(10);
        _sut.TotalCount.Should().Be(10);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldContainExpectedPlatforms()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Items.Should().Contain(i => i.PlatformAdi == "Trendyol");
        _sut.Items.Should().Contain(i => i.PlatformAdi == "Hepsiburada");
        _sut.Items.Should().Contain(i => i.PlatformAdi == "Amazon");
        _sut.Items.Should().Contain(i => i.PlatformAdi == "Shopify");
        _sut.Items.Should().Contain(i => i.PlatformAdi == "eBay");
    }

    [Fact]
    public async Task LoadAsync_ShouldHaveVariousStatuses()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Items.Should().Contain(i => i.Durum == "Basarili");
        _sut.Items.Should().Contain(i => i.Durum == "Hatali");
        _sut.Items.Should().Contain(i => i.Durum == "Bekliyor");
    }

    [Fact]
    public async Task SyncNowCommand_ShouldUpdateStatusToBasarili()
    {
        // Arrange
        await _sut.LoadAsync();
        var hatalıPlatform = _sut.Items.First(i => i.Durum == "Hatali");

        // Act
        await _sut.SyncNowCommand.ExecuteAsync(hatalıPlatform);

        // Assert
        hatalıPlatform.Durum.Should().Be("Basarili");
        hatalıPlatform.SonSenkronizasyon.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DurumRenk_ShouldReturnCorrectColorCodes()
    {
        // Arrange
        var basarili = new SyncStatusItemDto { Durum = "Basarili" };
        var hatali = new SyncStatusItemDto { Durum = "Hatali" };
        var bekliyor = new SyncStatusItemDto { Durum = "Bekliyor" };
        var bilinmeyen = new SyncStatusItemDto { Durum = "Bilinmeyen" };

        // Assert
        basarili.DurumRenk.Should().Be("#16A34A");
        hatali.DurumRenk.Should().Be("#DC2626");
        bekliyor.DurumRenk.Should().Be("#D97706");
        bilinmeyen.DurumRenk.Should().Be("#64748B");
    }
}
