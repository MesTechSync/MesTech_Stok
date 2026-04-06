using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MesTech.Avalonia.Views;
using Moq;

namespace MesTechStok.Avalonia.Tests;

/// <summary>
/// Headless tests for MainWindow — verifies the shell renders correctly
/// and sidebar navigation binds to the ViewModel.
/// </summary>
[Trait("Category", "Avalonia")]
[Trait("Layer", "UI")]
public class MainWindowTests
{
    [AvaloniaFact(Skip = "MesTechTheme.axaml precompiled XAML not found in headless xUnit + FluentIcons font unavailable")]
    public void MainWindow_Renders_WithoutException()
    {
        // Arrange & Act
        var window = new MainWindow();

        // Assert — window should instantiate without errors
        window.Should().NotBeNull();
        window.Title.Should().Be("MesTech Entegratör");
        window.Width.Should().Be(1400);
        window.Height.Should().Be(900);
    }

    [AvaloniaFact(Skip = "FluentIcons font unavailable in headless xUnit — Show() throws InvalidOperationException")]
    public void MainWindow_HasSidebar_WithNavigationButtons()
    {
        // Arrange
        var window = new MainWindow();
        window.Show();

        // Act — find all buttons in the visual tree
        var buttons = window.GetVisualDescendants()
            .OfType<Button>()
            .ToList();

        // Assert — sidebar should have navigation buttons
        buttons.Should().NotBeEmpty("sidebar should contain navigation buttons");
    }

    [AvaloniaFact(Skip = "MesTechTheme.axaml precompiled XAML not found in headless xUnit")]
    public void MainWindow_ContentArea_HasContentControl()
    {
        // Arrange — instantiate without Show() to avoid FluentIcons font error
        var window = new MainWindow();

        // Assert — window should instantiate and have content area
        window.Should().NotBeNull();
        window.Content.Should().NotBeNull("MainWindow must have content defined in AXAML");
    }

    [AvaloniaFact(Skip = "MesTechTheme.axaml precompiled XAML not found in headless xUnit")]
    public void MainWindow_WithViewModel_BindsDataContext()
    {
        // Arrange
        var mockFactory = new Mock<MesTech.Avalonia.Services.IViewModelFactory>();
        var vm = new MainWindowViewModel(mockFactory.Object, Mock.Of<MesTech.Avalonia.Services.IFeatureGateService>());

        var window = new MainWindow
        {
            DataContext = vm
        };
        // Note: Show() skipped — FluentIcons font unavailable in headless xUnit (G10806 Katman 1.5)
        // DataContext binding works without rendering.

        // Assert
        window.DataContext.Should().BeSameAs(vm);
    }
}
