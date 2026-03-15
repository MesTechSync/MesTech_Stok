using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MesTech.Tests.Unit.Shell;

/// <summary>
/// DEV 5 — Dalga 7.7.1 Task 5.02 (v4 update): Inner header structure tests.
/// Verifies:
/// 1. Visible tab count (12 visible + "Daha...")
/// 2. "Daha..." dropdown exists with overflow items (id="more-wrap", id="more-menu")
/// 3. Bitrix24 removed from visible tabs (moved to dropdown)
/// 4. Dropdown items are wired and navigable
/// 5. CSS styles for dropdown exist (.more-wrap, .more-btn, .more-menu)
/// All tests use source-file scanning (no browser needed).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "InnerHeader")]
[Trait("Category", "UIAutomation")]
public class InnerHeaderStructureTests
{
    private readonly string _shellDir;
    private readonly string _htmlContent;
    private readonly string _cssContent;
    private readonly string _jsContent;

    public InnerHeaderStructureTests()
    {
        var repoRoot = FindRepoRoot();
        _shellDir = Path.Combine(repoRoot, "frontend", "shell");
        _htmlContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.html"));
        _cssContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.css"));
        _jsContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-sidebar.js"));
    }

    #region 1. Visible Tab Count

    [Fact]
    public void InnerHeader_VisibleTabCount_IsAtMost14()
    {
        var visibleTabs = GetVisibleTabPages();

        visibleTabs.Count.Should().BeLessOrEqualTo(14,
            "inner header should have max 14 visible tabs for readability");
    }

    [Fact]
    public void InnerHeader_VisibleTabCount_IsAtLeast8()
    {
        var visibleTabs = GetVisibleTabPages();

        visibleTabs.Count.Should().BeGreaterOrEqualTo(8,
            "inner header should have at least 8 visible tabs (dashboard + platforms + core)");
    }

    [Fact]
    public void InnerHeader_HasDashboardTab()
    {
        var visibleTabs = GetVisibleTabPages();
        visibleTabs.Should().Contain("dashboard",
            "Dashboard must always be a visible tab");
    }

    [Theory]
    [InlineData("trendyol-dashboard", "Trendyol")]
    [InlineData("hb-dashboard", "HB")]
    [InlineData("n11-dashboard", "N11")]
    [InlineData("cs-dashboard", "CS")]
    [InlineData("pz-dashboard", "Pazarama")]
    [InlineData("amazon-dashboard", "Amazon")]
    public void InnerHeader_MarketplacePlatform_IsVisible(string page, string label)
    {
        var visibleTabs = GetVisibleTabPages();
        visibleTabs.Should().Contain(page,
            $"marketplace platform '{label}' should be a visible inner header tab");
    }

    [Theory]
    [InlineData("products", "Urunler")]
    [InlineData("unified-orders", "Siparisler")]
    [InlineData("shipping-ops", "Kargo")]
    [InlineData("reports-all", "Raporlar")]
    public void InnerHeader_CoreModule_IsVisible(string page, string label)
    {
        var visibleTabs = GetVisibleTabPages();
        visibleTabs.Should().Contain(page,
            $"core module '{label}' should be a visible inner header tab");
    }

    #endregion

    #region 2. Bitrix24 Not in Visible Tabs

    [Fact]
    public void InnerHeader_Bitrix24_NotInVisibleTabs()
    {
        var visibleTabs = GetVisibleTabPages();
        visibleTabs.Should().NotContain("bitrix24-dashboard",
            "Bitrix24 is a CRM integration, not a marketplace -- must be in 'Daha...' dropdown");
    }

    [Fact]
    public void InnerHeader_Bitrix24_InDropdown()
    {
        var dropdownPages = GetDropdownPages();
        dropdownPages.Should().Contain("bitrix24-dashboard",
            "Bitrix24 must be accessible via 'Daha...' dropdown");
    }

    #endregion

    #region 3. "Daha..." Dropdown Exists

