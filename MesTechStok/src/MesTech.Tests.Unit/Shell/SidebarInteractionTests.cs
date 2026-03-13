using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MesTech.Tests.Unit.Shell;

/// <summary>
/// DEV 5 — Dalga 7.7 Task 5.03: Sidebar interaction tests.
/// Verifies ESC key closes sidebar, overlay click closes sidebar,
/// Ctrl+B toggles, Ctrl+K focuses search, 768px responsive
/// auto-collapse, and sidebar overlay CSS — via source-file scanning.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Sidebar")]
public class SidebarInteractionTests
{
    private readonly string _shellDir;
    private readonly string _cssContent;
    private readonly string _jsContent;
    private readonly string _htmlContent;

    public SidebarInteractionTests()
    {
        var repoRoot = FindRepoRoot();
        _shellDir = Path.Combine(repoRoot, "frontend", "shell");
        _cssContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.css"));
        _jsContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.js"));
        _htmlContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.html"));
    }

    #region 1. ESC Key Closes Sidebar

    [Fact]
    public void JS_EscapeKeyHandlerExists()
    {
        _jsContent.Should().Contain("e.key === 'Escape'",
            "shell.js must handle Escape key press");
    }

    [Fact]
    public void JS_EscapeCallsCloseSidebar()
    {
        // When Escape is pressed and sidebar is expanded, closeSidebar() is called
        _jsContent.Should().Contain("closeSidebar()",
            "Escape handler must call closeSidebar()");
    }

    [Fact]
    public void JS_CloseSidebarChecksIsExpanded()
    {
        // closeSidebar must guard with isExpanded() check
        _jsContent.Should().Contain("MesTechSidebar.isExpanded()",
            "closeSidebar must check if sidebar is expanded before toggling");
    }

    [Fact]
    public void JS_CloseSidebarCallsToggle()
    {
        // closeSidebar implementation: if expanded, call toggle()
        var closeSidebarFn = ExtractFunction(_jsContent, "closeSidebar");
        closeSidebarFn.Should().NotBeNullOrEmpty("closeSidebar function must exist");
        closeSidebarFn.Should().Contain("MesTechSidebar.toggle()",
            "closeSidebar must call MesTechSidebar.toggle()");
    }

    [Fact]
    public void JS_EscapeClosesWallpaperPickerAsSecondPriority()
    {
        // Escape first closes sidebar, then wallpaper picker (if sidebar was not open)
        _jsContent.Should().Contain("closeWallpaperPicker()",
            "Escape handler should also close wallpaper picker as fallback");
    }

    #endregion

    #region 2. Overlay Click Closes Sidebar

    [Fact]
    public void HTML_HasSidebarOverlayElement()
    {
        _htmlContent.Should().Contain("id=\"sidebar-overlay\"",
            "shell HTML must have a sidebar-overlay element");
    }

    [Fact]
    public void HTML_OverlayHasCssClass()
    {
        _htmlContent.Should().Contain("class=\"sidebar-overlay\"",
            "sidebar overlay must have the correct CSS class");
    }

    [Fact]
    public void JS_OverlayClickCallsCloseSidebar()
    {
        // The overlay click handler must call closeSidebar()
        _jsContent.Should().Contain("sidebar-overlay",
            "shell.js must reference sidebar-overlay element");
        var initFn = ExtractFunction(_jsContent, "initSidebarCloseBehaviors");
        initFn.Should().NotBeNullOrEmpty("initSidebarCloseBehaviors function must exist");
        initFn.Should().Contain("closeSidebar()",
            "overlay click handler must call closeSidebar()");
    }

    [Fact]
    public void JS_SidebarToggleEventShowsOverlay()
    {
        // sidebar:toggle event listener must show/hide overlay
        _jsContent.Should().Contain("sidebar:toggle",
            "shell.js must listen for sidebar:toggle custom event");
        _jsContent.Should().Contain("showSidebarOverlay()",
            "expanded state should show overlay");
        _jsContent.Should().Contain("hideSidebarOverlay()",
            "collapsed state should hide overlay");
    }

