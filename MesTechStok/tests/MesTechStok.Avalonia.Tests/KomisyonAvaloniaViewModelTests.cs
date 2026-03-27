using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class KomisyonAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly KomisyonAvaloniaViewModel _sut;

    public KomisyonAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new KomisyonAvaloniaViewModel(_mediatorMock.Object, Mock.Of<MesTech.Domain.Interfaces.ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.SelectedPlatform.Should().Be("Tumu");
        _sut.SelectedCategory.Should().Be("Tumu");
        _sut.SearchText.Should().BeEmpty();
        _sut.Platforms.Should().Contain("Trendyol");
        _sut.Categories.Should().Contain("Giyim");
        _sut.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateItemsAndKPIs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Items.Should().HaveCount(6);
        _sut.TrendyolAvgRate.Should().Be("%12.5");
        _sut.HepsiburadaAvgRate.Should().Be("%15.0");
        _sut.CiceksepetiAvgRate.Should().Be("%18.0");
        _sut.N11AvgRate.Should().Be("%11.0");
        _sut.IsLoading.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task FilterByPlatform_ShouldNarrowResults()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedPlatform = "Trendyol";

        // Assert
        _sut.Items.Should().HaveCount(2);
        _sut.Items.Should().OnlyContain(x => x.Platform == "Trendyol");
    }

    [Fact]
    public async Task FilterByCategory_ShouldNarrowResults()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedCategory = "Giyim";

        // Assert
        _sut.Items.Should().HaveCount(2);
        _sut.Items.Should().OnlyContain(x => x.Category == "Giyim");
    }

    [Fact]
    public async Task SearchText_ShouldFilterByPlatformOrCategory()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "Ciceksepeti";

        // Assert
        _sut.Items.Should().HaveCount(1);
        _sut.Items[0].Platform.Should().Be("Ciceksepeti");
    }
}
