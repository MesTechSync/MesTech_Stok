using FluentAssertions;
using MesTech.Avalonia.ViewModels;
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
        _sut = new ProfitLossAvaloniaViewModel(_mediatorMock.Object);
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
    public async Task LoadAsync_ShouldPopulateKPIsAndLineItems()
    {
        // Act
        await _sut.LoadAsync();

        // Assert — revenue=125480, expenses=87320, profit=38160, margin~30.4%
        _sut.TotalRevenue.Should().Contain("125");
        _sut.TotalExpenses.Should().Contain("87");
        _sut.NetProfit.Should().Contain("38");
        _sut.ProfitMarginText.Should().Contain("%");
        _sut.ProfitMarginText.Should().Contain("30");
        _sut.LineItems.Should().HaveCount(7);
        _sut.LineItems.Should().Contain(x => x.Type == "Revenue");
        _sut.LineItems.Should().Contain(x => x.Type == "Expense");
        _sut.IsEmpty.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task PrevMonth_ShouldDecrementPeriodAndReload()
    {
        // Arrange
        var initialLabel = _sut.PeriodLabel;

        // Act
        await _sut.PrevMonthCommand.ExecuteAsync(null);

        // Assert
        _sut.PeriodLabel.Should().NotBe(initialLabel);
        _sut.LineItems.Should().HaveCount(7);
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task NextMonth_ShouldIncrementPeriodAndReload()
    {
        // Arrange — go back first then forward
        await _sut.PrevMonthCommand.ExecuteAsync(null);
        var afterPrevLabel = _sut.PeriodLabel;

        // Act
        await _sut.NextMonthCommand.ExecuteAsync(null);

        // Assert
        _sut.PeriodLabel.Should().NotBe(afterPrevLabel);
        _sut.LineItems.Should().HaveCount(7);
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