    [Fact]
    public void JS_ShowSidebarOverlayAddsActiveClass()
    {
        var fn = ExtractFunction(_jsContent, "showSidebarOverlay");
        fn.Should().NotBeNullOrEmpty("showSidebarOverlay function must exist");
        fn.Should().Contain("classList.add('active')",
            "showSidebarOverlay must add 'active' class");
    }

    [Fact]
    public void JS_HideSidebarOverlayRemovesActiveClass()
    {
        var fn = ExtractFunction(_jsContent, "hideSidebarOverlay");
        fn.Should().NotBeNullOrEmpty("hideSidebarOverlay function must exist");
        fn.Should().Contain("classList.remove('active')",
            "hideSidebarOverlay must remove 'active' class");
    }

    #endregion

    #region 3. Sidebar Overlay CSS

    [Fact]
    public void CSS_OverlayIsFixedPositioned()
    {
        _cssContent.Should().Contain(".sidebar-overlay",
            "CSS must define .sidebar-overlay");
        var block = ExtractCssBlockAfterSelector(_cssContent, ".sidebar-overlay {");
        block.Should().NotBeNull();
        block.Should().Contain("position: fixed",
            "overlay must be fixed positioned");
    }

    [Fact]
    public void CSS_OverlayHasCorrectZIndex()
    {
        var block = ExtractCssBlockAfterSelector(_cssContent, ".sidebar-overlay {");
        block.Should().NotBeNull();
        block.Should().Contain("z-index: 899",
            "overlay z-index must be 899 (below sidebar 900)");
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
    public void CSS_OverlayActiveDisplaysBlock()
    {
        _cssContent.Should().Contain(".sidebar-overlay.active",
            "CSS must define .sidebar-overlay.active");
        var block = ExtractCssBlockAfterSelector(_cssContent, ".sidebar-overlay.active");
        block.Should().NotBeNull();
        block.Should().Contain("display: block",
            "active overlay must display: block");
    }

    [Fact]
    public void CSS_OverlayHasSemiTransparentBackground()
    {
        var block = ExtractCssBlockAfterSelector(_cssContent, ".sidebar-overlay {");
        block.Should().NotBeNull();
        block.Should().Contain("rgba(0, 0, 0",
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

    #region 4. Ctrl+B Toggles Sidebar

    [Fact]
    public void JS_CtrlBShortcutExists()
    {
        _jsContent.Should().Contain("e.key === 'b'",
            "shell.js must handle 'b' key for Ctrl+B shortcut");
    }

    [Fact]
    public void JS_CtrlBUsesCtrlOrCmd()
    {
        _jsContent.Should().Contain("e.ctrlKey || e.metaKey",
            "keyboard shortcuts must support both Ctrl and Cmd (Mac)");
    }

    [Fact]
    public void JS_CtrlBCallsSidebarToggle()
    {
        _jsContent.Should().Contain("MesTechSidebar.toggle()",
            "Ctrl+B must call MesTechSidebar.toggle()");
    }

    [Fact]
    public void JS_CtrlBPreventsDefault()
    {
        // Ctrl+B should call e.preventDefault() to prevent browser bold
        _jsContent.Should().Contain("e.preventDefault()",
            "keyboard shortcuts must prevent default browser behavior");
    }

    #endregion

    #region 5. Ctrl+K Focuses Search

    [Fact]
    public void JS_CtrlKShortcutExists()
    {
        _jsContent.Should().Contain("e.key === 'k'",
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

    #region 6. 768px Responsive Auto-Collapse

    [Fact]
    public void JS_MobileBreakpointDefined()
    {
        _jsContent.Should().Contain("MOBILE_BREAKPOINT = '(max-width: 768px)'",
            "shell.js must define MOBILE_BREAKPOINT constant");
    }

    [Fact]
    public void JS_UsesMatchMedia()
    {
        _jsContent.Should().Contain("window.matchMedia(MOBILE_BREAKPOINT)",
            "responsive handler must use matchMedia with MOBILE_BREAKPOINT");
    }

    [Fact]
    public void JS_AutoCollapseOnNarrowViewport()
    {
        var fn = ExtractFunction(_jsContent, "initResponsiveSidebar");
        fn.Should().NotBeNullOrEmpty("initResponsiveSidebar function must exist");
        fn.Should().Contain("MesTechSidebar.isExpanded()",
            "responsive handler must check if sidebar is expanded");
        fn.Should().Contain("MesTechSidebar.toggle()",
            "responsive handler must toggle sidebar to collapse it");
    }

    [Fact]
    public void JS_AutoCollapsedStateFlagExists()
    {
        _jsContent.Should().Contain("_autoCollapsed",
            "shell.js must track auto-collapsed state");
    }

    [Fact]
    public void JS_ResponsiveHandlerListensForChanges()
    {
        // Must add listener for media query changes
        _jsContent.Should().Contain("mq.addEventListener",
            "responsive handler must listen for media query changes");
    }

    [Fact]
    public void JS_ResponsiveHandlerHasLegacyFallback()
    {
        // Older browsers use addListener instead of addEventListener
        _jsContent.Should().Contain("mq.addListener",
            "responsive handler should support older Safari/IE via addListener fallback");
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
    public void HTML_ToggleButtonHasChevronIcon()
    {
        _htmlContent.Should().Contain("id=\"toggle-icon\"",
            "toggle button must have a toggle-icon element");
        _htmlContent.Should().Contain("fa-chevron-right",
            "toggle icon should default to chevron-right (collapsed)");
    }

    [Fact]
    public void JS_SidebarToggleButtonWired()
    {
        var fn = ExtractFunction(_jsContent, "initSidebar");
        fn.Should().NotBeNullOrEmpty("initSidebar function must exist");
        fn.Should().Contain("sidebar-toggle",
            "initSidebar must wire the toggle button");
    }

    [Fact]
    public void JS_IconSyncListenerExists()
    {
        var fn = ExtractFunction(_jsContent, "initSidebarIconSync");
        fn.Should().NotBeNullOrEmpty("initSidebarIconSync function must exist");
        fn.Should().Contain("sidebar:toggle",
            "icon sync must listen for sidebar:toggle event");
        fn.Should().Contain("fa-chevron-left",
            "expanded state should show chevron-left");
        fn.Should().Contain("fa-chevron-right",
            "collapsed state should show chevron-right");
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
        _cssContent.Should().Contain("[data-state=\"expanded\"]",
            "CSS must handle sidebar expanded state via data-state attribute");
        _cssContent.Should().Contain("var(--sidebar-expanded)",
            "expanded state must use --sidebar-expanded variable");
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
        _cssContent.Should().Contain("[data-state=\"collapsed\"] .sidebar-link .label",
            "collapsed sidebar must hide link labels");
    }

    [Fact]
    public void CSS_CollapsedSectionTitlesHidden()
    {
        _cssContent.Should().Contain("[data-state=\"collapsed\"] .sidebar-section-title",
            "collapsed sidebar must hide section titles");
    }

    [Fact]
    public void CSS_ContentAreaShiftsWithSidebar()
    {
        _cssContent.Should().Contain("[data-state=\"expanded\"] ~ .shell-content",
            "content area must shift when sidebar is expanded");
        _cssContent.Should().Contain("[data-state=\"expanded\"] ~ .shell-inner-header",
            "inner header must shift when sidebar is expanded");
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
        // Keyboard shortcuts should use keydown (not keyup or keypress)
        var fn = ExtractFunction(_jsContent, "initKeyboardShortcuts");
        fn.Should().NotBeNull();
        fn.Should().Contain("'keydown'",
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
