using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MediatR;
using Moq;

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
        _sut = new OrderListAvaloniaViewModel(_mediatorMock.Object);
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
