using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MesTech.Tests.Unit.Shell;

/// <summary>
/// DEV 5 — Dalga 7.6 Task 5.01: Shell UI smoke tests.
/// File-scanning meta-tests that verify shell HTML structure,
/// sidebar navigation tree, wallpaper support, glass morphism CSS,
/// responsive breakpoints (768px), and router page registry.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Shell")]
public class ShellUiSmokeTests
{
    private readonly string _repoRoot;
    private readonly string _shellDir;

    public ShellUiSmokeTests()
    {
        _repoRoot = FindRepoRoot();
        _shellDir = Path.Combine(_repoRoot, "frontend", "shell");
    }

    #region 1. Shell HTML Structure

    [Fact]
    public void Shell_Html_FileExists()
    {
        File.Exists(Path.Combine(_shellDir, "mestech-shell.html"))
            .Should().BeTrue("mestech-shell.html is the shell entry point");
    }

    [Fact]
    public void Shell_Html_HasDoctype()
    {
        var html = ReadShellFile("mestech-shell.html");
        html.Should().StartWith("<!DOCTYPE html>",
            "shell HTML must start with DOCTYPE for standards mode");
    }

    [Fact]
    public void Shell_Html_HasLangTr()
    {
        var html = ReadShellFile("mestech-shell.html");
        html.Should().Contain("lang=\"tr\"",
            "shell targets Turkish locale");
    }

    [Fact]
    public void Shell_Html_HasViewportMeta()
    {
        var html = ReadShellFile("mestech-shell.html");
        html.Should().Contain("viewport",
            "viewport meta is required for responsive design");
    }

    [Fact]
    public void Shell_Html_HasFourLayerLayout()
    {
        var html = ReadShellFile("mestech-shell.html");

        // Layer 1: Top Header
        html.Should().Contain("shell-top-header",
            "Layer 1: top header must exist");

        // Layer 2: Inner Header
        html.Should().Contain("shell-inner-header",
            "Layer 2: inner header (platform tabs) must exist");

        // Layer 3: Sidebar
        html.Should().Contain("shell-sidebar",
            "Layer 3: sidebar navigation must exist");

        // Layer 4: Content area
        html.Should().Contain("shell-content",
            "Layer 4: content area must exist");
    }

    [Fact]
    public void Shell_Html_HasWallpaperLayer()
    {
        var html = ReadShellFile("mestech-shell.html");
        html.Should().Contain("shell-wallpaper",
            "wallpaper background layer is required for glass morphism design");
    }

    [Fact]
    public void Shell_Html_HasContentIframe()
    {
        var html = ReadShellFile("mestech-shell.html");
        html.Should().Contain("content-frame",
            "content iframe is required for page loading");
    }

    [Fact]
    public void Shell_Html_LoadsAllScripts()
    {
        var html = ReadShellFile("mestech-shell.html");

        html.Should().Contain("mestech-sidebar.js",
            "sidebar script must be loaded");
        html.Should().Contain("mestech-router.js",
            "router script must be loaded");
    }

    [Fact]
    public void Shell_Html_HasGlobalSearchInput()
    {
        var html = ReadShellFile("mestech-shell.html");
        html.Should().Contain("global-search",
            "global search input is required in the header");
    }

    [Fact]
    public void Shell_Html_HasSidebarToggleButton()
    {
        var html = ReadShellFile("mestech-shell.html");
        html.Should().Contain("sidebar-toggle",
            "sidebar toggle button is required for collapse/expand");
    }

    [Fact]
    public void Shell_Html_SidebarStartsCollapsed()
    {
        var html = ReadShellFile("mestech-shell.html");
        html.Should().Contain("data-state=\"collapsed\"",
            "sidebar should start in collapsed state by default");
    }

    #endregion

    #region 2. Inner Header — Platform Tabs

    [Theory]
    [InlineData("dashboard", "Dashboard")]
    [InlineData("trendyol-dashboard", "Trendyol")]
    [InlineData("hb-dashboard", "HB")]
    [InlineData("n11-dashboard", "N11")]
    [InlineData("cs-dashboard", "CS")]
    [InlineData("pz-dashboard", "Pazarama")]
    [InlineData("amazon-dashboard", "Amazon")]
    [InlineData("bitrix24-dashboard", "Bitrix24")]
    public void Shell_InnerHeader_HasPlatformTab(string dataPage, string label)
    {
        var html = ReadShellFile("mestech-shell.html");
        html.Should().Contain($"data-page=\"{dataPage}\"",
            $"inner header must have tab for {label}");
    }

