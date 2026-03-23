using FluentAssertions;
using MesTech.Avalonia.ViewModels;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockMovementAvaloniaViewModelTests
{
    private static StockMovementAvaloniaViewModel CreateSut()
    {
        return new StockMovementAvaloniaViewModel();
    }

    // ── 3-State: Default / Empty ──

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
        sut.ChangedCount.Should().Be(0);
        sut.UpdateStatus.Should().BeEmpty();
        sut.Items.Should().BeEmpty();
    }

    // ── 3-State: Loading → Loaded ──

    [Fact]
    public async Task LoadAsync_ShouldPopulateItemsAndSetTotalCount()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.Items.Should().HaveCount(10);
        sut.TotalCount.Should().Be(10);
        sut.ChangedCount.Should().Be(0, "initial YeniStok == MevcutStok");
        sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task ChangedCount_ShouldUpdateWhenItemYeniStokChanges()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act — modify one item's YeniStok
        sut.Items[0].YeniStok = sut.Items[0].MevcutStok + 5;

        // Assert
        sut.ChangedCount.Should().Be(1);
    }

    // ── 3-State: BulkUpdate action ──

    [Fact]
    public async Task BulkUpdateCommand_NoChanges_ShouldSetUpdateStatus()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act — no changes made, execute bulk update
        await sut.BulkUpdateCommand.ExecuteAsync(null);

        // Assert
        sut.UpdateStatus.Should().Be("Degisiklik yapilmadi.");
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task BulkUpdateCommand_WithChanges_ShouldApplyAndReportCount()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        sut.Items[0].YeniStok = 999;
        sut.Items[1].YeniStok = 888;

        // Act
        await sut.BulkUpdateCommand.ExecuteAsync(null);

        // Assert
        sut.UpdateStatus.Should().Contain("2 urun");
        sut.ChangedCount.Should().Be(0, "after bulk update MevcutStok == YeniStok");
        sut.Items[0].MevcutStok.Should().Be(999);
        sut.Items[1].MevcutStok.Should().Be(888);
    }
}
