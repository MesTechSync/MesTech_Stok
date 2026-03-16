using FluentAssertions;
using MesTech.Tests.E2E.Base;

namespace MesTech.Tests.E2E.Tests;

/// <summary>
/// Panel B Responsive E2E tests — 4 breakpoint validation.
/// Verifies the HTML Panel shell and pages render correctly at:
///   320px (mobile), 768px (tablet), 1024px (laptop), 1920px (desktop).
///
/// Checks sidebar visibility/collapse, hamburger menu, KPI grid layout,
/// table scroll behavior, and navigation at each breakpoint.
///
/// Target: mestech-shell.html served at BlazorBaseUrl (default http://localhost:5200)
/// CSS breakpoints defined in mestech-shell.css (1024 / 768 / 480)
/// and mds-responsive.css (1024 / 768 / 480).
/// </summary>
[TestFixture]
[Category("E2E")]
[Category("Responsive")]
public class ResponsiveTests : BlazorTestBase
{
    // ───────────────────────────────────────────────────────────
    // Breakpoint definitions: (Width, Height, Name)
    // ───────────────────────────────────────────────────────────
    private static readonly (int Width, int Height, string Name)[] Breakpoints =
    [
        (320, 568, "mobile"),
        (768, 1024, "tablet"),
        (1024, 768, "laptop"),
        (1920, 1080, "desktop")
    ];

    /// <summary>
    /// Helper: set viewport and navigate to a hash route in the shell.
    /// </summary>
    private async Task SetViewportAndNavigateAsync(int width, int height, string hashRoute)
    {
        await Page.SetViewportSizeAsync(width, height);
        await Page.GotoAsync($"{BlazorBaseUrl}/#{hashRoute}");
        await Page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(800);
    }

    // ═══════════════════════════════════════════════════════════
    // TEST 1: Shell loads correctly at each breakpoint
    // ═══════════════════════════════════════════════════════════

