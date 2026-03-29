using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MesTech.Domain.Interfaces;
using MediatR;
using Moq;

namespace MesTechStok.Avalonia.Tests;

// ════════════════════════════════════════════════════════
// DEV5 TUR 2: G403 — 4 Marketplace VM unit tests
// Etsy, Shopify, WooCommerce, Zalando
// Pattern: Constructor defaults + LoadAsync 3-state + Error handling
// ════════════════════════════════════════════════════════

#region EtsyAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class EtsyAvaloniaViewModelTests
{
    private static EtsyAvaloniaViewModel CreateSut()
        => new(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>());

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsConnected.Should().BeFalse();
        sut.ProductCount.Should().Be(0);
        sut.OrderCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        var sut = CreateSut();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(EtsyAvaloniaViewModel.IsLoading))
                loadingStates.Add(sut.IsLoading);
        };

        await sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorFails_SetsErrorState()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<IRequest<It.IsAnyType>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Etsy API down"));

        var sut = new EtsyAvaloniaViewModel(mediator.Object, Mock.Of<ICurrentUserService>());
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().NotBeNullOrEmpty();
        sut.IsLoading.Should().BeFalse();
    }
}

#endregion

#region ShopifyAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ShopifyAvaloniaViewModelTests
{
    private static ShopifyAvaloniaViewModel CreateSut()
        => new(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>());

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsConnected.Should().BeFalse();
        sut.ProductCount.Should().Be(0);
        sut.OrderCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        var sut = CreateSut();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ShopifyAvaloniaViewModel.IsLoading))
                loadingStates.Add(sut.IsLoading);
        };

        await sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorFails_SetsErrorState()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<IRequest<It.IsAnyType>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Shopify timeout"));

        var sut = new ShopifyAvaloniaViewModel(mediator.Object, Mock.Of<ICurrentUserService>());
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().NotBeNullOrEmpty();
        sut.IsLoading.Should().BeFalse();
    }
}

#endregion

#region WooCommerceAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class WooCommerceAvaloniaViewModelTests
{
    private static WooCommerceAvaloniaViewModel CreateSut()
        => new(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>());

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsConnected.Should().BeFalse();
        sut.ProductCount.Should().Be(0);
        sut.OrderCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        var sut = CreateSut();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WooCommerceAvaloniaViewModel.IsLoading))
                loadingStates.Add(sut.IsLoading);
        };

        await sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorFails_SetsErrorState()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<IRequest<It.IsAnyType>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("WooCommerce error"));

        var sut = new WooCommerceAvaloniaViewModel(mediator.Object, Mock.Of<ICurrentUserService>());
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().NotBeNullOrEmpty();
        sut.IsLoading.Should().BeFalse();
    }
}

#endregion

#region ZalandoAvaloniaViewModel

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class ZalandoAvaloniaViewModelTests
{
    private static ZalandoAvaloniaViewModel CreateSut()
        => new(Mock.Of<IMediator>(), Mock.Of<ICurrentUserService>());

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var sut = CreateSut();

        sut.IsLoading.Should().BeFalse();
        sut.HasError.Should().BeFalse();
        sut.IsConnected.Should().BeFalse();
        sut.ProductCount.Should().Be(0);
        sut.OrderCount.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_ShouldSetLoadingStates()
    {
        var loadingStates = new List<bool>();
        var sut = CreateSut();
        sut.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ZalandoAvaloniaViewModel.IsLoading))
                loadingStates.Add(sut.IsLoading);
        };

        await sut.LoadAsync();

        loadingStates.Should().ContainInOrder(true, false);
        sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_WhenMediatorFails_SetsErrorState()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<IRequest<It.IsAnyType>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Zalando API error"));

        var sut = new ZalandoAvaloniaViewModel(mediator.Object, Mock.Of<ICurrentUserService>());
        await sut.LoadAsync();

        sut.HasError.Should().BeTrue();
        sut.ErrorMessage.Should().NotBeNullOrEmpty();
        sut.IsLoading.Should().BeFalse();
    }
}

#endregion
