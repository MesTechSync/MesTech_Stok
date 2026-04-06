using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Product.Queries.GetProducts;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockUpdateAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private StockUpdateAvaloniaViewModel CreateSut()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProductsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ProductDto>());
        return new StockUpdateAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ITenantProvider>());
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
        sut.StockItems.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
        sut.IsEmpty.Should().BeTrue();
    }

    // ── 3-State: BulkUpdate with no items ──

    [Fact]
    public async Task BulkUpdateCommand_NoItems_ShouldReportNoUpdates()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        await sut.BulkUpdateCommand.ExecuteAsync(null);

        // Assert
        sut.UpdateStatus.Should().Be("Guncellenecek stok degisikligi bulunamadi.");
        sut.IsLoading.Should().BeFalse();
    }
}
