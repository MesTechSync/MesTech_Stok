using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class CashFlowReportViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly CashFlowReportViewModel _sut;

    public CashFlowReportViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new CashFlowReportViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.Title.Should().Be("Nakit Akis Raporu");
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.MonthlyFlows.Should().BeEmpty();
        _sut.TotalInflowText.Should().Contain("TL");
        _sut.TotalOutflowText.Should().Contain("TL");
        _sut.NetFlowText.Should().Contain("TL");
    }

    [Fact]
    public void Constructor_ShouldSetDefaultDateRange()
    {
        _sut.DateFrom.Should().NotBeNull();
        _sut.DateTo.Should().NotBeNull();
        _sut.DateFrom!.Value.Year.Should().Be(DateTime.Now.Year);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateMonthlyFlows()
    {
        await _sut.LoadAsync();

        _sut.MonthlyFlows.Should().NotBeEmpty();
        _sut.MonthlyFlows.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateTotals()
    {
        await _sut.LoadAsync();

        _sut.TotalInflowText.Should().NotBe("0.00 TL");
        _sut.TotalOutflowText.Should().NotBe("0.00 TL");
        _sut.NetFlowText.Should().NotBe("0.00 TL");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CashFlowReportViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        await _sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        _sut.HasError.Should().BeFalse();
    }
}
