using FluentAssertions;
using MesTech.Application.Queries.GetWarehouses;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class WarehouseAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private WarehouseAvaloniaViewModel CreateSut()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetWarehousesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<WarehouseListDto>());
        return new WarehouseAvaloniaViewModel(_mediatorMock.Object);
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

    // ── 3-State: Loading → Loaded (empty) ──

    [Fact]
    public async Task LoadAsync_WithEmptyData_ShouldCompleteWithoutError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeTrue();
        sut.Items.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
    }

    // ── 3-State: Add Warehouse to empty list ──

    [Fact]
    public async Task SaveWarehouseCommand_ShouldAddNewWarehouseToEmptyList()
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
        sut.Items.Should().HaveCount(1);
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
