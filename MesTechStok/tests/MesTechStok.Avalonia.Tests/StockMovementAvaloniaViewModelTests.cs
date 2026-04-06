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
public class StockMovementAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITenantProvider> _tenantProviderMock = new();

    private StockMovementAvaloniaViewModel CreateSut()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetStockMovementsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<StockMovementDto>)Array.Empty<StockMovementDto>());
        return new StockMovementAvaloniaViewModel(_mediatorMock.Object, _tenantProviderMock.Object);
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
        sut.Items.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
        sut.ChangedCount.Should().Be(0);
        sut.IsEmpty.Should().BeTrue();
    }

    // ── 3-State: BulkUpdate with no items ──

    [Fact]
    public async Task BulkUpdateCommand_NoItems_ShouldSetUpdateStatus()
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
}
