using FluentAssertions;
using MediatR;
using MesTech.Application.Features.Dashboard.Queries.GetTopProducts;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ProductsAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private ProductsAvaloniaViewModel CreateSut()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetTopProductsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TopProductDto>().AsReadOnly());
        return new ProductsAvaloniaViewModel(_mediatorMock.Object, Mock.Of<MesTech.Domain.Interfaces.ICurrentUserService>());
    }

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
        sut.SelectedPlatform.Should().Be("Tumu");
        sut.TotalCount.Should().Be(0);
        sut.Products.Should().BeEmpty();
        sut.IsListView.Should().BeTrue();
        sut.IsGridView.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetIsLoadingAndCompleteWithoutError()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_FilterByPlatform_ShouldNotThrow()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SelectedPlatform = "Trendyol";

        // Assert — empty mock data, filter yields empty
        sut.Products.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_SearchByBarcode_WhenNoData_ShouldBeEmpty()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act — search on empty data
        sut.SearchText = "8806095380001";

        // Assert
        sut.Products.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_WhenNoMatchingProducts_ShouldSetEmptyState()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act — search for non-existent product
        sut.SearchText = "NONEXISTENT_PRODUCT_XYZ";

        // Assert
        sut.IsEmpty.Should().BeTrue();
        sut.Products.Should().BeEmpty();
        sut.TotalCount.Should().Be(0);
    }
}
