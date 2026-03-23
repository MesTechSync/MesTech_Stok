using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ReturnListAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ReturnListAvaloniaViewModel _sut;

    public ReturnListAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new ReturnListAvaloniaViewModel(_mediatorMock.Object);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateReturns()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.Returns.Should().HaveCount(7);
        _sut.TotalCount.Should().Be(7);
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingState()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ReturnListAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task SelectedStatus_ShouldFilterReturns()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedStatus = "Beklemede";

        // Assert
        _sut.Returns.Should().OnlyContain(r => r.Durum == "Beklemede");
        _sut.TotalCount.Should().Be(2); // IAD-2001, IAD-2007
    }

    [Fact]
    public async Task SearchText_ShouldFilterByCustomerOrOrderNo()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "Ahmet";

        // Assert
        _sut.Returns.Should().HaveCount(1);
        _sut.Returns[0].Musteri.Should().Contain("Ahmet");
    }

    [Fact]
    public async Task StatusFilter_CombinedWithSearch_ShouldNarrowResults()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act — filter by status then search
        _sut.SelectedStatus = "Tumu";
        _sut.SearchText = "IAD-200";

        // Assert — all items match IAD-200x pattern
        _sut.Returns.Should().HaveCount(7);
        _sut.Returns.Should().OnlyContain(r =>
            r.IadeNo.Contains("IAD-200", StringComparison.OrdinalIgnoreCase));
    }
}
