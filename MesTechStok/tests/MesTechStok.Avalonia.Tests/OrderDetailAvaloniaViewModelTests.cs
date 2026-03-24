using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

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
        _sut = new OrderDetailAvaloniaViewModel(_mediatorMock.Object);
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
    public async Task LoadAsync_ShouldPopulateOrderDetails()
    {
        await _sut.LoadAsync();

        _sut.CustomerName.Should().NotBeNullOrEmpty();
        _sut.Platform.Should().NotBeNullOrEmpty();
        _sut.OrderDate.Should().NotBeNullOrEmpty();
        _sut.CargoCompany.Should().NotBeNullOrEmpty();
        _sut.TrackingNumber.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoadAsync_ShouldPopulateOrderItems()
    {
        await _sut.LoadAsync();

        _sut.OrderItems.Should().NotBeEmpty();
        _sut.OrderItems.Should().HaveCountGreaterThanOrEqualTo(2);
        _sut.OrderItems.First().ProductName.Should().NotBeNullOrEmpty();
        _sut.OrderItems.First().Sku.Should().NotBeNullOrEmpty();
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
