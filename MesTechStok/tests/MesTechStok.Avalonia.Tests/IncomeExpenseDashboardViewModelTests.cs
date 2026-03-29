using FluentAssertions;
using MesTech.Avalonia.ViewModels.Accounting;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class IncomeExpenseDashboardViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly IncomeExpenseDashboardViewModel _sut;

    public IncomeExpenseDashboardViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new IncomeExpenseDashboardViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.Title.Should().Be("Gelir / Gider Ozeti");
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.TotalIncome.Should().Be(0);
        _sut.TotalExpense.Should().Be(0);
        _sut.NetProfit.Should().Be(0);
        _sut.RecentTransactions.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateTransactions()
    {
        await _sut.LoadAsync();

        _sut.RecentTransactions.Should().NotBeEmpty();
        _sut.RecentTransactions.Count.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateKPIs()
    {
        await _sut.LoadAsync();

        _sut.TotalIncome.Should().BeGreaterThan(0);
        _sut.TotalExpense.Should().BeGreaterThan(0);
        _sut.TotalIncomeFormatted.Should().Contain("TL");
        _sut.TotalExpenseFormatted.Should().Contain("TL");
        _sut.NetProfitFormatted.Should().Contain("TL");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(IncomeExpenseDashboardViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        await _sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }
}
