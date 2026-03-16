using FluentAssertions;
using MesTech.Tests.E2E.Base;

namespace MesTech.Tests.E2E.Tests;

/// <summary>
/// Panel B E2E tests — HTML Panel frontend (shell + panel pages).
/// Validates dashboard rendering, product list with filters,
/// order management table, settings account form, and sidebar navigation.
///
/// Target: mestech-shell.html served at BlazorBaseUrl (default http://localhost:5200)
/// Shell uses hash-based routing: #dashboard, #products, #unified-orders, etc.
/// </summary>
[TestFixture]
[Category("E2E")]
[Category("PanelB")]
public class PanelBTests : BlazorTestBase
{
    /// <summary>
    /// Helper: navigate to the shell and go to a hash route.
    /// Shell uses MesTechRouter with hash-based navigation.
    /// </summary>
    private async Task NavigateToShellPageAsync(string hashRoute)
    {
        await Page.GotoAsync($"{BlazorBaseUrl}/#{ hashRoute}");
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(800);
    }

    // ───────────────────────────────────────────────────────────
    // TEST 1: Shell dashboard loads with header and sidebar
    // ───────────────────────────────────────────────────────────
    [Test]
    public async Task Shell_DashboardLoads_HeaderAndSidebarVisible()
    {
        await NavigateToShellPageAsync("dashboard");

        // Shell header should be present
        var shellHeader = Page.Locator("#shell-header");
        await shellHeader.WaitForAsync(new() { Timeout = 8000 });
        (await shellHeader.IsVisibleAsync()).Should().BeTrue(
            "Shell header (#shell-header) gorunur olmali");

        // Sidebar should be present with navigation items
        var sidebar = Page.Locator("#sidebar");
        (await sidebar.IsVisibleAsync()).Should().BeTrue(
            "Sidebar (#sidebar) gorunur olmali");

        // Dashboard tab in inner header should be active
        var dashboardTab = Page.Locator(".inner-tab[data-page='dashboard']");
        (await dashboardTab.CountAsync()).Should().BeGreaterThan(0,
            "Dashboard inner-tab mevcut olmali");

        // Content area should exist
        var contentArea = Page.Locator("#content-area");
        (await contentArea.CountAsync()).Should().BeGreaterThan(0,
            "Content area (#content-area) DOM'da olmali");

        // Logo should display MesTech branding
        var logoText = Page.Locator(".h-logo-text");
        var logoContent = await logoText.TextContentAsync();
        logoContent.Should().Contain("MesTech",
            "Logo metni 'MesTech' icermeli");
    }

    // ───────────────────────────────────────────────────────────
    // TEST 2: Products page loads table and KPI cards
    // ───────────────────────────────────────────────────────────
    [Test]
    public async Task Products_ListPage_ShowsTableAndKpiCards()
    {
        await NavigateToShellPageAsync("products");

        // Wait for content to render in the shell content area
        var contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // Page header should show product title
        var pageTitle = Page.Locator("h1");
        await pageTitle.First.WaitForAsync(new() { Timeout = 5000 });

        // KPI container should be rendered with stat cards
        var kpiContainer = Page.Locator("#kpi-container");
        (await kpiContainer.CountAsync()).Should().BeGreaterThan(0,
            "KPI container (#kpi-container) mevcut olmali");

        // Product table container should exist
        var tableContainer = Page.Locator("#table-container");
        (await tableContainer.CountAsync()).Should().BeGreaterThan(0,
            "Urun tablosu container'i (#table-container) mevcut olmali");

        // Filter container should be present for search/filter
        var filterContainer = Page.Locator("#filter-container");
        (await filterContainer.CountAsync()).Should().BeGreaterThan(0,
            "Filtre alani (#filter-container) mevcut olmali");

        // Export button should exist
        var exportBtn = Page.Locator("#btn-export");
        (await exportBtn.CountAsync()).Should().BeGreaterThan(0,
            "CSV Indir butonu (#btn-export) mevcut olmali");
    }

