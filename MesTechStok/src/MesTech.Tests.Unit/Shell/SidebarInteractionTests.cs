using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MesTech.Tests.Unit.Shell;

/// <summary>
/// DEV 5 — Dalga 7.7 Task 5.03 (v4 update): Sidebar interaction tests.
/// Verifies ESC key closes sidebar, overlay click closes sidebar,
/// Ctrl+B toggles, Ctrl+K focuses search, 768px responsive
/// auto-collapse, and sidebar overlay CSS — via source-file scanning.
///
/// v4: Sidebar logic split between mestech-shell.js (Ctrl+K, wallpaper ESC)
/// and mestech-sidebar.js (SidebarController: ESC, Ctrl+B, overlay, resize).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Sidebar")]
[Trait("Category", "UIAutomation")]
public class SidebarInteractionTests
{
    private readonly string _shellDir;
    private readonly string _cssContent;
    private readonly string _jsContent;
    private readonly string _sidebarJsContent;
    private readonly string _htmlContent;

    public SidebarInteractionTests()
    {
        var repoRoot = FindRepoRoot();
        _shellDir = Path.Combine(repoRoot, "frontend", "shell");
        _cssContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.css"));
        _jsContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.js"));
        _sidebarJsContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-sidebar.js"));
        _htmlContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.html"));
    }

    #region 1. ESC Key Closes Sidebar (v4: SidebarController)

    [Fact]
    public void JS_EscapeKeyHandlerExists()
    {
        // v4: Escape handler is in SidebarController (mestech-sidebar.js)
        _sidebarJsContent.Should().Contain("e.key !== \"Escape\"",
            "SidebarController must handle Escape key press");
    }

    [Fact]
    public void JS_EscapeClosesDropdownFirst()
    {
        // v4: ESC first closes Daha... dropdown, then sidebar
        _sidebarJsContent.Should().Contain("classList.contains(\"open\")",
            "Escape handler must check if dropdown is open");
        _sidebarJsContent.Should().Contain("classList.remove(\"open\")",
            "Escape handler must close dropdown by removing 'open' class");
    }

    [Fact]
    public void JS_EscapeClosesMobileSidebar()
    {
        // v4: ESC closes mobile sidebar via _closeMobile
        _sidebarJsContent.Should().Contain("mobile-open",
            "Escape handler must check for mobile-open state");
    }

    [Fact]
    public void JS_EscapeCollapsesSidebar()
    {
        // v4: ESC collapses desktop sidebar by adding 'collapsed' class
        _sidebarJsContent.Should().Contain("classList.add(\"collapsed\")",
            "Escape handler must collapse desktop sidebar");
    }

    [Fact]
    public void JS_EscapeClosesWallpaperPickerInShell()
    {
        // v4: shell.js handles Escape for wallpaper picker
        _jsContent.Should().Contain("closeWallpaperPicker()",
            "shell.js Escape handler should close wallpaper picker");
    }

    #endregion

    #region 2. Overlay for Mobile Sidebar (v4: SidebarController)

    [Fact]
    public void HTML_HasSidebarOverlayElement()
    {
        _htmlContent.Should().Contain("id=\"sidebar-overlay\"",
            "shell HTML must have a sidebar-overlay element");
    }

    [Fact]
    public void HTML_OverlayHasCssClass()
    {
        _htmlContent.Should().Contain("sidebar-overlay",
            "sidebar overlay element must exist in HTML");
    }

    [Fact]
    public void JS_SidebarControllerReferencesOverlay()
    {
        // v4: SidebarController constructor reads sidebar-overlay element
        _sidebarJsContent.Should().Contain("sidebar-overlay",
            "SidebarController must reference sidebar-overlay element");
    }

    [Fact]
    public void JS_MobileToggleControlsOverlay()
    {
        // v4: _toggleMobile shows/hides overlay via style.display
        _sidebarJsContent.Should().Contain("overlay.style.display",
            "mobile toggle must control overlay display");
    }

    #endregion

    #region 3. Sidebar Overlay CSS

    [Fact]
    public void CSS_OverlayExists()
    {
        _cssContent.Should().Contain(".sidebar-overlay",
            "CSS must define .sidebar-overlay");
    }

    [Fact]
    public void CSS_OverlayIsFixedPositioned()
    {
        var block = ExtractCssBlockAfterSelector(_cssContent, ".sidebar-overlay {");
        block.Should().NotBeNull("sidebar-overlay CSS block must exist");
        block.Should().Contain("position: fixed",
            "overlay must be fixed positioned");
    }

    [Fact]
    public void CSS_OverlayHiddenByDefault()
    {
        var block = ExtractCssBlockAfterSelector(_cssContent, ".sidebar-overlay {");
        block.Should().NotBeNull();
        block.Should().Contain("display: none",
            "overlay must be hidden by default");
    }

