using FluentAssertions;
using MesTech.Application.Features.Reports.SalesAnalytics;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class SalesAnalyticsViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly SalesAnalyticsViewModel _sut;

    public SalesAnalyticsViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetSalesAnalyticsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SalesAnalyticsDto
            {
                TotalRevenue = 125_000m,
                TotalOrders = 342,
                AverageOrderValue = 365.50m,
                ConversionRate = 3.2m,
                TopSellingPlatform = "Trendyol",
                PlatformBreakdown = new()
                {
                    new() { Platform = "Trendyol", Revenue = 75_000m, Orders = 200, Percentage = 60m },
                    new() { Platform = "Hepsiburada", Revenue = 30_000m, Orders = 82, Percentage = 24m },
                    new() { Platform = "Amazon", Revenue = 20_000m, Orders = 60, Percentage = 16m },
                },
                TopProducts = new()
                {
                    new() { ProductName = "Urun A", SKU = "SKU-001", QuantitySold = 120, Revenue = 24_000m },
                    new() { ProductName = "Urun B", SKU = "SKU-002", QuantitySold = 95, Revenue = 19_000m },
                    new() { ProductName = "Urun C", SKU = "SKU-003", QuantitySold = 80, Revenue = 16_000m },
                    new() { ProductName = "Urun D", SKU = "SKU-004", QuantitySold = 60, Revenue = 12_000m },
                    new() { ProductName = "Urun E", SKU = "SKU-005", QuantitySold = 45, Revenue = 9_000m },
                }
            });
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());
        _sut = new SalesAnalyticsViewModel(_mediatorMock.Object, currentUserMock.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.Title.Should().Be("Satis Analizi");
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.SelectedPlatform.Should().Be("Tumu");
        _sut.PlatformSales.Should().BeEmpty();
        _sut.TopProducts.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_ShouldPopulatePlatformOptions()
    {
        _sut.PlatformOptions.Should().Contain("Tumu");
        _sut.PlatformOptions.Should().Contain("Trendyol");
        _sut.PlatformOptions.Should().Contain("Amazon");
        _sut.PlatformOptions.Count.Should().BeGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulatePlatformSales()
    {
        await _sut.LoadAsync();

        _sut.PlatformSales.Should().NotBeEmpty();
        _sut.PlatformSales.Select(p => p.Platform).Should().Contain("Trendyol");
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateTopProducts()
    {
        await _sut.LoadAsync();

        _sut.TopProducts.Should().NotBeEmpty();
        _sut.TopProducts.Should().HaveCountGreaterThanOrEqualTo(5);
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateSummary()
    {
        await _sut.LoadAsync();

        _sut.TotalSalesText.Should().NotBe("0.00 TL");
        _sut.OrderCountText.Should().NotBe("0");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SalesAnalyticsViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        await _sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        _sut.HasError.Should().BeFalse();
    }
}
