using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using MesTech.Tests.Integration.UI._Shared;

namespace MesTech.Tests.Integration.UI;

/// <summary>
/// Emirname G3: "Hicbir tiklama crash olmaz" — comprehensive sweep.
/// Navigates to each screen, finds all buttons, clicks each one.
/// Verifies zero error dialogs across the entire application.
/// </summary>
[Collection("DesktopApp")]
[Trait("Category", "UIAutomation")]
public class NoCrashSweepTests
{
    private readonly DesktopAppFixture _fixture;
    private readonly List<string> _errors = new();

    private static readonly string[] SidebarIds =
    {
        "NavDashboard", "NavProducts", "NavStock", "NavOrders", "NavCustomers",
        "NavBarcode", "NavReports", "NavExports", "NavOpenCart", "NavSystemResources",
        "NavTrendyolConnection", "NavPlatformOrders", "NavInvoiceManagement",
        "NavApiHealthDashboard", "NavPlatformSyncStatus", "NavLogs", "NavSettings"
    };

    public NoCrashSweepTests(DesktopAppFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact(Skip = "Requires physical display and compiled WPF EXE — run locally only")]
    public void FullSweep_AllScreens_ZeroCrash()
    {
        foreach (var navId in SidebarIds)
        {
            _fixture.DismissAnyDialog();

            var navButton = _fixture.MainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId(navId))?.AsButton();
            if (navButton == null) continue;

            navButton.Click();
            Thread.Sleep(1500);

            CheckForCrash($"Navigation to {navId}");

            // Find content-area buttons (exclude sidebar nav buttons)
            var allButtons = _fixture.MainWindow.FindAllDescendants(cf =>
                cf.ByControlType(ControlType.Button));

            var contentButtons = allButtons
                .Where(b => !SidebarIds.Contains(b.AutomationId ?? ""))
                .Where(b => b.AutomationId != "NavReturnToWelcome")
                .Where(b => b.AutomationId != "NavStockPlacement")
                .Where(b => b.IsEnabled && b.IsOffscreen == false)
                .Take(20) // Limit per screen to avoid combinatorial explosion
                .ToList();

            foreach (var btn in contentButtons)
            {
                try
                {
                    btn.AsButton().Click();
                    Thread.Sleep(500);
                    CheckForCrash($"Button '{btn.Name ?? btn.AutomationId}' on screen {navId}");
                    _fixture.DismissAnyDialog();
                }
                catch (Exception ex) when (ex is not Xunit.Sdk.XunitException)
                {
                    // FlaUI element may have become stale — skip
                }
            }

            // Return to dashboard between screens
            var dashboard = _fixture.MainWindow.FindFirstDescendant(cf =>
                cf.ByAutomationId("NavDashboard"))?.AsButton();
            dashboard?.Click();
            Thread.Sleep(500);
        }

        _errors.Should().BeEmpty(
            $"Zero-crash sweep found {_errors.Count} error(s):\n" +
            string.Join("\n", _errors));
    }

    private void CheckForCrash(string context)
    {
        var dialog = _fixture.FindErrorDialog();
        if (dialog != null)
        {
            _errors.Add($"[CRASH] {context} -- Dialog: {dialog.Title}");
            _fixture.DismissAnyDialog();
        }
    }
}
