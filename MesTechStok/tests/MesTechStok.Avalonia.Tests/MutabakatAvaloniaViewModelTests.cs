using FluentAssertions;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationDashboard;
using MesTech.Application.Features.Accounting.Queries.GetReconciliationMatches;
using MesTech.Application.DTOs.Accounting;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
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
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetReconciliationDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReconciliationDashboardDto());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetReconciliationMatchesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReconciliationMatchDto>().AsReadOnly());
        _sut = new MutabakatAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
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
    public async Task LoadAsync_ShouldCompleteWithoutError()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — empty mock data: all zeroes
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.TotalRecords.Should().Be("0");
        _sut.MatchedCount.Should().Be("0");
        _sut.UnmatchedCount.Should().Be("0");
        _sut.MatchScoreText.Should().Be("%0");
    }

    [Fact]
    public async Task FilterByStatus_WhenEmpty_ShouldRemainEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedStatusFilter = "Eslesmedi";

        // Assert
        _sut.Items.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task FilterBySource_WhenEmpty_ShouldRemainEmpty()
    {
        // Arrange
        await _sut.LoadAsync();

        // Act
        _sut.SelectedSource = "Cari - Trendyol";

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
        _sut.SearchText = "hakedis";

        // Assert
        _sut.Items.Should().BeEmpty();
        _sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorThrows_SetsHasError()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetReconciliationDashboardQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB down"));

        var sut = new MutabakatAvaloniaViewModel(mediator.Object, Mock.Of<ICurrentUserService>());
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().NotBeNullOrEmpty();
        sut.IsLoading.Should().BeFalse(); // KÇ-13
    }
}