    // ───────────────────────────────────────────────────────────
    // TEST 3: Orders management page with filter and status
    // ───────────────────────────────────────────────────────────
    [Test]
    public async Task Orders_ManagementPage_ShowsTableWithStatusFilter()
    {
        await NavigateToShellPageAsync("unified-orders");

        var contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // KPI container should render order statistics
        var kpiContainer = Page.Locator("#kpi-container");
        (await kpiContainer.CountAsync()).Should().BeGreaterThan(0,
            "Siparis KPI container'i mevcut olmali");

        // Orders table should be present
        var ordersTable = Page.Locator("#orders-table");
        (await ordersTable.CountAsync()).Should().BeGreaterThan(0,
            "Siparis tablosu (#orders-table) mevcut olmali");

        // Filter container for search and platform/status filtering
        var filterContainer = Page.Locator("#filter-container");
        (await filterContainer.CountAsync()).Should().BeGreaterThan(0,
            "Siparis filtre alani mevcut olmali");

        // Bulk ship button should exist in page actions
        var bulkShipBtn = Page.Locator("#btn-bulk-ship");
        (await bulkShipBtn.CountAsync()).Should().BeGreaterThan(0,
            "Toplu Kargola butonu (#btn-bulk-ship) mevcut olmali");

        // Hidden stat counters should be in DOM
        var totalCount = Page.Locator("#totalOrdersCount");
        (await totalCount.CountAsync()).Should().BeGreaterThan(0,
            "Toplam siparis sayaci (#totalOrdersCount) DOM'da olmali");
    }

    // ───────────────────────────────────────────────────────────
    // TEST 4: Settings account page renders form and toggles
    // ───────────────────────────────────────────────────────────
    [Test]
    public async Task Settings_AccountPage_RendersProfileFormAndToggles()
    {
        await NavigateToShellPageAsync("settings-account");

        var contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // Page title should indicate account settings
        var pageTitle = Page.Locator("h1");
        await pageTitle.First.WaitForAsync(new() { Timeout = 5000 });

        // Tabs container for profile/security/notification tabs
        var tabsContainer = Page.Locator("#tabs-container");
        (await tabsContainer.CountAsync()).Should().BeGreaterThan(0,
            "Tabs container (#tabs-container) mevcut olmali");

        // Profile template should exist in DOM
        var profileTemplate = Page.Locator("#tmpl-profile");
        (await profileTemplate.CountAsync()).Should().BeGreaterThan(0,
            "Profil sablonu (#tmpl-profile) DOM'da olmali");

        // Security template should exist
        var securityTemplate = Page.Locator("#tmpl-security");
        (await securityTemplate.CountAsync()).Should().BeGreaterThan(0,
            "Guvenlik sablonu (#tmpl-security) DOM'da olmali");

        // Notification template should exist
        var notifTemplate = Page.Locator("#tmpl-notifications");
        (await notifTemplate.CountAsync()).Should().BeGreaterThan(0,
            "Bildirim sablonu (#tmpl-notifications) DOM'da olmali");
    }

    // ───────────────────────────────────────────────────────────
    // TEST 5: Sidebar navigation between pages works correctly
    // ───────────────────────────────────────────────────────────
    [Test]
    public async Task Sidebar_NavigateBetweenPages_HashChanges()
    {
        await NavigateToShellPageAsync("dashboard");

        // Click on Products sidebar item to navigate
        var productsLink = Page.Locator(".s-item[data-page='products']");
        (await productsLink.CountAsync()).Should().BeGreaterThan(0,
            "Urunler sidebar link'i mevcut olmali");
        await productsLink.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        // Hash should update to #products
        Page.Url.Should().Contain("#products",
            "Urunler'e tiklaninca URL hash'i #products olmali");

        // Navigate to orders via sidebar
        // First expand the Siparisler submenu if collapsed
        var ordersParent = Page.Locator(".s-item[data-has-sub='true']").Filter(
            new() { HasText = "Sipari" });
        if (await ordersParent.CountAsync() > 0)
        {
            await ordersParent.ClickAsync();
            await Page.WaitForTimeoutAsync(300);
        }

        var unifiedOrdersLink = Page.Locator(".s-item[data-page='unified-orders']");
        if (await unifiedOrdersLink.CountAsync() > 0)
        {
            await unifiedOrdersLink.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);

            Page.Url.Should().Contain("#unified-orders",
                "Tum Siparisler'e tiklaninca URL hash'i #unified-orders olmali");
        }

        // Navigate back to dashboard via inner tab
        var dashboardTab = Page.Locator(".inner-tab[data-page='dashboard']");
        await dashboardTab.ClickAsync();
        await Page.WaitForTimeoutAsync(1000);

        Page.Url.Should().Contain("#dashboard",
            "Dashboard tab'ina tiklaninca URL hash'i #dashboard olmali");
    }
}
