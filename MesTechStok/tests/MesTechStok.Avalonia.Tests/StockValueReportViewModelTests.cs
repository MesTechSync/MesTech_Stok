using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class StockValueReportViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly StockValueReportViewModel _sut;

    public StockValueReportViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new StockValueReportViewModel(_mediatorMock.Object);
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.Title.Should().Be("Stok Deger Raporu");
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.IsEmpty.Should().BeFalse();
        _sut.WarehouseStocks.Should().BeEmpty();
        _sut.AgingBuckets.Should().BeEmpty();
        _sut.TotalStockValueText.Should().Contain("TL");
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateWarehouses()
    {
        await _sut.LoadAsync();

        _sut.WarehouseStocks.Should().NotBeEmpty();
        _sut.WarehouseStocks.Should().HaveCountGreaterThanOrEqualTo(3);
        _sut.WarehouseStocks.Select(w => w.WarehouseName).Should().Contain("Ana Depo - Istanbul");
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateAgingBuckets()
    {
        await _sut.LoadAsync();

        _sut.AgingBuckets.Should().NotBeEmpty();
        _sut.AgingBuckets.Should().HaveCountGreaterThanOrEqualTo(4);
    }

    [Fact]
    public async Task LoadAsync_ShouldCalculateTotals()
    {
        await _sut.LoadAsync();

        _sut.TotalStockValueText.Should().NotBe("0.00 TL");
        _sut.TotalSkuText.Should().NotBe("0");
        _sut.AverageTurnoverText.Should().Contain("gun");
    }
}