    [Fact]
    public void CSS_OverlayHasSemiTransparentBackground()
    {
        var block = ExtractCssBlockAfterSelector(_cssContent, ".sidebar-overlay {");
        block.Should().NotBeNull();
        block.Should().Contain("rgba(0,0,0",
            "overlay must have semi-transparent background");
    }

    [Fact]
    public void CSS_OverlayCoversFullScreen()
    {
        var block = ExtractCssBlockAfterSelector(_cssContent, ".sidebar-overlay {");
        block.Should().NotBeNull();
        block.Should().Contain("inset: 0",
            "overlay must use inset: 0 to cover full screen");
    }

    #endregion

    #region 4. Ctrl+B Toggles Sidebar (v4: SidebarController)

    [Fact]
    public void JS_CtrlBShortcutExists()
    {
        // v4: Ctrl+B is in SidebarController (mestech-sidebar.js)
        _sidebarJsContent.Should().Contain("e.key === \"b\"",
            "SidebarController must handle 'b' key for Ctrl+B shortcut");
    }

    [Fact]
    public void JS_CtrlBUsesCtrlOrCmd()
    {
        _sidebarJsContent.Should().Contain("e.ctrlKey || e.metaKey",
            "keyboard shortcuts must support both Ctrl and Cmd (Mac)");
    }

    [Fact]
    public void JS_CtrlBCallsSidebarToggle()
    {
        // v4: Ctrl+B calls self.toggle()
        _sidebarJsContent.Should().Contain("self.toggle()",
            "Ctrl+B must call sidebar toggle");
    }

    [Fact]
    public void JS_CtrlBPreventsDefault()
    {
        _sidebarJsContent.Should().Contain("e.preventDefault()",
            "Ctrl+B must prevent default browser behavior");
    }

    #endregion

    #region 5. Ctrl+K Focuses Search (shell.js)

    [Fact]
    public void JS_CtrlKShortcutExists()
    {
        _jsContent.Should().Contain("e.key === \"k\"",
            "shell.js must handle 'k' key for Ctrl+K shortcut");
    }

    [Fact]
    public void JS_CtrlKFocusesSearchInput()
    {
        _jsContent.Should().Contain("global-search",
            "Ctrl+K handler must reference global-search input");
        _jsContent.Should().Contain("searchInput.focus()",
            "Ctrl+K must focus the search input");
    }

    [Fact]
    public void HTML_HasGlobalSearchInput()
    {
        _htmlContent.Should().Contain("id=\"global-search\"",
            "shell HTML must have a global-search input");
    }

    #endregion

    #region 6. Responsive Auto-Collapse (v4: SidebarController)

    [Fact]
    public void JS_MobileBreakpointDefined()
    {
        // v4: MOBILE_BREAKPOINT is in shell.js as media query string
        _jsContent.Should().Contain("MOBILE_BREAKPOINT",
            "shell.js must define MOBILE_BREAKPOINT constant");
    }

    [Fact]
    public void JS_SidebarControllerHasMobileCheck()
    {
        // v4: SidebarController uses window.innerWidth <= 768
        _sidebarJsContent.Should().Contain("window.innerWidth <= 768",
            "SidebarController must check for mobile breakpoint at 768px");
    }

    [Fact]
    public void JS_SidebarControllerHasTabletCheck()
    {
        // v4: SidebarController auto-collapses on tablet (>768 && <=1024)
        _sidebarJsContent.Should().Contain("window.innerWidth <= 1024",
            "SidebarController must check for tablet breakpoint at 1024px");
    }

    [Fact]
    public void JS_SidebarControllerHasResizeHandler()
    {
        // v4: SidebarController listens for resize events
        _sidebarJsContent.Should().Contain("resize",
            "SidebarController must listen for window resize events");
    }

    [Fact]
    public void JS_AutoCollapsedStateFlagExists()
    {
        _jsContent.Should().Contain("_autoCollapsed",
            "shell.js must track auto-collapsed state");
    }

    [Fact]
    public void JS_InitResponsiveCalledInOnReady()
    {
        _jsContent.Should().Contain("initResponsiveSidebar()",
            "onReady must call initResponsiveSidebar()");
    }

    #endregion

    #region 7. Sidebar Toggle Button

    [Fact]
    public void HTML_HasSidebarToggleButton()
    {
        _htmlContent.Should().Contain("id=\"sidebar-toggle\"",
            "shell HTML must have a sidebar toggle button");
    }

    [Fact]
    public void HTML_ToggleButtonHasIcon()
    {
        _htmlContent.Should().Contain("id=\"toggle-icon\"",
            "toggle button must have a toggle-icon element");
        // v4: HTML starts with angles-left (expanded state); JS swaps to angles-right on collapse
        _htmlContent.Should().Contain("fa-angles-left",
            "toggle icon should default to angles-left (expanded state)");
    }

