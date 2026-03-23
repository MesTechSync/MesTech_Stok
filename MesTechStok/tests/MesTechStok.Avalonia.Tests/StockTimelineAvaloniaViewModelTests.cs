using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockTimelineAvaloniaViewModelTests
{
    private static StockTimelineAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        return new StockTimelineAvaloniaViewModel(mediatorMock.Object);
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
        sut.Movements.Should().BeEmpty();
    }

    // ── 3-State: Loading → Loaded ──

    [Fact]
    public async Task LoadAsync_ShouldPopulateMovementsAndSetTotalCount()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Movements.Should().HaveCount(10);
        sut.TotalCount.Should().Be(10);
    }

    [Fact]
    public async Task LoadAsync_MovementTypes_ShouldCoverAllFiveTypes()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — 5 movement types: Sale, Purchase, Transfer, Return, Adjustment
        var types = sut.Movements.Select(m => m.MovementType).Distinct().ToList();
        types.Should().Contain("Sale");
        types.Should().Contain("Purchase");
        types.Should().Contain("Transfer");
        types.Should().Contain("Return");
        types.Should().Contain("Adjustment");
    }

    // ── 3-State: DTO computed properties ──

    [Fact]
    public async Task StockTimelineItemDto_ComputedProperties_ShouldReturnCorrectValues()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Assert — verify computed display properties
        var sale = sut.Movements.First(m => m.MovementType == "Sale");
        sale.TypeText.Should().Be("Satis");
        sale.TypeColor.Should().Be("#DC2626");
        sale.QuantityColor.Should().Be("#DC2626", "negative quantity is red");

        var purchase = sut.Movements.First(m => m.MovementType == "Purchase");
        purchase.TypeText.Should().Be("Alim");
        purchase.TypeColor.Should().Be("#16A34A");
        purchase.QuantityText.Should().StartWith("+");
    }

    [Fact]
    public async Task RefreshCommand_ShouldReloadMovements()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        var initialCount = sut.Movements.Count;

        // Act
        await sut.RefreshCommand.ExecuteAsync(null);

        // Assert
        sut.Movements.Should().HaveCount(initialCount);
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}
