using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Queries.GetStockMovements;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockTimelineAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITenantProvider> _tenantProviderMock = new();

    private StockTimelineAvaloniaViewModel CreateSut()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetStockMovementsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StockMovementDto>)Array.Empty<StockMovementDto>());
        return new StockTimelineAvaloniaViewModel(_mediatorMock.Object, _tenantProviderMock.Object);
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
        sut.Movements.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task RefreshCommand_ShouldReloadWithoutError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.RefreshCommand.ExecuteAsync(null);

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}