    #endregion

    #region 3. Sidebar Navigation — Menu Items

    [Fact]
    public void Sidebar_Script_FileExists()
    {
        File.Exists(Path.Combine(_shellDir, "mestech-sidebar.js"))
            .Should().BeTrue("mestech-sidebar.js drives sidebar navigation");
    }

    [Theory]
    [InlineData("Dashboard", "dashboard")]
    [InlineData("Stok Yonetimi", "stock")]
    [InlineData("Siparisler", "orders")]
    [InlineData("Finans", "finance-commission")]
    [InlineData("Kargo", "shipping")]
    [InlineData("Raporlar", "reports")]
    [InlineData("Entegrasyonlar", "integrations")]
    [InlineData("Dropshipping", "dropshipping")]
    [InlineData("MESA AI", "mesa-ai")]
    [InlineData("Ayarlar", "settings")]
    [InlineData("Yardim", "help-center")]
    public void Sidebar_HasMenuItem(string label, string page)
    {
        var js = ReadShellFile("mestech-sidebar.js");
        js.Should().Contain($"label: '{label}'",
            $"sidebar must have menu item '{label}'");
        js.Should().Contain($"page: '{page}'",
            $"sidebar menu item '{label}' must route to page '{page}'");
    }

    [Theory]
    [InlineData("Trendyol", "trendyol-dashboard")]
    [InlineData("Hepsiburada", "hb-dashboard")]
    [InlineData("N11", "n11-dashboard")]
    [InlineData("Ciceksepeti", "cs-dashboard")]
    [InlineData("Pazarama", "pz-dashboard")]
    [InlineData("Amazon TR", "amazon-dashboard")]
    [InlineData("Bitrix24", "bitrix24-dashboard")]
    [InlineData("OpenCart", "opencart-dashboard")]
    public void Sidebar_HasPlatformChild(string label, string page)
    {
        var js = ReadShellFile("mestech-sidebar.js");
        js.Should().Contain($"label: '{label}'",
            $"sidebar 'Entegrasyonlar' sub-menu must include '{label}'");
        js.Should().Contain($"page: '{page}'",
            $"platform '{label}' must route to '{page}'");
    }

    [Fact]
    public void Sidebar_ExportsPublicApi()
    {
        var js = ReadShellFile("mestech-sidebar.js");
        js.Should().Contain("window.MesTechSidebar",
            "sidebar must export public API on window.MesTechSidebar");
        js.Should().Contain("render:", "API must expose render()");
        js.Should().Contain("toggle:", "API must expose toggle()");
        js.Should().Contain("setActive:", "API must expose setActive()");
        js.Should().Contain("isExpanded:", "API must expose isExpanded()");
    }

    [Fact]
    public void Sidebar_HasDividers()
    {
        var js = ReadShellFile("mestech-sidebar.js");
        var dividerCount = Regex.Matches(js, @"divider:\s*true").Count;
        dividerCount.Should().BeGreaterOrEqualTo(2,
            "sidebar should have at least 2 dividers to separate sections");
    }

    #endregion

    #region 4. Wallpaper Support

    [Theory]
    [InlineData("space-default")]
    [InlineData("nature")]
    [InlineData("city")]
    [InlineData("abstract")]
    [InlineData("ocean")]
    public void Shell_Css_HasWallpaperVariant(string wallpaper)
    {
        var css = ReadShellFile("mestech-shell.css");
        css.Should().Contain($"data-wallpaper=\"{wallpaper}\"",
            $"CSS must define wallpaper variant '{wallpaper}'");
    }

    [Fact]
    public void Shell_Html_HasWallpaperPicker()
    {
        var html = ReadShellFile("mestech-shell.html");
        html.Should().Contain("wallpaper-picker",
            "wallpaper picker UI must be present in shell HTML");
    }

