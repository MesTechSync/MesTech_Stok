using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ProductsAvaloniaViewModelTests
{
    private static ProductsAvaloniaViewModel CreateSut()
    {
        var mediatorMock = new Mock<IMediator>();
        return new ProductsAvaloniaViewModel(mediatorMock.Object, Mock.Of<MesTech.Domain.Interfaces.ICurrentUserService>());
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
    public async Task LoadAsync_ShouldSetIsLoadingAndPopulateProducts()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.Products.Should().NotBeEmpty();
        sut.TotalCount.Should().BeGreaterThan(0);
        sut.IsEmpty.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_FilterByPlatform_ShouldReduceResults()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();
        var totalBefore = sut.TotalCount;

        // Act
        sut.SelectedPlatform = "Trendyol";

        // Assert
        sut.TotalCount.Should().BeLessThan(totalBefore);
        sut.Products.Should().OnlyContain(p => p.Platform == "Trendyol");
    }

    [Fact]
    public async Task LoadAsync_SearchByBarcode_ShouldMatchSingleProduct()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act — search for a known barcode fragment
        sut.SearchText = "8806095380001";

        // Assert
        sut.Products.Should().HaveCount(1);
        sut.Products.First().SKU.Should().Be("TRY-ELK-001");
        sut.SearchMatchInfo.Should().Contain("Barkod");
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
