using FluentAssertions;
using MediatR;
using MesTech.Avalonia.ViewModels;
using GetOrderListQuery = MesTech.Application.Features.Orders.Queries.GetOrderList.GetOrderListQuery;
using AppOrderListItemDto = MesTech.Application.Features.Orders.Queries.GetOrderList.OrderListItemDto;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class OrdersAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITenantProvider> _tenantMock = new();
    private readonly Mock<INavigationService> _navigationMock = new();

    private OrdersAvaloniaViewModel CreateSut()
    {
        _tenantMock.Setup(t => t.GetCurrentTenantId()).Returns(Guid.NewGuid());
        return new OrdersAvaloniaViewModel(_mediatorMock.Object, _tenantMock.Object, _navigationMock.Object);
    }

    private static IReadOnlyList<AppOrderListItemDto> CreateTestOrders() =>
    [
        new AppOrderListItemDto { OrderNumber = "SIP-001", CustomerName = "Ahmet Yilmaz", Status = "Yeni", TotalAmount = 500m, OrderDate = DateTime.Now, SourcePlatform = "Trendyol" },
        new AppOrderListItemDto { OrderNumber = "SIP-002", CustomerName = "Mehmet Kaya", Status = "Hazırlanıyor", TotalAmount = 750m, OrderDate = DateTime.Now, SourcePlatform = "Hepsiburada" },
        new AppOrderListItemDto { OrderNumber = "SIP-003", CustomerName = "Ayse Demir", Status = "Yeni", TotalAmount = 320m, OrderDate = DateTime.Now, SourcePlatform = "N11" },
        new AppOrderListItemDto { OrderNumber = "SIP-004", CustomerName = "Fatma Celik", Status = "Kargoda", TotalAmount = 1200m, OrderDate = DateTime.Now, SourcePlatform = "Trendyol" },
        new AppOrderListItemDto { OrderNumber = "SIP-005", CustomerName = "Ali Ozturk", Status = "Teslim Edildi", TotalAmount = 890m, OrderDate = DateTime.Now, SourcePlatform = "Amazon" },
    ];

    private void SetupMediatorWithOrders(IReadOnlyList<AppOrderListItemDto>? orders = null)
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetOrderListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders ?? CreateTestOrders());
    }

    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsEmpty.Should().BeFalse();
        sut.ErrorMessage.Should().BeEmpty();
        sut.SearchText.Should().BeEmpty();
        sut.SelectedStatus.Should().Be("Tümü");
        sut.TotalCount.Should().Be(0);
        sut.Orders.Should().BeEmpty();
        sut.Statuses.Should().HaveCountGreaterThan(0);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetIsLoadingDuringExecution()
    {
        var tcs = new TaskCompletionSource<IReadOnlyList<AppOrderListItemDto>>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetOrderListQuery>(), It.IsAny<CancellationToken>()))
            .Returns(tcs.Task);

        var sut = CreateSut();
        var loadTask = sut.LoadAsync();

        sut.IsLoading.Should().BeTrue();

        tcs.SetResult(CreateTestOrders());
        await loadTask;

        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateOrders()
    {
        SetupMediatorWithOrders();
        var sut = CreateSut();

        await sut.LoadAsync();

        sut.Orders.Should().NotBeEmpty();
        sut.TotalCount.Should().BeGreaterThan(0);
        sut.IsEmpty.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.Orders.First().OrderNo.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task LoadAsync_FilterByStatus_ShouldReduceResults()
    {
        SetupMediatorWithOrders();
        var sut = CreateSut();
        await sut.LoadAsync();
        var totalBefore = sut.TotalCount;

        sut.SelectedStatus = "Yeni";

        sut.TotalCount.Should().BeLessThan(totalBefore);
        sut.Orders.Should().OnlyContain(o => o.Status == "Yeni");
    }

    [Fact]
    public async Task LoadAsync_SearchText_ShouldFilterByCustomerOrOrderNo()
    {
        SetupMediatorWithOrders();
        var sut = CreateSut();
        await sut.LoadAsync();

        sut.SearchText = "Ahmet";

        sut.Orders.Should().HaveCount(1);
        sut.Orders.First().Customer.Should().Contain("Ahmet");
    }
}