    [Fact]
    public void JS_SidebarControllerWiresToggButton()
    {
        // v4: SidebarController constructor reads sidebar-toggle button
        _sidebarJsContent.Should().Contain("sidebar-toggle",
            "SidebarController must reference sidebar-toggle button");
    }

    [Fact]
    public void JS_SidebarControllerUpdatesToggleIcon()
    {
        // v4: _updateIcon sets fa-angles-left/right (not chevron)
        _sidebarJsContent.Should().Contain("fa-angles-left",
            "expanded state should show angles-left icon");
        _sidebarJsContent.Should().Contain("fa-angles-right",
            "collapsed state should show angles-right icon");
    }

    #endregion

    #region 8. Sidebar CSS States

    [Fact]
    public void CSS_SidebarCollapsedWidth()
    {
        _cssContent.Should().Contain("--sidebar-collapsed: 60px",
            "collapsed sidebar width should be 60px");
    }

    [Fact]
    public void CSS_SidebarExpandedWidth()
    {
        _cssContent.Should().Contain("--sidebar-expanded: 240px",
            "expanded sidebar width should be 240px");
    }

    [Fact]
    public void CSS_SidebarExpandedState()
    {
        // v4: sidebar is 240px by default (expanded), collapsed via .collapsed class
        _cssContent.Should().Contain("--sidebar-w: 240px",
            "CSS must define default sidebar width (expanded state)");
        _cssContent.Should().Contain("--sidebar-expanded: 240px",
            "expanded sidebar width variable must be defined");
    }

    [Fact]
    public void CSS_SidebarHasTransition()
    {
        var block = ExtractCssBlockAfterSelector(_cssContent, ".shell-sidebar {");
        block.Should().NotBeNull();
        block.Should().Contain("transition: width",
            "sidebar must have width transition for smooth animation");
    }

    [Fact]
    public void CSS_CollapsedLabelsHidden()
    {
        // v4: collapsed sidebar hides .s-label (not .sidebar-link .label)
        _cssContent.Should().Contain(".shell-sidebar.collapsed .s-label",
            "collapsed sidebar must hide link labels (.s-label)");
    }

    [Fact]
    public void CSS_CollapsedSectionTitlesHidden()
    {
        // v4: collapsed sidebar hides .s-section (not .sidebar-section-title)
        _cssContent.Should().Contain(".shell-sidebar.collapsed .s-section",
            "collapsed sidebar must hide section titles (.s-section)");
    }

    [Fact]
    public void CSS_ContentAreaShiftsWithSidebar()
    {
        // v4: content/inner-header shift uses .collapsed ~ sibling selector
        _cssContent.Should().Contain(".shell-sidebar.collapsed ~ .shell-content",
            "content area must shift when sidebar is collapsed");
        _cssContent.Should().Contain(".shell-sidebar.collapsed ~ .shell-inner-header",
            "inner header must shift when sidebar is collapsed");
    }

    #endregion

    #region 9. Keyboard Shortcuts Registration

    [Fact]
    public void JS_InitKeyboardShortcutsExists()
    {
        var fn = ExtractFunction(_jsContent, "initKeyboardShortcuts");
        fn.Should().NotBeNullOrEmpty("initKeyboardShortcuts function must exist");
    }

    [Fact]
    public void JS_ShortcutsRegisteredInOnReady()
    {
        _jsContent.Should().Contain("initKeyboardShortcuts()",
            "onReady must call initKeyboardShortcuts()");
    }

    [Fact]
    public void JS_ShortcutsUseKeydownEvent()
    {
        // v4: double-quoted "keydown"
        var fn = ExtractFunction(_jsContent, "initKeyboardShortcuts");
        fn.Should().NotBeNull();
        fn.Should().Contain("\"keydown\"",
            "keyboard shortcuts must use keydown event");
    }

    [Fact]
    public void JS_SidebarCloseBehaviorsCalledInOnReady()
    {
        _jsContent.Should().Contain("initSidebarCloseBehaviors()",
            "onReady must call initSidebarCloseBehaviors()");
    }

    #endregion

    #region 10. Shell Public API

    [Fact]
    public void JS_ShellExportsToggleSidebar()
    {
        _jsContent.Should().Contain("toggleSidebar:",
            "MesTechShell public API must expose toggleSidebar");
    }

    [Fact]
    public void JS_ShellExportsNavigate()
    {
        _jsContent.Should().Contain("navigate:",
            "MesTechShell public API must expose navigate");
    }

    [Fact]
    public void JS_ShellObjectExported()
    {
        _jsContent.Should().Contain("window.MesTechShell",
            "shell must export API on window.MesTechShell");
    }

    #endregion

    #region Helpers

    private static string? ExtractFunction(string js, string functionName)
    {
        var pattern = $@"function\s+{Regex.Escape(functionName)}\s*\(";
        var match = Regex.Match(js, pattern);
        if (!match.Success) return null;

        // Find the opening brace and balance braces to find the end
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