    [Fact]
    public void HTML_HasMoreDropdownWrapper()
    {
        // v4: wrapper is id="more-wrap" (not "inner-header-more")
        _htmlContent.Should().Contain("id=\"more-wrap\"",
            "HTML must have the more-wrap wrapper element");
    }

    [Fact]
    public void HTML_HasMoreDropdownButton()
    {
        _htmlContent.Should().Contain("id=\"more-dropdown-btn\"",
            "HTML must have the Daha... trigger button");
    }

    [Fact]
    public void HTML_MoreButtonText_ContainsDaha()
    {
        // The button text should contain "Daha..."
        var btnRegex = new Regex(@"more-dropdown-btn[^>]*>[\s\S]*?Daha\s*\.\.\.");
        btnRegex.IsMatch(_htmlContent).Should().BeTrue(
            "the dropdown trigger button must display 'Daha...'");
    }

    [Fact]
    public void HTML_HasMoreMenuContainer()
    {
        // v4: menu container is id="more-menu" (not "more-dropdown")
        _htmlContent.Should().Contain("id=\"more-menu\"",
            "HTML must have the more-menu container for overflow items");
    }

    [Fact]
    public void HTML_DropdownHasItems()
    {
        var dropdownPages = GetDropdownPages();
        dropdownPages.Count.Should().BeGreaterOrEqualTo(8,
            "dropdown should contain at least 8 overflow items");
    }

    [Fact]
    public void HTML_MoreButtonHasChevronIcon()
    {
        // The "Daha..." button should have a chevron-down icon
        _htmlContent.Should().Contain("more-dropdown-btn",
            "more button must exist");
        var btnSection = ExtractHtmlSection(_htmlContent, "more-dropdown-btn");
        btnSection.Should().Contain("fa-chevron-down",
            "more button should have a dropdown chevron icon");
    }

    #endregion

    #region 4. Dropdown Contents

    [Theory]
    [InlineData("invoices", "Faturalar")]
    [InlineData("commission", "Komisyonlar")]
    [InlineData("marketing-campaigns", "Pazarlama")]
    [InlineData("ad-analytics", "Reklamlar")]
    [InlineData("analytics", "Analitik")]
    [InlineData("messages", "Mesajlar")]
    [InlineData("kanban", "Kanban")]
    [InlineData("dropshipping", "Dropshipping")]
    [InlineData("bitrix24-dashboard", "Bitrix24")]
    [InlineData("mesa-ai", "MESA AI")]
    [InlineData("settings", "Ayarlar")]
    public void Dropdown_ContainsExpectedItem(string page, string label)
    {
        var dropdownPages = GetDropdownPages();
        dropdownPages.Should().Contain(page,
            $"'{label}' should be in the 'Daha...' dropdown (page='{page}')");
    }

    [Fact]
    public void Dropdown_ItemsHaveIcons()
    {
        // v4: dropdown items are <a> tags with <i class="fa-solid fa-..."> icons
        var menuSection = ExtractMoreMenuSection();
        var iconPattern = new Regex(@"<i\s+class=""fa-solid\s+fa-");
        var matches = iconPattern.Matches(menuSection);
        matches.Count.Should().BeGreaterOrEqualTo(8,
            "dropdown items should have FontAwesome icons");
    }

    [Fact]
    public void Dropdown_AllItemsHaveDataPage()
    {
        // v4: dropdown items are <a data-page="..."> (not .more-dropdown-item)
        var menuSection = ExtractMoreMenuSection();
        var itemPattern = new Regex(@"<a\s+data-page=""([^""]+)""");
        var matches = itemPattern.Matches(menuSection);
        matches.Count.Should().BeGreaterOrEqualTo(8,
            "all dropdown items must have data-page attributes");

        foreach (Match m in matches)
        {
            m.Groups[1].Value.Should().NotBeNullOrWhiteSpace(
                "data-page value must not be empty");
        }
    }

    #endregion

    #region 5. CSS Styles for Dropdown

