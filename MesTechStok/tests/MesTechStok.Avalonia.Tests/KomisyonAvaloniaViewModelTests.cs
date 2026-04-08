using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetCommissionSummary;
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
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCommissionSummaryQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CommissionSummaryDto { ByPlatform = [] });
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
    public async Task LoadAsync_ShouldCompleteWithoutError()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.TrendyolAvgRate.Should().Be("%0.0");
        _sut.HepsiburadaAvgRate.Should().Be("%0.0");
    }

    [Fact]
    public async Task FilterByPlatform_WhenEmpty_ShouldRemainEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedPlatform = "Trendyol";

        // Assert — empty mock data
        _sut.Items.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task FilterByCategory_WhenEmpty_ShouldRemainEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedCategory = "Giyim";

        // Assert
        _sut.Items.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task SearchText_WhenEmpty_ShouldRemainEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "Ciceksepeti";

        // Assert
        _sut.Items.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
    }
}
