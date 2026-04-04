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
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProfitReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfitReportDto());
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
    public async Task LoadAsync_WithEmptyData_ShouldCompleteWithoutError()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsLoading.Should().BeFalse();
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
