using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using MesTech.Tests.Integration.UI._Shared;

namespace MesTech.Tests.Integration.UI;

/// <summary>
/// Emirname G2: Dashboard buton tiklama testleri.
/// DashboardView icindeki hizli erisim butonlari.
/// </summary>
[Collection("DesktopApp")]
[Trait("Category", "UIAutomation")]
public class DashboardNavigationTests
{
    private readonly DesktopAppFixture _fixture;

    public DashboardNavigationTests(DesktopAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Dashboard_ShouldLoadWithoutError()
    {
        if (DesktopAppFixture.IsCI) return;

        _fixture.DismissAnyDialog();
        var navDashboard = _fixture.MainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NavDashboard"))?.AsButton();
        navDashboard?.Click();
        Thread.Sleep(2000);

        var errorDialog = _fixture.FindErrorDialog();
        errorDialog.Should().BeNull("Dashboard should load without error dialog");
    }

    [Theory]
    [InlineData("Urun")]
    [InlineData("Barkod")]
    [InlineData("Rapor")]
    [InlineData("Stok")]
    public void DashboardQuickLink_ShouldNotCrash(string linkTextContains)
    {
        if (DesktopAppFixture.IsCI) return;

        _fixture.DismissAnyDialog();

        var navDashboard = _fixture.MainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId("NavDashboard"))?.AsButton();
        navDashboard?.Click();
        Thread.Sleep(1500);

        var buttons = _fixture.MainWindow.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.Button));

        var quickLink = buttons
            .FirstOrDefault(b => b.Name?.Contains(linkTextContains, StringComparison.OrdinalIgnoreCase) == true);

        if (quickLink == null) return; // Quick link may not exist in all dashboard variants

        quickLink.AsButton().Click();
        Thread.Sleep(2000);

        var errorDialog = _fixture.FindErrorDialog();
        errorDialog.Should().BeNull(
            $"Dashboard quick link containing '{linkTextContains}' should not crash");
    }
}