    [Theory]
    [InlineData("space-default.svg")]
    [InlineData("nature-mountains.svg")]
    [InlineData("city-night.svg")]
    [InlineData("abstract-gradient.svg")]
    [InlineData("ocean-calm.svg")]
    public void Shell_WallpaperAssets_Exist(string filename)
    {
        var path = Path.Combine(_shellDir, "wallpapers", filename);
        File.Exists(path).Should().BeTrue(
            $"wallpaper asset '{filename}' must exist in frontend/shell/wallpapers/");
    }

    #endregion

    #region 5. Glass Morphism CSS

    [Fact]
    public void Shell_Css_HasGlassCustomProperties()
    {
        var css = ReadShellFile("mestech-shell.css");
        css.Should().Contain("--glass-bg",
            "glass background custom property required");
        css.Should().Contain("--glass-border",
            "glass border custom property required");
        css.Should().Contain("--glass-blur",
            "glass blur custom property required");
        css.Should().Contain("--glass-shadow",
            "glass shadow custom property required");
    }

    [Fact]
    public void Shell_Css_UsesBackdropFilter()
    {
        var css = ReadShellFile("mestech-shell.css");
        var backdropCount = Regex.Matches(css, @"backdrop-filter:\s*blur").Count;
        backdropCount.Should().BeGreaterOrEqualTo(3,
            "glass morphism requires backdrop-filter on header, sidebar, and inner header");
    }

    [Fact]
    public void Shell_Css_HasCardHoverLift()
    {
        var css = ReadShellFile("mestech-shell.css");
        css.Should().Contain("--card-hover-lift",
            "3D card hover lift custom property required for card depth effect");
    }

    [Fact]
    public void Shell_Css_HasGlassCard()
    {
        var css = ReadShellFile("mestech-shell.css");
        css.Should().Contain(".glass-card",
            "reusable .glass-card component must be defined");
    }

    #endregion

    #region 6. Responsive Breakpoints

    [Fact]
    public void Shell_Css_Has768pxBreakpoint()
    {
        var css = ReadShellFile("mestech-shell.css");
        css.Should().Contain("max-width: 768px",
            "768px responsive breakpoint is required per emirname spec");
    }

    [Fact]
    public void Shell_Css_ForcesSidebarCollapsedOnMobile()
    {
        var css = ReadShellFile("mestech-shell.css");
        // The 768px media query should force sidebar collapsed width
        var mobileSection = ExtractMediaQuery(css, "768px");
        mobileSection.Should().NotBeNull("768px media query must exist");
        mobileSection.Should().Contain("sidebar-collapsed",
            "mobile breakpoint must force sidebar to collapsed width");
    }

    [Fact]
    public void Shell_Css_HasLayoutCustomProperties()
    {
        var css = ReadShellFile("mestech-shell.css");
        css.Should().Contain("--header-height:", "header height must be defined");
        css.Should().Contain("--inner-header-height:", "inner header height must be defined");
        css.Should().Contain("--sidebar-collapsed:", "collapsed sidebar width must be defined");
        css.Should().Contain("--sidebar-expanded:", "expanded sidebar width must be defined");
    }

    [Fact]
    public void Shell_Css_SidebarTransitions()
    {
        var css = ReadShellFile("mestech-shell.css");
        css.Should().Contain("transition: width",
            "sidebar must animate width changes for smooth collapse/expand");
    }

    #endregion

    #region 7. Router — Page Registry

    [Fact]
    public void Router_Script_FileExists()
    {
        File.Exists(Path.Combine(_shellDir, "mestech-router.js"))
            .Should().BeTrue("mestech-router.js is required for page navigation");
    }

    [Fact]
    public void Router_ExportsPublicApi()
    {
        var js = ReadShellFile("mestech-router.js");
        js.Should().Contain("window.MesTechRouter",
            "router must export public API on window.MesTechRouter");
        js.Should().Contain("navigate:", "API must expose navigate()");
        js.Should().Contain("getRoutes:", "API must expose getRoutes()");
    }

    [Theory]
    [InlineData("dashboard")]
    [InlineData("products")]
    [InlineData("orders")]
    [InlineData("invoices")]
    [InlineData("reports")]
    [InlineData("settings")]
    [InlineData("trendyol-dashboard")]
    [InlineData("hb-dashboard")]
    [InlineData("n11-dashboard")]
    [InlineData("bitrix24-dashboard")]
    public void Router_HasRoute(string pageName)
    {
        var js = ReadShellFile("mestech-router.js");
        js.Should().Contain($"'{pageName}':",
            $"router must have route for '{pageName}'");
    }