    [Test]
    [TestCaseSource(nameof(Breakpoints))]
    public async Task Shell_LoadsCorrectly_AtBreakpoint((int Width, int Height, string Name) bp)
    {
        await SetViewportAndNavigateAsync(bp.Width, bp.Height, "dashboard");

        // Shell header should always be visible
        var shellHeader = Page.Locator("#shell-header");
        await shellHeader.WaitForAsync(new() { Timeout = 8000 });
        (await shellHeader.IsVisibleAsync()).Should().BeTrue(
            $"Shell header (#shell-header) {bp.Name} ({bp.Width}px) breakpoint'inde gorunur olmali");

        // Content area should exist in DOM
        var contentArea = Page.Locator("#content-area");
        (await contentArea.CountAsync()).Should().BeGreaterThan(0,
            $"Content area (#content-area) {bp.Name} breakpoint'inde DOM'da olmali");

        // Inner header should be visible
        var innerHeader = Page.Locator("#inner-header");
        (await innerHeader.IsVisibleAsync()).Should().BeTrue(
            $"Inner header (#inner-header) {bp.Name} breakpoint'inde gorunur olmali");

        if (bp.Width <= 768)
        {
            // Mobile: hamburger button should be displayed
            var hamburger = Page.Locator("#hamburger");
            (await hamburger.IsVisibleAsync()).Should().BeTrue(
                $"Hamburger butonu (#hamburger) {bp.Name} ({bp.Width}px) breakpoint'inde gorunur olmali");

            // Mobile: sidebar should be off-screen (translateX(-100%)), not visible by default
            var sidebar = Page.Locator("#sidebar");
            var sidebarBox = await sidebar.BoundingBoxAsync();
            // Sidebar is translated off-screen, so its x should be negative or it should be hidden
            if (sidebarBox != null)
            {
                sidebarBox.X.Should().BeLessThan(0,
                    $"Sidebar {bp.Name} breakpoint'inde ekran disinda (translateX) olmali");
            }

            // Mobile: logo text should be hidden in header
            var logoText = Page.Locator(".h-logo-text");
            (await logoText.IsVisibleAsync()).Should().BeFalse(
                $"Logo text (.h-logo-text) {bp.Name} breakpoint'inde gizli olmali");

            // Mobile: profile name should be hidden in header
            var profileName = Page.Locator(".h-profile-name");
            (await profileName.IsVisibleAsync()).Should().BeFalse(
                $"Profil adi (.h-profile-name) {bp.Name} breakpoint'inde gizli olmali");
        }
        else if (bp.Width > 768 && bp.Width <= 1024)
        {
            // Tablet: sidebar auto-collapses to 60px
            var sidebar = Page.Locator("#sidebar");
            var sidebarBox = await sidebar.BoundingBoxAsync();
            sidebarBox.Should().NotBeNull(
                $"Sidebar {bp.Name} breakpoint'inde gorunur olmali");
            sidebarBox!.Width.Should().BeLessOrEqualTo(70,
                $"Sidebar {bp.Name} ({bp.Width}px) breakpoint'inde collapsed (60px) olmali");

            // Tablet: sidebar labels should be hidden
            var sidebarLabels = Page.Locator(".s-label");
            // At least one label should exist in DOM but all should be hidden
            if (await sidebarLabels.CountAsync() > 0)
            {
                (await sidebarLabels.First.IsVisibleAsync()).Should().BeFalse(
                    $"Sidebar label'lari {bp.Name} breakpoint'inde gizli olmali");
            }

            // Tablet: info strip should be hidden
            var infoStrip = Page.Locator(".h-info");
            (await infoStrip.IsVisibleAsync()).Should().BeFalse(
                $"Header info strip (.h-info) {bp.Name} breakpoint'inde gizli olmali");
        }
        else
        {
            // Desktop: sidebar should be fully expanded (240px)
            var sidebar = Page.Locator("#sidebar");
            var sidebarBox = await sidebar.BoundingBoxAsync();
            sidebarBox.Should().NotBeNull(
                $"Sidebar {bp.Name} breakpoint'inde gorunur olmali");
            sidebarBox!.Width.Should().BeGreaterOrEqualTo(200,
                $"Sidebar {bp.Name} ({bp.Width}px) breakpoint'inde expanded (240px) olmali");

            // Desktop: sidebar labels should be visible
            var firstLabel = Page.Locator(".s-label").First;
            (await firstLabel.IsVisibleAsync()).Should().BeTrue(
                $"Sidebar label (.s-label) {bp.Name} breakpoint'inde gorunur olmali");

            // Desktop: hamburger should be hidden
            var hamburger = Page.Locator("#hamburger");
            (await hamburger.IsVisibleAsync()).Should().BeFalse(
                $"Hamburger butonu {bp.Name} breakpoint'inde gizli olmali");

            // Desktop: logo text should be visible
            var logoText = Page.Locator(".h-logo-text");
            (await logoText.IsVisibleAsync()).Should().BeTrue(
                $"Logo text (.h-logo-text) {bp.Name} breakpoint'inde gorunur olmali");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // TEST 2: Dashboard renders correctly at each breakpoint
    // ═══════════════════════════════════════════════════════════

    [Test]
    [TestCaseSource(nameof(Breakpoints))]
    public async Task Dashboard_RendersCorrectly_AtBreakpoint((int Width, int Height, string Name) bp)
    {
        await SetViewportAndNavigateAsync(bp.Width, bp.Height, "dashboard");

        // Content area should be present
        var contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // KPI grid should render (either .kpi-grid from shell or .mds-kpi-grid from MDS)
        var kpiGrid = Page.Locator(".kpi-grid, .mds-kpi-grid");
        if (await kpiGrid.CountAsync() > 0)
        {
            (await kpiGrid.First.IsVisibleAsync()).Should().BeTrue(
                $"KPI grid {bp.Name} ({bp.Width}px) breakpoint'inde gorunur olmali");

            if (bp.Width <= 480)
            {
                // Small mobile: KPI cards should stack to 1 column (grid-template-columns: 1fr)
                var gridStyle = await kpiGrid.First.EvaluateAsync<string>(
                    "el => getComputedStyle(el).gridTemplateColumns");
                // Single column means only one column value (no spaces separating multiple column widths)
                gridStyle.Should().NotContain(" ",
                    $"KPI grid {bp.Name} ({bp.Width}px) breakpoint'inde tek sutun olmali");
            }
            else if (bp.Width <= 1024)
            {
                // Tablet/laptop: KPI cards should be 2 columns
                var gridStyle = await kpiGrid.First.EvaluateAsync<string>(
                    "el => getComputedStyle(el).gridTemplateColumns");
                var columnCount = gridStyle.Split(' ',
                    StringSplitOptions.RemoveEmptyEntries).Length;
                columnCount.Should().BeLessOrEqualTo(3,
                    $"KPI grid {bp.Name} ({bp.Width}px) breakpoint'inde en fazla 3 sutun olmali");
            }
            else
            {
                // Desktop: KPI cards should be 3+ columns
                var gridStyle = await kpiGrid.First.EvaluateAsync<string>(
                    "el => getComputedStyle(el).gridTemplateColumns");
                var columnCount = gridStyle.Split(' ',
                    StringSplitOptions.RemoveEmptyEntries).Length;
                columnCount.Should().BeGreaterOrEqualTo(3,
                    $"KPI grid {bp.Name} ({bp.Width}px) breakpoint'inde en az 3 sutun olmali");
            }
        }

        // Dashboard tab should exist in inner header
        var dashboardTab = Page.Locator(".inner-tab[data-page='dashboard']");
        (await dashboardTab.CountAsync()).Should().BeGreaterThan(0,
            $"Dashboard tab {bp.Name} breakpoint'inde mevcut olmali");

        // Content area should not be empty (page loaded some content)
        var contentHtml = await contentArea.InnerHTMLAsync();
        contentHtml.Should().NotBeNullOrWhiteSpace(
            $"Content area {bp.Name} breakpoint'inde icerik icermeli");
    }

    // ═══════════════════════════════════════════════════════════
    // TEST 3: Products table is responsive at each breakpoint
    // ═══════════════════════════════════════════════════════════

    [Test]
    [TestCaseSource(nameof(Breakpoints))]
    public async Task Products_TableResponsive_AtBreakpoint((int Width, int Height, string Name) bp)
    {
        await SetViewportAndNavigateAsync(bp.Width, bp.Height, "products");

        var contentArea = Page.Locator("#content-area");
        await contentArea.WaitForAsync(new() { Timeout = 8000 });

        // KPI container should be present on the products page
        var kpiContainer = Page.Locator("#kpi-container");
        (await kpiContainer.CountAsync()).Should().BeGreaterThan(0,
            $"KPI container (#kpi-container) {bp.Name} breakpoint'inde mevcut olmali");

        // Table container should be present
        var tableContainer = Page.Locator("#table-container");
        (await tableContainer.CountAsync()).Should().BeGreaterThan(0,
            $"Tablo container (#table-container) {bp.Name} breakpoint'inde mevcut olmali");

        // Filter container should be present
        var filterContainer = Page.Locator("#filter-container");
        (await filterContainer.CountAsync()).Should().BeGreaterThan(0,
            $"Filtre container (#filter-container) {bp.Name} breakpoint'inde mevcut olmali");

        if (bp.Width <= 768)
        {
            // Mobile/tablet: table wrapper should have horizontal scroll (overflow-x: auto)
            var tableWrap = Page.Locator(".mds-table-wrap");
            if (await tableWrap.CountAsync() > 0)
            {
                var overflowX = await tableWrap.First.EvaluateAsync<string>(
                    "el => getComputedStyle(el).overflowX");
                overflowX.Should().BeOneOf("auto", "scroll",
                    $"Tablo wrapper {bp.Name} ({bp.Width}px) breakpoint'inde yatay scroll olmali");
            }

            // On mobile, content area should use full width (left: 0 for shell-content)
            var shellContent = Page.Locator(".shell-content");
            var contentBox = await shellContent.BoundingBoxAsync();
            if (contentBox != null)
            {
                contentBox.X.Should().BeLessOrEqualTo(5,
                    $"Shell content {bp.Name} breakpoint'inde sol kenardan baslamali (left: 0)");
            }
        }
        else
        {
            // Desktop: table should render inline without forced horizontal scroll
            var table = Page.Locator(".mds-table, table");
            if (await table.CountAsync() > 0)
            {
                (await table.First.IsVisibleAsync()).Should().BeTrue(
                    $"Tablo {bp.Name} ({bp.Width}px) breakpoint'inde gorunur olmali");
            }
        }

        // Export button should be accessible at all breakpoints
        var exportBtn = Page.Locator("#btn-export");
        (await exportBtn.CountAsync()).Should().BeGreaterThan(0,
            $"CSV Indir butonu (#btn-export) {bp.Name} breakpoint'inde DOM'da olmali");
    }

    // ═══════════════════════════════════════════════════════════
    // TEST 4: Navigation works correctly at each breakpoint
    // ═══════════════════════════════════════════════════════════

    [Test]
    [TestCaseSource(nameof(Breakpoints))]
    public async Task Navigation_WorksCorrectly_AtBreakpoint((int Width, int Height, string Name) bp)
    {
        await SetViewportAndNavigateAsync(bp.Width, bp.Height, "dashboard");

        if (bp.Width <= 768)
        {
            // Mobile: hamburger menu should open sidebar
            var hamburger = Page.Locator("#hamburger");
            (await hamburger.IsVisibleAsync()).Should().BeTrue(
                $"Hamburger butonu {bp.Name} breakpoint'inde gorunur olmali");

            // Click hamburger to open mobile sidebar
            await hamburger.ClickAsync();
            await Page.WaitForTimeoutAsync(500);

            // Sidebar should now be visible (mobile-open class applied)
            var sidebar = Page.Locator("#sidebar");
            var hasMobileOpen = await sidebar.EvaluateAsync<bool>(
                "el => el.classList.contains('mobile-open')");
            hasMobileOpen.Should().BeTrue(
                $"Sidebar {bp.Name} breakpoint'inde hamburger tiklaninca mobile-open class'i almali");

            // Overlay should be visible
            var overlay = Page.Locator("#sidebar-overlay");
            (await overlay.IsVisibleAsync()).Should().BeTrue(
                $"Sidebar overlay {bp.Name} breakpoint'inde gorunur olmali");

            // Navigate via sidebar — click Products link
            var productsLink = Page.Locator(".s-item[data-page='products']");
            if (await productsLink.CountAsync() > 0)
            {
                await productsLink.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);

                Page.Url.Should().Contain("#products",
                    $"{bp.Name} breakpoint'inde sidebar'dan Urunler'e navigasyon calismali");
            }
        }
        else if (bp.Width > 768 && bp.Width <= 1024)
        {
            // Tablet: sidebar is auto-collapsed (60px icon-only), use inner tabs
            var dashboardTab = Page.Locator(".inner-tab[data-page='dashboard']");
            (await dashboardTab.IsVisibleAsync()).Should().BeTrue(
                $"Dashboard tab {bp.Name} breakpoint'inde gorunur olmali");

            // Navigate via inner tab to Products
            var productsTab = Page.Locator(".inner-tab[data-page='products']");
            if (await productsTab.CountAsync() > 0 && await productsTab.IsVisibleAsync())
            {
                await productsTab.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);

                Page.Url.Should().Contain("#products",
                    $"{bp.Name} breakpoint'inde inner tab'dan Urunler'e navigasyon calismali");
            }
        }
        else
        {
            // Desktop: full sidebar navigation
            var productsLink = Page.Locator(".s-item[data-page='products']");
            (await productsLink.CountAsync()).Should().BeGreaterThan(0,
                $"Urunler sidebar link'i {bp.Name} breakpoint'inde mevcut olmali");

            // Expand Stok Yonetimi submenu first if needed
            var stokParent = Page.Locator(".s-item[data-has-sub='true']").Filter(
                new() { HasText = "Stok" });
            if (await stokParent.CountAsync() > 0)
            {
                await stokParent.ClickAsync();
                await Page.WaitForTimeoutAsync(300);
            }

            // Click Products
            if (await productsLink.IsVisibleAsync())
            {
                await productsLink.ClickAsync();
                await Page.WaitForTimeoutAsync(1000);

                Page.Url.Should().Contain("#products",
                    $"{bp.Name} breakpoint'inde sidebar'dan Urunler'e navigasyon calismali");
            }

            // Navigate back to dashboard via inner tab
            var dashboardTab = Page.Locator(".inner-tab[data-page='dashboard']");
            await dashboardTab.ClickAsync();
            await Page.WaitForTimeoutAsync(1000);

            Page.Url.Should().Contain("#dashboard",
                $"{bp.Name} breakpoint'inde inner tab'dan Dashboard'a geri donus calismali");
        }
    }
}
