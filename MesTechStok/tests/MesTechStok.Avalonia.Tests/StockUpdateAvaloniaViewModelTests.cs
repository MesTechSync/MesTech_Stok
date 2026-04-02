using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockUpdateAvaloniaViewModelTests
{
    private static StockUpdateAvaloniaViewModel CreateSut()
    {
        return new StockUpdateAvaloniaViewModel(Mock.Of<IMediator>(), Mock.Of<ITenantProvider>());
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
        sut.SearchText.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
        sut.UpdateStatus.Should().BeEmpty();
        sut.StockItems.Should().BeEmpty();
    }

    // ── 3-State: Loading → Loaded ──

    [Fact]
    public async Task LoadAsync_ShouldPopulateStockItemsAndSetTotalCount()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.StockItems.Should().HaveCount(10);
        sut.TotalCount.Should().Be(10);
        sut.IsEmpty.Should().BeFalse();
    }

    // ── 3-State: Search/Filter ──

    [Fact]
    public async Task SearchText_ShouldFilterItemsBySkuOrName()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SearchText = "Trendyol";

        // Assert — 3 Trendyol items in demo data
        sut.StockItems.Should().OnlyContain(i => i.Platform == "Trendyol");
        sut.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task BulkUpdateCommand_WithChanges_ShouldUpdateAndReportCount()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        // Demo data has 2 items where MevcutStok != YeniStok:
        // CS-MNT-005 (8 vs 15) and OC-EV-007 (0 vs 50) and HB-KSA-009 (5 vs 20)

        // Act
        await sut.BulkUpdateCommand.ExecuteAsync(null);

        // Assert
        sut.UpdateStatus.Should().Contain("urun stoku guncellendi");
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task BulkUpdateCommand_NoChanges_ShouldReportNoUpdates()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        // First bulk update to equalize all MevcutStok == YeniStok
        await sut.BulkUpdateCommand.ExecuteAsync(null);

        // Act — second bulk update with no remaining changes
        await sut.BulkUpdateCommand.ExecuteAsync(null);

        // Assert
        sut.UpdateStatus.Should().Be("Guncellenecek stok degisikligi bulunamadi.");
    }
}
