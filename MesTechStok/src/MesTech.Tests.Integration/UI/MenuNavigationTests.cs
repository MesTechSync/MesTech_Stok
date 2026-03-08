using System;
using System.Threading;
using FluentAssertions;
using FlaUI.Core.AutomationElements;
using MesTech.Tests.Integration.UI._Shared;

namespace MesTech.Tests.Integration.UI;

/// <summary>
/// Emirname G2: Her menu linki icin UI otomasyon testi.
/// 17 sidebar butonunun her birini tiklar ve crash kontrolu yapar.
/// Kural: "Hicbir tiklama hata vermeyecek"
/// </summary>
[Collection("DesktopApp")]
[Trait("Category", "UIAutomation")]
public class MenuNavigationTests
{
    private readonly DesktopAppFixture _fixture;

    public MenuNavigationTests(DesktopAppFixture fixture)
    {
        _fixture = fixture;
    }

    public static TheoryData<string, string> SidebarButtons => new()
    {
        { "NavDashboard",           "Ana Sayfa" },
        { "NavProducts",            "Urunler" },
        { "NavStock",               "Stok Takibi" },
        { "NavOrders",              "Siparisler" },
        { "NavCustomers",           "Musteriler" },
        { "NavBarcode",             "Barkod Okuyucu" },
        { "NavReports",             "Raporlar" },
        { "NavExports",             "Disa Aktarma" },
        { "NavOpenCart",             "OpenCart" },
        { "NavSystemResources",     "Sistem Kaynaklari" },
        { "NavTrendyolConnection",  "Trendyol Baglanti" },
        { "NavPlatformOrders",      "Platform Siparisler" },
        { "NavInvoiceManagement",   "Fatura Yonetimi" },
        { "NavApiHealthDashboard",  "API Saglik" },
        { "NavPlatformSyncStatus",  "Sync Durumu" },
        { "NavLogs",                "Loglar" },
        { "NavSettings",            "Ayarlar" },
    };

    [Theory]
    [MemberData(nameof(SidebarButtons))]
    public void SidebarButton_ShouldNotCrash(string automationId, string displayName)
    {
        if (DesktopAppFixture.IsCI) return;

        _fixture.DismissAnyDialog();

        var button = _fixture.MainWindow.FindFirstDescendant(cf =>
            cf.ByAutomationId(automationId))?.AsButton();

        button.Should().NotBeNull(
            $"Sidebar button '{automationId}' ({displayName}) should exist in MainWindow");

        button!.Click();
        Thread.Sleep(2000);

        var errorDialog = _fixture.FindErrorDialog();
        errorDialog.Should().BeNull(
            $"Clicking '{displayName}' ({automationId}) should not produce an error dialog. " +
            $"Dialog found: {errorDialog?.Title}");

        _fixture.RefreshMainWindow();
        _fixture.MainWindow.Should().NotBeNull(
            "App should still be running after clicking " + displayName);
    }

    [Fact]
    public void AllSidebarButtons_ShouldExist()
    {
        if (DesktopAppFixture.IsCI) return;

        var expectedIds = new[]
        {
            "NavDashboard", "NavProducts", "NavStock", "NavOrders", "NavCustomers",
            "NavBarcode", "NavReports", "NavExports", "NavOpenCart", "NavSystemResources",
            "NavTrendyolConnection", "NavPlatformOrders", "NavInvoiceManagement",
            "NavApiHealthDashboard", "NavPlatformSyncStatus", "NavLogs", "NavSettings"
        };

        foreach (var id in expectedIds)
        {
            var button = _fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(id));
            button.Should().NotBeNull($"Sidebar should contain button with AutomationId='{id}'");
        }
    }
}
