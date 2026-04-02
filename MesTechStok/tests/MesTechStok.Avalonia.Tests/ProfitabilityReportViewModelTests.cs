using FluentAssertions;
using MesTech.Application.Features.Reports.ProfitabilityReport;
using MesTech.Avalonia.ViewModels.Accounting;
using MesTech.Domain.Interfaces;
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
        _mediatorMock.Setup(m => m.Send(It.IsAny<ProfitabilityReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfitabilityReportDto
            {
                FromDate = DateTime.Now.AddMonths(-1),
                ToDate = DateTime.Now,
                TotalRevenue = 485_000.00m,
                TotalCost = 245_000.00m,
                TotalCommission = 58_200.00m,
                TotalShipping = 32_400.00m,
                TotalTax = 24_250.00m,
                NetProfit = 125_150.00m,
                ProfitMargin = 25.8m,
                TotalOrders = 1_842,
                ByPlatform = new List<PlatformProfitDto>
                {
                    new("Trendyol", 820, 195_000m, 98_000m, 23_400m, 13_000m, 9_750m, 50_850m, 26.1m),
                    new("Hepsiburada", 412, 120_000m, 60_000m, 14_400m, 8_000m, 6_000m, 31_600m, 26.3m),
                    new("Amazon", 285, 85_000m, 42_500m, 10_200m, 5_700m, 4_250m, 22_350m, 26.3m),
                    new("N11", 180, 50_000m, 25_000m, 6_000m, 3_400m, 2_500m, 13_100m, 26.2m),
                    new("Ciceksepeti", 145, 35_000m, 19_500m, 4_200m, 2_300m, 1_750m, 7_250m, 20.7m),
                },
                TopProfitableProducts = new List<ProductProfitDto>
                {
                    new(Guid.NewGuid(), "SKU-001", "Samsung Galaxy A54", 320, 96_000m, 48_000m, 28_800m, 30.0m),
                },
                LeastProfitableProducts = new List<ProductProfitDto>
                {
                    new(Guid.NewGuid(), "SKU-099", "USB Kablo 1m", 1200, 12_000m, 10_800m, -600m, -5.0m),
                }
            });
        _sut = new ProfitabilityReportViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
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
