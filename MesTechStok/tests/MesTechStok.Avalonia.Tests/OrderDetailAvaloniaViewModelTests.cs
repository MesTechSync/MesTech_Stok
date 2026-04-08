using FluentAssertions;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Application.Features.Orders.Queries.GetOrderDetail;
using MesTech.Avalonia.ViewModels;
using MesTech.Avalonia.Services;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;
using AppOrderListItemDto = MesTech.Application.Features.Orders.Queries.GetOrderList.OrderListItemDto;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class OrderDetailAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly OrderDetailAvaloniaViewModel _sut;

    public OrderDetailAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetOrderListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AppOrderListItemDto>().AsReadOnly());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetOrderDetailQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderDetailDto?)null);
        _sut = new OrderDetailAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>(), Mock.Of<INavigationService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.OrderNumber.Should().NotBeNullOrEmpty();
        _sut.OrderItems.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_WhenNoOrders_ShouldSetEmptyState()
    {
        await _sut.LoadAsync();

        _sut.IsEmpty.Should().BeTrue();
        _sut.HasError.Should().BeFalse();
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenNoOrders_OrderItemsShouldBeEmpty()
    {
        await _sut.LoadAsync();

        _sut.OrderItems.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OrderDetailAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        await _sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        _sut.HasError.Should().BeFalse();
    }
}
