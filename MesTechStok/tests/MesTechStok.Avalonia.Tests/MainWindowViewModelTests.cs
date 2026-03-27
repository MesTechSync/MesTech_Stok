using FluentAssertions;
using MesTech.Avalonia.Services;
using MesTech.Avalonia.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Moq;

namespace MesTechStok.Avalonia.Tests;

[Trait("Category", "Unit")]
[Trait("Layer", "ViewModel")]
public class MainWindowViewModelTests
{
    private readonly Mock<IViewModelFactory> _factoryMock;
    private readonly MainWindowViewModel _sut;

    public MainWindowViewModelTests()
    {
        _factoryMock = new Mock<IViewModelFactory>();
        _sut = new MainWindowViewModel(_factoryMock.Object, Mock.Of<MesTech.Avalonia.Services.IFeatureGateService>());
    }

    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        _sut.CurrentView.Should().BeNull();
        _sut.CurrentViewTitle.Should().Be("Dashboard");
    }

    [Fact]
    public void NavigateToCommand_ShouldSetCurrentView()
    {
        var mockVm = new Mock<ObservableObject>();
        _factoryMock.Setup(f => f.Create("Products")).Returns(mockVm.Object);

        _sut.NavigateToCommand.Execute("Products");

        _sut.CurrentView.Should().Be(mockVm.Object);
        _sut.CurrentViewTitle.Should().Be("Urunler");
    }

    [Fact]
    public void NavigateToCommand_Dashboard_ShouldSetTitle()
    {
        var mockVm = new Mock<ObservableObject>();
        _factoryMock.Setup(f => f.Create("Dashboard")).Returns(mockVm.Object);

        _sut.NavigateToCommand.Execute("Dashboard");

        _sut.CurrentViewTitle.Should().Be("Kontrol Paneli");
    }

    [Fact]
    public void NavigateToCommand_Orders_ShouldSetTitle()
    {
        var mockVm = new Mock<ObservableObject>();
        _factoryMock.Setup(f => f.Create("Orders")).Returns(mockVm.Object);

        _sut.NavigateToCommand.Execute("Orders");

        _sut.CurrentViewTitle.Should().Be("Siparis Yonetimi");
    }

    [Fact]
    public void NavigateToCommand_NullFactory_ShouldNotThrow()
    {
        _factoryMock.Setup(f => f.Create("NonExistent")).Returns((ObservableObject?)null);

        var act = () => _sut.NavigateToCommand.Execute("NonExistent");

        act.Should().NotThrow();
    }

    [Fact]
    public void NavigateToCommand_ShouldRaisePropertyChanged()
    {
        var mockVm = new Mock<ObservableObject>();
        _factoryMock.Setup(f => f.Create("Stock")).Returns(mockVm.Object);

        var changedProperties = new List<string>();
        _sut.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        _sut.NavigateToCommand.Execute("Stock");

        changedProperties.Should().Contain("CurrentView");
        changedProperties.Should().Contain("CurrentViewTitle");
    }
}
