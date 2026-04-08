using System.Collections.ObjectModel;
using FluentAssertions;
using MediatR;
using MesTech.Application.DTOs;
using MesTech.Application.Features.Product.Queries.GetProducts;
using MesTech.Application.Queries.GetCategories;
using MesTech.Avalonia.Services;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Common;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

/// <summary>
/// TEST 2/4 — Product → DataGrid ViewModel mapping.
/// ProductsAvaloniaVM.LoadAsync → IMediator → ObservableCollection doluyor mu?
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "ViewModel")]
public class ProductsViewModelMappingTests
{
    private static readonly Guid TenantId = Guid.Parse("00000000-0000-0000-0000-000000000099");

    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ICurrentUserService> _userMock = new();
    private readonly Mock<IToastService> _toastMock = new();

    public ProductsViewModelMappingTests()
    {
        _userMock.Setup(u => u.TenantId).Returns(TenantId);
        // GetCategoriesQuery mock — ProductsVM.LoadAsync category lookup için gerekli
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetCategoriesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CategoryListDto>().AsReadOnly() as IReadOnlyList<CategoryListDto>);
    }

    private ProductsAvaloniaViewModel CreateVM() => new(_mediatorMock.Object, _userMock.Object, _toastMock.Object);

    private void SetupMediatorWith(params ProductDto[] items)
    {
        var result = PagedResult<ProductDto>.Create(items.ToList(), items.Length, 1, 5000);
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetProductsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private static ProductDto MakeDto(string sku, string name, decimal price, int stock,
        string? barcode = null, string? brand = null, string? category = null, bool active = true)
        => new()
        {
            Id = Guid.NewGuid(), SKU = sku, Name = name, SalePrice = price, Stock = stock,
            Barcode = barcode, Brand = brand, CategoryName = category, IsActive = active,
            ListPrice = price + 50m, MinimumStock = 5, StockStatus = stock == 0 ? "OutOfStock" : "InStock"
        };

    // ══════════════════════════════════════
    // LoadAsync → ObservableCollection
    // ══════════════════════════════════════

    [Fact]
    public async Task LoadAsync_ShouldPopulateProductsCollection()
    {
        SetupMediatorWith(
            MakeDto("SKU-001", "Urun A", 100m, 50),
            MakeDto("SKU-002", "Urun B", 200m, 30));

        var vm = CreateVM();
        await vm.LoadAsync();

        vm.Products.Should().HaveCount(2);
        vm.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task LoadAsync_ShouldMapSkuAndName()
    {
        SetupMediatorWith(MakeDto("TY-TSH-001", "Pamuklu T-Shirt", 149.90m, 200, "8691234567001"));

        var vm = CreateVM();
        await vm.LoadAsync();

        vm.Products[0].SKU.Should().Be("TY-TSH-001");
        vm.Products[0].Name.Should().Be("Pamuklu T-Shirt");
    }

    [Fact]
    public async Task LoadAsync_ShouldMapPriceAndSalePrice()
    {
        SetupMediatorWith(MakeDto("SKU-P", "Price Test", 149.90m, 10));

        var vm = CreateVM();
        await vm.LoadAsync();

        vm.Products[0].SalePrice.Should().Be(149.90m);
        vm.Products[0].Price.Should().Be(199.90m, "ListPrice = SalePrice + 50 from fixture");
    }

    [Fact]
    public async Task LoadAsync_ShouldMapStockAndMinimumStock()
    {
        SetupMediatorWith(MakeDto("SKU-S", "Stock Test", 100m, 42));

        var vm = CreateVM();
        await vm.LoadAsync();

        vm.Products[0].Stock.Should().Be(42);
        vm.Products[0].MinimumStock.Should().Be(5);
    }

    [Fact]
    public async Task LoadAsync_ShouldMapBarcodeAndBrand()
    {
        SetupMediatorWith(MakeDto("SKU-B", "Brand Test", 100m, 10, "8691111222333", "Nike"));

        var vm = CreateVM();
        await vm.LoadAsync();

        vm.Products[0].Barcode.Should().Be("8691111222333");
        vm.Products[0].Brand.Should().Be("Nike");
    }

    [Fact]
    public async Task LoadAsync_ShouldMapActiveStatus()
    {
        SetupMediatorWith(
            MakeDto("SKU-ACT", "Aktif", 100m, 10, active: true),
            MakeDto("SKU-PAS", "Pasif", 100m, 0, active: false));

        var vm = CreateVM();
        await vm.LoadAsync();

        vm.Products[0].Status.Should().Be("Aktif");
        vm.Products[1].Status.Should().Be("Pasif");
    }

    [Fact]
    public async Task LoadAsync_EmptyResult_ShouldClearCollection()
    {
        SetupMediatorWith(); // boş dizi

        var vm = CreateVM();
        await vm.LoadAsync();

        vm.Products.Should().BeEmpty();
        vm.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetPaginationInfo()
    {
        SetupMediatorWith(
            MakeDto("A", "A", 10m, 1), MakeDto("B", "B", 20m, 2), MakeDto("C", "C", 30m, 3));

        var vm = CreateVM();
        await vm.LoadAsync();

        vm.PaginationInfo.Should().Contain("3 urun");
        vm.CurrentPage.Should().Be(1);
    }
}
