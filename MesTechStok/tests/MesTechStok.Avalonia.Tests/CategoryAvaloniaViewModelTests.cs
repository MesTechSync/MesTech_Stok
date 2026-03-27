using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CategoryAvaloniaViewModelTests
{
    private static CategoryAvaloniaViewModel CreateSut()
    {
        return new CategoryAvaloniaViewModel(Mock.Of<IMediator>());
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
    public async Task LoadAsync_ShouldPopulateCategoriesWithHierarchy()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.Categories.Should().HaveCount(12);
        sut.TotalCount.Should().Be(12);
    }

    [Fact]
    public async Task LoadAsync_ShouldContainRootAndChildCategories()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        await sut.LoadAsync();

        // Assert — root categories have ParentCategory == "—"
        var roots = sut.Categories.Where(c => c.ParentCategory == "\u2014").ToList();
        roots.Should().HaveCountGreaterOrEqualTo(3);
        roots.Select(r => r.Name.Trim()).Should().Contain("Elektronik");
        roots.Select(r => r.Name.Trim()).Should().Contain("Giyim");
        roots.Select(r => r.Name.Trim()).Should().Contain("Kozmetik");
    }

    // ── 3-State: Search/Filter ──

    [Fact]
    public async Task SearchText_ShouldFilterCategoriesByName()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SearchText = "Giyim";

        // Assert — "Giyim", "Kadin Giyim", "Erkek Giyim"
        sut.Categories.Should().HaveCount(3);
        sut.Categories.Should().OnlyContain(c =>
            c.Name.Contains("Giyim", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SearchText_ByPlatform_ShouldFilterCorrectly()
    {
        // Arrange
        var sut = CreateSut();
        await sut.LoadAsync();

        // Act
        sut.SearchText = "Hepsiburada";

        // Assert
        sut.Categories.Should().NotBeEmpty();
        sut.Categories.Should().OnlyContain(c => c.Platform == "Hepsiburada");
    }
}
