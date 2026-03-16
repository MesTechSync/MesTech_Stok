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
    [AvaloniaFact]
    public void MainWindow_Renders_WithoutException()
    {
        // Arrange & Act
        var window = new MainWindow();

        // Assert — window should instantiate without errors
        window.Should().NotBeNull();
        window.Title.Should().Be("MesTech Stok — Avalonia PoC");
        window.Width.Should().Be(1280);
        window.Height.Should().Be(800);
    }

    [AvaloniaFact]
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

    [AvaloniaFact]
    public void MainWindow_ContentArea_HasContentControl()
    {
        // Arrange
        var window = new MainWindow();
        window.Show();

        // Act — find the ContentControl that hosts dynamic views
        var contentControl = window.GetVisualDescendants()
            .OfType<ContentControl>()
            .FirstOrDefault(c => c.ContentTemplate != null || c.DataTemplates.Count > 0);

        // Assert
        contentControl.Should().NotBeNull("MainWindow must have a ContentControl for view navigation");
    }

    [AvaloniaFact]
    public void MainWindow_WithViewModel_BindsDataContext()
    {
        // Arrange
        var mockFactory = new Mock<MesTech.Avalonia.Services.IViewModelFactory>();
        var vm = new MainWindowViewModel(mockFactory.Object);

        var window = new MainWindow
        {
            DataContext = vm
        };
        window.Show();

        // Assert
        window.DataContext.Should().BeSameAs(vm);
    }
}
