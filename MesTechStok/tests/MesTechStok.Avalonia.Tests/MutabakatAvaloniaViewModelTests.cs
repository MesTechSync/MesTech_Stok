using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class MutabakatAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly MutabakatAvaloniaViewModel _sut;

    public MutabakatAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new MutabakatAvaloniaViewModel(_mediatorMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.TotalRecords.Should().Be("0");
        _sut.MatchedCount.Should().Be("0");
        _sut.UnmatchedCount.Should().Be("0");
        _sut.MatchScoreText.Should().Be("%0");
        _sut.SelectedSource.Should().Be("Tumu");
        _sut.SelectedStatusFilter.Should().Be("Tumu");
        _sut.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateItemsAndKPIs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — 6 items: 3 Eslesti, 2 Eslesmedi, 1 Beklemede
        _sut.Items.Should().HaveCount(6);
        _sut.TotalRecords.Should().Be("6");
        _sut.MatchedCount.Should().Be("3");
        _sut.UnmatchedCount.Should().Be("2");
        _sut.MatchScoreText.Should().Be("%50");
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task FilterByStatus_Eslesmedi_ShouldNarrowResults()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedStatusFilter = "Eslesmedi";

        // Assert
        _sut.Items.Should().HaveCount(2);
        _sut.Items.Should().OnlyContain(x => x.Status == "Eslesmedi");
    }

    [Fact]
    public async Task FilterBySource_ShouldNarrowResults()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedSource = "Cari - Trendyol";

        // Assert
        _sut.Items.Should().HaveCount(2);
        _sut.Items.Should().OnlyContain(x => x.Source == "Cari - Trendyol");
    }

    [Fact]
    public async Task SearchText_ShouldFilterByDescriptionOrReference()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "hakedis";

        // Assert — 3 items contain "hakedis" in Description
        _sut.Items.Should().HaveCount(3);
        _sut.Items.Should().OnlyContain(x => x.Description.Contains("hakedis", StringComparison.OrdinalIgnoreCase));
    }
}
