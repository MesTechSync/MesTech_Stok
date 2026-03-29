using FluentAssertions;
using MesTech.Application.DTOs.Accounting;
using MesTech.Application.Features.Accounting.Queries.GetProfitReport;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class KarlilikAnaliziAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly KarlilikAnaliziAvaloniaViewModel _sut;

    public KarlilikAnaliziAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new KarlilikAnaliziAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldInitializeWithDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.TotalRevenue.Should().Contain("0,00 TL");
        _sut.TotalProfit.Should().Contain("0,00 TL");
        _sut.AverageMargin.Should().Be("%0.0");
        _sut.SelectedPlatform.Should().Be("Tumu");
        _sut.SelectedCategory.Should().Be("Tumu");
        _sut.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateItemsAndKPIs()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — 5 products, revenue=73500
        _sut.Items.Should().HaveCount(5);
        _sut.TotalRevenue.Should().Contain("73");
        _sut.TotalProfit.Should().NotContain("0,00 TL");
        _sut.AverageMargin.Should().Contain("%");
        _sut.IsLoading.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task SearchText_ShouldFilterByProductName()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "Gomlek";

        // Assert
        _sut.Items.Should().HaveCount(1);
        _sut.Items[0].ProductName.Should().Contain("Gomlek");
    }

    [Fact]
    public async Task SearchText_ShortTerm_ShouldNotFilter()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SearchText = "G";

        // Assert — single char should not trigger filter
        _sut.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task LoadAsync_ShouldTransitionLoadingStates()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(KarlilikAnaliziAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        // Act
        await _sut.LoadAsync();

        // Assert
        loadingStates.Should().Contain(true);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorThrows_SetsHasError()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetProfitReportQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var sut = new KarlilikAnaliziAvaloniaViewModel(mediator.Object, Mock.Of<ICurrentUserService>());
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().NotBeNullOrEmpty();
        sut.IsLoading.Should().BeFalse(); // KÇ-13
    }

    [Fact]
    public async Task LoadAsync_WhenNullResponse_SetsIsEmpty()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetProfitReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfitReportDto?)null);

        var sut = new KarlilikAnaliziAvaloniaViewModel(mediator.Object, Mock.Of<ICurrentUserService>());
        await sut.LoadAsync();

        sut.IsEmpty.Should().BeTrue();
        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
    }
}