    [Theory]
    [InlineData("bitrix24-deals")]
    [InlineData("bitrix24-contacts")]
    [InlineData("bitrix24-products")]
    [InlineData("bitrix24-sync")]
    [InlineData("bitrix24-settings")]
    [InlineData("bitrix24-webhooks")]
    public void Router_HasBitrix24SubRoutes(string pageName)
    {
        var js = ReadShellFile("mestech-router.js");
        js.Should().Contain($"'{pageName}':",
            $"router must have Bitrix24 sub-route for '{pageName}'");
    }

    [Fact]
    public void Router_HasErrorHandling()
    {
        var js = ReadShellFile("mestech-router.js");
        js.Should().Contain("showErrorState",
            "router must handle page load errors");
        js.Should().Contain("LOAD_TIMEOUT_MS",
            "router must have a load timeout mechanism");
    }

    [Fact]
    public void Router_HasHashNavigation()
    {
        var js = ReadShellFile("mestech-router.js");
        js.Should().Contain("hashchange",
            "router must listen for hash change events for browser navigation");
    }

    #endregion

    #region 8. Panel Integrity — Unified Pages

    [Theory]
    [InlineData("unified-products.html")]
    [InlineData("unified-orders.html")]
    [InlineData("unified-stock.html")]
    [InlineData("unified-invoice.html")]
    [InlineData("unified-dashboard.html")]
    [InlineData("unified-reports.html")]
    [InlineData("unified-settings.html")]
    [InlineData("unified-categories.html")]
    [InlineData("unified-notifications.html")]
    [InlineData("unified-dropshipping.html")]
    [InlineData("unified-ai-tools.html")]
    [InlineData("unified-akademi.html")]
    [InlineData("unified-returns.html")]
    [InlineData("unified-cari.html")]
    [InlineData("unified-commission.html")]
    [InlineData("unified-payments.html")]
    public void Panel_UnifiedPage_Exists(string filename)
    {
        var path = Path.Combine(_repoRoot, "frontend", "panel", "pages", "unified", filename);
        File.Exists(path).Should().BeTrue(
            $"unified page '{filename}' must exist in frontend/panel/pages/unified/");
    }

    [Fact]
    public void Panel_Components_HeaderExists()
    {
        var path = Path.Combine(_repoRoot, "frontend", "panel", "components", "Header", "header.html");
        File.Exists(path).Should().BeTrue(
            "header component must exist for legacy panel compatibility");
    }

    [Fact]
    public void Panel_Components_SidebarExists()
    {
        var path = Path.Combine(_repoRoot, "frontend", "panel", "components", "Sidebar", "sidebar.html");
        File.Exists(path).Should().BeTrue(
            "sidebar component must exist in panel components");
    }

    #endregion

    #region Helpers

    private string ReadShellFile(string filename)
    {
        var path = Path.Combine(_shellDir, filename);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Shell file not found: {path}");
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Extract the content of a @media (max-width: Xpx) block.
    /// Simple bracket-counting extraction.
    /// </summary>
    private static string? ExtractMediaQuery(string css, string breakpoint)
    {
        var pattern = $@"@media\s*\(max-width:\s*{Regex.Escape(breakpoint)}\)";
        var match = Regex.Match(css, pattern);
        if (!match.Success) return null;

        var startIdx = match.Index + match.Length;
        var braceStart = css.IndexOf('{', startIdx);
        if (braceStart < 0) return null;

        var depth = 0;
        var braceEnd = -1;
        for (int i = braceStart; i < css.Length; i++)
        {
            if (css[i] == '{') depth++;
            else if (css[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    braceEnd = i;
                    break;
                }
            }
        }

        if (braceEnd < 0) return null;
        return css.Substring(braceStart, braceEnd - braceStart + 1);
    }

    private static string FindRepoRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;

        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, "MesTech_Trendyol")) &&
                Directory.Exists(Path.Combine(dir, "MesTech_Stok")) &&
                Directory.Exists(Path.Combine(dir, "frontend")))
                return dir;

            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new DirectoryNotFoundException(
            "Could not find MesTech repo root from: " + AppDomain.CurrentDomain.BaseDirectory);
    }

    #endregion
}
