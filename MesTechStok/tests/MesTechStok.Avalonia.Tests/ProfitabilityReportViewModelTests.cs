using FluentAssertions;
using MesTech.Avalonia.ViewModels.Accounting;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ProfitabilityReportViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly ProfitabilityReportViewModel _sut;

    public ProfitabilityReportViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new ProfitabilityReportViewModel(_mediatorMock.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.Title.Should().Be("Karlilik Raporu");
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.TotalIncome.Should().Be(0);
        _sut.TotalExpense.Should().Be(0);
        _sut.TotalNetProfit.Should().Be(0);
        _sut.PlatformProfits.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulatePlatformProfits()
    {
        await _sut.LoadAsync();

        _sut.PlatformProfits.Should().NotBeEmpty();
        _sut.PlatformProfits.Should().HaveCountGreaterThanOrEqualTo(5);
        _sut.PlatformProfits.Select(p => p.Platform).Should().Contain("Trendyol");
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateTotals()
    {
        await _sut.LoadAsync();

        _sut.TotalIncome.Should().BeGreaterThan(0);
        _sut.TotalExpense.Should().BeGreaterThan(0);
        _sut.TotalNetProfit.Should().BeGreaterThan(0);
        _sut.TotalIncomeFormatted.Should().Contain("TL");
        _sut.TotalNetProfitFormatted.Should().Contain("TL");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ProfitabilityReportViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        await _sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        _sut.HasError.Should().BeFalse();
    }
}
