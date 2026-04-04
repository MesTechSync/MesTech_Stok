using FluentAssertions;
using MesTech.Application.Queries.GetCategories;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CategoryAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();

    private CategoryAvaloniaViewModel CreateSut()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCategoriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryListDto>().AsReadOnly());
        return new CategoryAvaloniaViewModel(_mediatorMock.Object);
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
        sut.Categories.Should().BeEmpty();
    }

    // ── 3-State: Loading → Loaded ──

    [Fact]
    public async Task LoadAsync_ShouldCompleteWithoutError()
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
    public async Task LoadAsync_WhenEmpty_ShouldSetEmptyState()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.Categories.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
        sut.TotalCount.Should().Be(0);
    }

    // ── 3-State: Search/Filter ──

    [Fact]
    public async Task SearchText_WhenEmpty_ShouldRemainEmpty()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SearchText = "Giyim";

        // Assert — no data to filter
        sut.Categories.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task SearchText_ByPlatform_WhenEmpty_ShouldRemainEmpty()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SearchText = "Hepsiburada";

        // Assert
        sut.Categories.Should().BeEmpty();
        sut.IsEmpty.Should().BeTrue();
    }
}
