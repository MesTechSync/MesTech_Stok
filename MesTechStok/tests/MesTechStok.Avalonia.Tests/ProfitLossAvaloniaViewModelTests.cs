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
public class ProfitLossAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ProfitLossAvaloniaViewModel _sut;

    public ProfitLossAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetProfitReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfitReportDto());
        _sut = new ProfitLossAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetPeriodLabelToCurrentMonth()
    {
        var now = DateTime.Now;
        _sut.PeriodLabel.Should().Contain(now.Year.ToString());
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.LineItems.Should().BeEmpty();
        _sut.TotalRevenue.Should().Contain("0.00 TL");
    }

    [Fact]
    public async Task LoadAsync_WithEmptyData_ShouldCompleteWithoutError()
    {
        // Act
        await _sut.LoadAsync();

        // Assert
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }

    [Fact]
    public async Task PrevThenNext_ShouldReturnToOriginalPeriod()
    {
        // Arrange
        var originalLabel = _sut.PeriodLabel;

        // Act
        await _sut.PrevMonthCommand.ExecuteAsync(null);
        await _sut.NextMonthCommand.ExecuteAsync(null);

        // Assert
        _sut.PeriodLabel.Should().Be(originalLabel);
    }
}
