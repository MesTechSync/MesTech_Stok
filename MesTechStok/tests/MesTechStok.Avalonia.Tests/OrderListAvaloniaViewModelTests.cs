using FluentAssertions;
using MesTech.Application.Features.Orders.Queries.GetOrderList;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;
using OrderQueryDto = MesTech.Application.Features.Orders.Queries.GetOrderList.OrderListItemDto;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class OrderListAvaloniaViewModelTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly OrderListAvaloniaViewModel _sut;

    public OrderListAvaloniaViewModelTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetOrderListQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<OrderQueryDto>)new List<OrderQueryDto>
            {
                new() { Id = Guid.NewGuid(), OrderNumber = "SIP-001", CustomerName = "Ali Yilmaz", Status = "Yeni", SourcePlatform = "Trendyol", TotalAmount = 450m, OrderDate = DateTime.Now.AddDays(-1) },
                new() { Id = Guid.NewGuid(), OrderNumber = "SIP-002", CustomerName = "Veli Demir", Status = "Hazirlaniyor", SourcePlatform = "Hepsiburada", TotalAmount = 890m, OrderDate = DateTime.Now.AddDays(-2) },
                new() { Id = Guid.NewGuid(), OrderNumber = "SIP-003", CustomerName = "Ayse Kaya", Status = "Kargoda", SourcePlatform = "Trendyol", TotalAmount = 320m, OrderDate = DateTime.Now.AddDays(-3) },
                new() { Id = Guid.NewGuid(), OrderNumber = "SIP-004", CustomerName = "Mehmet Can", Status = "Teslim Edildi", SourcePlatform = "N11", TotalAmount = 1200m, OrderDate = DateTime.Now.AddDays(-4) },
                new() { Id = Guid.NewGuid(), OrderNumber = "SIP-005", CustomerName = "Zeynep Ak", Status = "Yeni", SourcePlatform = "Hepsiburada", TotalAmount = 650m, OrderDate = DateTime.Now.AddDays(-5) }
            });
        _sut = new OrderListAvaloniaViewModel(_mediatorMock.Object, Mock.Of<ICurrentUserService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
        _sut.ErrorMessage.Should().BeEmpty();
        _sut.IsEmpty.Should().BeFalse();
        _sut.SearchText.Should().BeEmpty();
        _sut.TotalCount.Should().Be(0);
        _sut.Orders.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateOrders()
    {
        await _sut.LoadAsync();

        _sut.Orders.Should().NotBeEmpty();
        _sut.TotalCount.Should().BeGreaterThan(0);
        _sut.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ShouldContainExpectedPlatforms()
    {
        await _sut.LoadAsync();

        _sut.Orders.Select(o => o.Platform).Should().Contain("Trendyol");
        _sut.Orders.Select(o => o.Platform).Should().Contain("Hepsiburada");
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(OrderListAvaloniaViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        await _sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        _sut.IsLoading.Should().BeFalse();
        _sut.HasError.Should().BeFalse();
    }
}
