using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MesTech.Tests.Unit.Shell;

/// <summary>
/// DEV 5 — Dalga 7.7.1 Task 5.02: Inner header structure tests.
/// Verifies:
/// 1. Visible tab count ≤ 14 (target: 12 visible + "Daha...")
/// 2. "Daha..." dropdown exists with overflow items
/// 3. Bitrix24 removed from visible tabs (moved to dropdown)
/// 4. Dropdown items are wired and navigable
/// 5. CSS styles for dropdown exist
/// All tests use source-file scanning (no browser needed).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "InnerHeader")]
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
        _jsContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.js"));
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
    [InlineData("orders", "Siparisler")]
    [InlineData("shipping", "Kargo")]
    [InlineData("reports", "Raporlar")]
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
            "Bitrix24 is a CRM integration, not a marketplace — must be in 'Daha...' dropdown");
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
        _htmlContent.Should().Contain("id=\"inner-header-more\"",
            "HTML must have the inner-header-more wrapper element");
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
    public void HTML_HasMoreDropdownContainer()
    {
        _htmlContent.Should().Contain("id=\"more-dropdown\"",
            "HTML must have the more-dropdown container for overflow items");
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
    [InlineData("finance-commission", "Finans")]
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
        // Each .more-dropdown-item should contain an <i> icon
        var itemPattern = new Regex(@"class=""more-dropdown-item""[^>]*>[\s\S]*?<i\s+class=""fas\s+fa-");
        var matches = itemPattern.Matches(_htmlContent);
        matches.Count.Should().BeGreaterOrEqualTo(8,
            "dropdown items should have FontAwesome icons");
    }

    [Fact]
    public void Dropdown_AllItemsHaveDataPage()
    {
        var itemPattern = new Regex(@"more-dropdown-item""\s+data-page=""([^""]+)""");
        var matches = itemPattern.Matches(_htmlContent);
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
    public void CSS_HasInnerHeaderMoreStyles()
    {
        _cssContent.Should().Contain(".inner-header-more",
            "CSS must have .inner-header-more wrapper styles");
    }

    [Fact]
    public void CSS_HasMoreBtnStyles()
    {
        _cssContent.Should().Contain(".more-btn",
            "CSS must have .more-btn button styles");
    }

    [Fact]
    public void CSS_HasMoreDropdownStyles()
    {
        _cssContent.Should().Contain(".more-dropdown",
            "CSS must have .more-dropdown container styles");
    }

    [Fact]
    public void CSS_HasMoreDropdownItemStyles()
    {
        _cssContent.Should().Contain(".more-dropdown-item",
            "CSS must have .more-dropdown-item styles");
    }

    [Fact]
    public void CSS_DropdownHasOpenState()
    {
        _cssContent.Should().Contain(".more-dropdown.open",
            "CSS must style the open state of dropdown");
    }

    [Fact]
    public void CSS_DropdownHasDarkTheme()
    {
        _cssContent.Should().Contain("[data-theme=\"dark\"] .more-dropdown",
            "CSS must have dark theme overrides for dropdown");
    }

    [Fact]
    public void CSS_DropdownItemHasHoverState()
    {
        _cssContent.Should().Contain(".more-dropdown-item:hover",
            "CSS must have hover state for dropdown items");
    }

    #endregion

    #region 6. JS Wiring for Dropdown

    [Fact]
    public void JS_HasCloseMoreDropdownFunction()
    {
        _jsContent.Should().Contain("function closeMoreDropdown",
            "shell.js must define closeMoreDropdown function");
    }

    [Fact]
    public void JS_MoreDropdownToggleWired()
    {
        _jsContent.Should().Contain("more-dropdown-btn",
            "shell.js must reference more-dropdown-btn element");
        _jsContent.Should().Contain("inner-header-more",
            "shell.js must reference inner-header-more wrapper");
    }

    [Fact]
    public void JS_DropdownItemsWired()
    {
        _jsContent.Should().Contain(".more-dropdown-item",
            "shell.js must wire click handlers for dropdown items");
    }

    [Fact]
    public void JS_DropdownClosesOnOutsideClick()
    {
        _jsContent.Should().Contain("moreWrapper.contains(e.target)",
            "shell.js must close dropdown when clicking outside");
    }

    [Fact]
    public void JS_DropdownItemsNavigateViaRouter()
    {
        // Dropdown items should call MesTechRouter.navigate
        var initFn = ExtractFunction(_jsContent, "initInnerHeaderTabs");
        initFn.Should().NotBeNull();
        initFn.Should().Contain("MesTechRouter.navigate(page)",
            "dropdown items must navigate via router");
        initFn.Should().Contain("closeMoreDropdown()",
            "dropdown items must close dropdown after navigation");
    }

    [Fact]
    public void JS_EscapeClosesDropdownFirst()
    {
        // Escape should close "Daha..." dropdown before sidebar
        _jsContent.Should().Contain("more-dropdown",
            "Escape handler must reference dropdown");
        _jsContent.Should().Contain("closeMoreDropdown()",
            "Escape handler must call closeMoreDropdown");
    }

    [Fact]
    public void JS_VisibleTabsExcludeMoreBtn()
    {
        // Tab wiring should exclude the "Daha..." button
        _jsContent.Should().Contain(".inner-tab:not(.more-btn)",
            "visible tab selector must exclude .more-btn");
    }

    #endregion

    #region 7. Tab Dividers

    [Fact]
    public void HTML_HasTabDividers()
    {
        var dividerCount = Regex.Matches(_htmlContent, @"class=""tab-divider""").Count;
        dividerCount.Should().BeGreaterOrEqualTo(2,
            "inner header should have at least 2 tab dividers for visual grouping");
    }

    [Fact]
    public void CSS_HasTabDividerStyles()
    {
        _cssContent.Should().Contain(".tab-divider",
            "CSS must style tab dividers");
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
    public void CSS_InnerHeaderHasCorrectZIndex()
    {
        var block = ExtractCssBlockAfterSelector(_cssContent, ".shell-inner-header {");
        block.Should().NotBeNull();
        block.Should().Contain("z-index: 800",
            "inner header z-index should be 800");
    }

    [Fact]
    public void HTML_InnerHeaderComment_DescribesStructure()
    {
        // The comment should mention the tab count + dropdown
        _htmlContent.Should().Contain("12 visible tabs",
            "inner header comment should document the 12 visible tab count");
    }

    #endregion

    #region Helpers

    private List<string> GetVisibleTabPages()
    {
        // Match data-page on .inner-tab elements that are NOT inside .more-dropdown
        // Strategy: find the more-dropdown section and exclude it
        var dropdownStart = _htmlContent.IndexOf("id=\"more-dropdown\"", StringComparison.Ordinal);

        var searchArea = dropdownStart > 0
            ? _htmlContent.Substring(0, dropdownStart)
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
        var dropdownStart = _htmlContent.IndexOf("id=\"more-dropdown\"", StringComparison.Ordinal);
        if (dropdownStart < 0) return new List<string>();

        var dropdownEnd = _htmlContent.IndexOf("</div>", dropdownStart + 100, StringComparison.Ordinal);
        // Find the closing div that matches (might need to go further)
        var searchIdx = dropdownStart;
        var depth = 0;
        while (searchIdx < _htmlContent.Length)
        {
            var nextOpen = _htmlContent.IndexOf("<div", searchIdx + 1, StringComparison.Ordinal);
            var nextClose = _htmlContent.IndexOf("</div>", searchIdx + 1, StringComparison.Ordinal);

            if (nextClose < 0) break;

            if (nextOpen >= 0 && nextOpen < nextClose)
            {
                depth++;
                searchIdx = nextOpen + 1;
            }
            else
            {
                if (depth == 0)
                {
                    dropdownEnd = nextClose + 6;
                    break;
                }
                depth--;
                searchIdx = nextClose + 1;
            }
        }

        var dropdownHtml = _htmlContent.Substring(dropdownStart,
            Math.Min(dropdownEnd - dropdownStart, _htmlContent.Length - dropdownStart));

        var itemPattern = new Regex(@"data-page=""([^""]+)""");
        return itemPattern.Matches(dropdownHtml)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .ToList();
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