    [Fact]
    public void CSS_HasMoreWrapStyles()
    {
        // v4: wrapper class is .more-wrap (not .inner-header-more)
        _cssContent.Should().Contain(".more-wrap",
            "CSS must have .more-wrap wrapper styles");
    }

    [Fact]
    public void CSS_HasMoreBtnStyles()
    {
        _cssContent.Should().Contain(".more-btn",
            "CSS must have .more-btn button styles");
    }

    [Fact]
    public void CSS_HasMoreMenuStyles()
    {
        // v4: menu class is .more-menu (not .more-dropdown)
        _cssContent.Should().Contain(".more-menu",
            "CSS must have .more-menu container styles");
    }

    [Fact]
    public void CSS_HasMoreMenuItemStyles()
    {
        // v4: items are .more-menu a (not .more-dropdown-item)
        _cssContent.Should().Contain(".more-menu a",
            "CSS must have .more-menu a item styles");
    }

    [Fact]
    public void CSS_DropdownHasOpenState()
    {
        // v4: open state is .more-wrap.open (not .more-dropdown.open)
        _cssContent.Should().Contain(".more-wrap.open",
            "CSS must style the open state of dropdown wrapper");
    }

    [Fact]
    public void CSS_DropdownHasDarkTheme()
    {
        _cssContent.Should().Contain("[data-theme=\"dark\"] .more-menu",
            "CSS must have dark theme overrides for dropdown menu");
    }

    [Fact]
    public void CSS_DropdownItemHasHoverState()
    {
        _cssContent.Should().Contain(".more-menu a:hover",
            "CSS must have hover state for dropdown items");
    }

    #endregion

    #region 6. JS Wiring for Dropdown (v4: SidebarController)

    [Fact]
    public void JS_MoreDropdownToggleWired()
    {
        // v4: SidebarController wires more-dropdown-btn
        _jsContent.Should().Contain("more-dropdown-btn",
            "SidebarController must reference more-dropdown-btn element");
        _jsContent.Should().Contain("more-wrap",
            "SidebarController must reference more-wrap wrapper");
    }

    [Fact]
    public void JS_DropdownItemsWired()
    {
        // v4: SidebarController wires more-menu a click handlers
        _jsContent.Should().Contain("more-menu",
            "SidebarController must wire click handlers for dropdown menu items");
    }

    [Fact]
    public void JS_DropdownClosesOnOutsideClick()
    {
        _jsContent.Should().Contain("moreWrap.contains(e.target)",
            "SidebarController must close dropdown when clicking outside");
    }

    [Fact]
    public void JS_DropdownItemsNavigateViaHash()
    {
        // v4: dropdown items navigate via hash
        _jsContent.Should().Contain("window.location.hash",
            "dropdown items must navigate via hash");
    }

    [Fact]
    public void JS_EscapeClosesDropdownFirst()
    {
        // Escape should close "Daha..." dropdown before sidebar
        _jsContent.Should().Contain("Escape",
            "Escape handler must reference Escape key");
    }

    #endregion

    #region 7. Tab Styling

    [Fact]
    public void CSS_HasInnerTabStyles()
    {
        _cssContent.Should().Contain(".inner-tab",
            "CSS must style inner-tab elements");
    }

    [Fact]
    public void CSS_HasTabDividerStyles()
    {
        // CSS defines .tab-divider for visual grouping (may be used dynamically)
        _cssContent.Should().Contain(".tab-divider",
            "CSS must define tab divider styles");
    }

    #endregion

    #region 8. Inner Header Layout

    [Fact]
    public void HTML_HasInnerHeaderElement()
    {
        _htmlContent.Should().Contain("id=\"inner-header\"",
            "shell HTML must have the inner-header element");
    }

    [Fact]
    public void CSS_InnerHeaderIsFixed()
    {
        _cssContent.Should().Contain(".shell-inner-header");
        var block = ExtractCssBlockAfterSelector(_cssContent, ".shell-inner-header {");
        block.Should().NotBeNull();
        block.Should().Contain("position: fixed",
            "inner header must be fixed positioned");
    }

