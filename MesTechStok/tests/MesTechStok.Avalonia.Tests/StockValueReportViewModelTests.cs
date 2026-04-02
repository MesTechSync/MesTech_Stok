using FluentAssertions;
using MesTech.Application.Features.Stock.Queries.GetStockValueReport;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
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
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetStockValueReportQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StockValueReportResult
            {
                TotalValue = 485_750.00m,
                TotalCostValue = 312_400.00m,
                UnrealizedProfitLoss = 173_350.00m,
                TotalProducts = 1_245,
                ZeroStockProducts = 38,
                TopValueProducts = new List<StockValueLineDto>
                {
                    new() { ProductId = Guid.NewGuid(), ProductName = "Ana Depo - Istanbul", SKU = "SKU-DEPO-01", Stock = 520, Price = 185.00m, CostPrice = 120.00m, TotalValue = 96_200.00m, TotalCost = 62_400.00m },
                    new() { ProductId = Guid.NewGuid(), ProductName = "Trendyol FBT Depo - Gebze", SKU = "SKU-DEPO-02", Stock = 340, Price = 210.00m, CostPrice = 135.00m, TotalValue = 71_400.00m, TotalCost = 45_900.00m },
                    new() { ProductId = Guid.NewGuid(), ProductName = "Hepsiburada Depo - Ankara", SKU = "SKU-DEPO-03", Stock = 280, Price = 195.00m, CostPrice = 125.00m, TotalValue = 54_600.00m, TotalCost = 35_000.00m },
                    new() { ProductId = Guid.NewGuid(), ProductName = "Amazon FBA Depo - Kocaeli", SKU = "SKU-DEPO-04", Stock = 150, Price = 250.00m, CostPrice = 160.00m, TotalValue = 37_500.00m, TotalCost = 24_000.00m },
                }
            });
        var currentUserMock = new Mock<ICurrentUserService>();
        currentUserMock.Setup(x => x.TenantId).Returns(Guid.NewGuid());
        _sut = new StockValueReportViewModel(_mediatorMock.Object, currentUserMock.Object);
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
