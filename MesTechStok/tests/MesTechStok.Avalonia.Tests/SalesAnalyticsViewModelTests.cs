using FluentAssertions;
using MesTech.Avalonia.ViewModels;
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
        _sut = new SalesAnalyticsViewModel(_mediatorMock.Object);
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
