using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using FluentAssertions;
using MesTech.Avalonia.ViewModels;
using MesTech.Avalonia.Views;
using MesTech.Domain.Interfaces;
using Moq;

namespace MesTechStok.Avalonia.Tests;

/// <summary>
/// Headless tests for DashboardAvaloniaView — verifies the KPI card layout
/// renders correctly and bindings work in headless mode.
/// </summary>
[Trait("Category", "Avalonia")]
[Trait("Layer", "UI")]
public class DashboardViewTests
{
    [AvaloniaFact]
    public void DashboardView_Renders_WithoutException()
    {
        // Arrange & Act
        var view = new DashboardAvaloniaView();

        // Assert
        view.Should().NotBeNull();
    }

    [AvaloniaFact]
    public void DashboardView_WithViewModel_ShowsKpiCards()
    {
        // Arrange
        var mockMediator = new Mock<MediatR.IMediator>();
        var mockTenant = new Mock<ITenantProvider>();
        var vm = new DashboardAvaloniaViewModel(mockMediator.Object, mockTenant.Object);
        var view = new DashboardAvaloniaView
        {
            DataContext = vm
        };

        var window = new Window { Content = view };
        window.Show();

        // Act — find TextBlocks in the visual tree
        var textBlocks = view.GetVisualDescendants()
            .OfType<TextBlock>()
            .ToList();

        // Assert — dashboard should have KPI label text blocks
        textBlocks.Should().NotBeEmpty("dashboard view should render KPI labels");
    }

    [AvaloniaFact]
    public void DashboardView_HasRefreshButton()
    {
        // Arrange
        var mockMediator = new Mock<MediatR.IMediator>();
        var mockTenant = new Mock<ITenantProvider>();
        var vm = new DashboardAvaloniaViewModel(mockMediator.Object, mockTenant.Object);
        var view = new DashboardAvaloniaView
        {
            DataContext = vm
        };

        var window = new Window { Content = view };
        window.Show();

        // Act — find the Refresh button
        var refreshButton = view.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(b => b.Content?.ToString() == "Yenile");

        // Assert
        refreshButton.Should().NotBeNull("dashboard should have a Yenile (Refresh) button");
    }

    [Fact]
    [Trait("Category", "Avalonia")]
    public async Task DashboardViewModel_LoadAsync_PopulatesKpiData()
    {
        // Arrange
        var mockMediator = new Mock<MediatR.IMediator>();
        var mockTenant = new Mock<ITenantProvider>();
        var vm = new DashboardAvaloniaViewModel(mockMediator.Object, mockTenant.Object);

        // Assert initial state
        vm.TotalProducts.Should().Be("0");
        vm.IsLoading.Should().BeFalse();

        // Act
        await vm.LoadAsync();

        // Assert
        vm.TotalProducts.Should().NotBe("0");
        vm.TodayOrderCount.Should().NotBe("0");
        vm.TodayRevenue.Should().NotBe("0 TL");
        vm.CriticalStockCount.Should().NotBe("0");
        vm.IsLoading.Should().BeFalse();
        vm.LastUpdated.Should().NotBe("--:--");
    }
}