    [Fact]
    public void HTML_InnerHeaderComment_DescribesStructure()
    {
        // The comment should mention the tab count + dropdown
        _htmlContent.Should().Contain("12 tab",
            "inner header comment should document the 12 tab count");
    }

    #endregion

    #region Helpers

    private List<string> GetVisibleTabPages()
    {
        // Match data-page on .inner-tab elements that are NOT inside .more-menu
        // Strategy: find the more-menu section and exclude it
        var menuStart = _htmlContent.IndexOf("id=\"more-menu\"", StringComparison.Ordinal);

        var searchArea = menuStart > 0
            ? _htmlContent.Substring(0, menuStart)
            : _htmlContent;

        var tabPattern = new Regex(@"class=""inner-tab(?:\s+[^""]*)?""[^>]*data-page=""([^""]+)""");
        var matches = tabPattern.Matches(searchArea);

        // Also try data-page first
        var tabPattern2 = new Regex(@"data-page=""([^""]+)""[^>]*class=""inner-tab");
        var matches2 = tabPattern2.Matches(searchArea);

        var pages = new HashSet<string>();
        foreach (Match m in matches)
            pages.Add(m.Groups[1].Value);
        foreach (Match m in matches2)
            pages.Add(m.Groups[1].Value);

        return pages.ToList();
    }

    private List<string> GetDropdownPages()
    {
        // v4: dropdown is id="more-menu" (not "more-dropdown")
        var menuStart = _htmlContent.IndexOf("id=\"more-menu\"", StringComparison.Ordinal);
        if (menuStart < 0) return new List<string>();

        // Find closing </div>
        var menuEnd = _htmlContent.IndexOf("</div>", menuStart + 20, StringComparison.Ordinal);
        if (menuEnd < 0) menuEnd = _htmlContent.Length;

        var menuHtml = _htmlContent.Substring(menuStart,
            Math.Min(menuEnd - menuStart + 6, _htmlContent.Length - menuStart));

        // v4: items are <a data-page="...">
        var itemPattern = new Regex(@"data-page=""([^""]+)""");
        return itemPattern.Matches(menuHtml)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .ToList();
    }

    private string ExtractMoreMenuSection()
    {
        var menuStart = _htmlContent.IndexOf("id=\"more-menu\"", StringComparison.Ordinal);
        if (menuStart < 0) return string.Empty;
        var menuEnd = _htmlContent.IndexOf("</div>", menuStart + 20, StringComparison.Ordinal);
        if (menuEnd < 0) menuEnd = _htmlContent.Length;
        return _htmlContent.Substring(menuStart,
            Math.Min(menuEnd - menuStart + 6, _htmlContent.Length - menuStart));
    }

    private static string? ExtractHtmlSection(string html, string nearId)
    {
        var idx = html.IndexOf(nearId, StringComparison.Ordinal);
        if (idx < 0) return null;
        var start = Math.Max(0, idx - 50);
        var end = Math.Min(html.Length, idx + 200);
        return html.Substring(start, end - start);
    }

    private static string? ExtractFunction(string js, string functionName)
    {
        var pattern = $@"function\s+{Regex.Escape(functionName)}\s*\(";
        var match = Regex.Match(js, pattern);
        if (!match.Success) return null;

        var startIdx = match.Index;
        var braceIdx = js.IndexOf('{', startIdx);
        if (braceIdx < 0) return null;

        var depth = 0;
        var i = braceIdx;
        while (i < js.Length)
        {
            if (js[i] == '{') depth++;
            else if (js[i] == '}') depth--;
            if (depth == 0) break;
            i++;
        }

        return i < js.Length ? js.Substring(startIdx, i - startIdx + 1) : null;
    }

    private static string? ExtractCssBlockAfterSelector(string css, string selector)
    {
        var idx = css.IndexOf(selector, StringComparison.Ordinal);
        if (idx < 0) return null;
        var braceStart = css.IndexOf('{', idx);
        if (braceStart < 0) return null;
        var braceEnd = css.IndexOf('}', braceStart);
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
