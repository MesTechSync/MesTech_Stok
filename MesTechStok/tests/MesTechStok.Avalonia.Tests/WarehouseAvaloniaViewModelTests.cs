using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class WarehouseAvaloniaViewModelTests
{
    private static WarehouseAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        return new WarehouseAvaloniaViewModel(mediatorMock.Object);
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
        sut.IsAddingWarehouse.Should().BeFalse();
        sut.NewWarehouseName.Should().BeEmpty();
        sut.NewWarehouseCapacity.Should().Be(1000);
        sut.Items.Should().BeEmpty();
    }

    // ── 3-State: Loading → Loaded ──

    [Fact]
    public async Task LoadAsync_ShouldPopulateWarehouseCards()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Items.Should().HaveCount(3);
        sut.TotalCount.Should().Be(3);
        sut.Items[0].Name.Should().Be("Ana Depo");
    }

    // ── 3-State: Search/Filter ──

    [Fact]
    public async Task SearchText_ShouldFilterWarehousesByNameOrLocation()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SearchText = "Tuzla";

        // Assert
        sut.Items.Should().HaveCount(1);
        sut.Items[0].Name.Should().Be("Yedek Depo");
    }

    // ── 3-State: Add Warehouse ──

    [Fact]
    public async Task SaveWarehouseCommand_ShouldAddNewWarehouseToList()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        sut.AddWarehouseCommand.Execute(null);
        sut.NewWarehouseName = "Test Depo";
        sut.NewWarehouseLocation = "Ankara";
        sut.NewWarehouseCapacity = 5000;

        // Act
        await sut.SaveWarehouseCommand.ExecuteAsync(null);

        // Assert
        sut.Items.Should().HaveCount(4);
        sut.Items.Should().Contain(w => w.Name == "Test Depo");
        sut.IsAddingWarehouse.Should().BeFalse();
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public void WarehouseCardDto_CapacityPercent_ShouldCalculateCorrectly()
    {
        // Arrange
        var dto = new WarehouseCardDto
        {
            Capacity = 10000,
            UsedCapacity = 4520
        };

        // Assert
        dto.CapacityPercent.Should().Be(45);
        dto.CapacityColor.Should().Be("#22C55E", "below 70% is green");
    }
}
