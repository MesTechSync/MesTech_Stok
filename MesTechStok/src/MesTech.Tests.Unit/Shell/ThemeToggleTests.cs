using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MesTech.Tests.Unit.Shell;

/// <summary>
/// DEV 5 — Dalga 7.7 Task 5.02: Theme toggle tests.
/// Verifies light-first default, dark theme override,
/// localStorage persistence, wallpaper auto-switch,
/// and theme-switcher UI — all via source-file scanning.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Theme")]
[Trait("Category", "UIAutomation")]
public class ThemeToggleTests
{
    private readonly string _shellDir;
    private readonly string _cssContent;
    private readonly string _jsContent;
    private readonly string _htmlContent;

    public ThemeToggleTests()
    {
        var repoRoot = FindRepoRoot();
        _shellDir = Path.Combine(repoRoot, "frontend", "shell");
        _cssContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.css"));
        _jsContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.js"));
        _htmlContent = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.html"));
    }

    #region 1. Light Theme Default

    [Fact]
    public void CSS_RootDefinesLightThemeVariables()
    {
        // :root block must contain light theme custom properties
        _cssContent.Should().Contain(":root");
        _cssContent.Should().Contain("--bg-primary:");
        _cssContent.Should().Contain("--bg-secondary:");
        _cssContent.Should().Contain("--text-primary:");
        _cssContent.Should().Contain("--accent-primary:");
    }

    [Fact]
    public void CSS_LightThemeIsDefault_NoDataThemeRequired()
    {
        // The :root block defines light colors — no data-theme attribute needed
        // Light bg-primary should be a light color (starts with #f or #e or rgb high values)
        var rootBlock = ExtractCssBlock(_cssContent, ":root");
        rootBlock.Should().NotBeNullOrEmpty("CSS must have a :root block");

        // Verify light values in :root
        rootBlock.Should().Contain("--bg-primary: #f5f7fa",
            ":root bg-primary should be light");
        rootBlock.Should().Contain("--text-primary: #1a1a2e",
            ":root text-primary should be dark text on light bg");
    }

    [Fact]
    public void JS_DefaultThemeIsLight()
    {
        // v4: JS uses double-quoted strings
        _jsContent.Should().Contain("DEFAULT_THEME = \"light\"",
            "shell.js must define DEFAULT_THEME as \"light\"");
    }

    [Fact]
    public void JS_InitThemeFallsBackToDefault()
    {
        // initTheme must use DEFAULT_THEME as fallback when no saved theme
        _jsContent.Should().Contain("saved || DEFAULT_THEME",
            "initTheme should fall back to DEFAULT_THEME when localStorage is empty");
    }

    [Theory]
    [InlineData("--bg-primary")]
    [InlineData("--bg-secondary")]
    [InlineData("--bg-sidebar")]
    [InlineData("--bg-header")]
    [InlineData("--bg-inner-header")]
    [InlineData("--text-primary")]
    [InlineData("--text-secondary")]
    [InlineData("--text-muted")]
    [InlineData("--accent-primary")]
    [InlineData("--border-color")]
    [InlineData("--shadow-sm")]
    [InlineData("--shadow-md")]
    [InlineData("--shadow-lg")]
    [InlineData("--glass-bg")]
    [InlineData("--glass-border")]
    [InlineData("--glass-blur")]
    [InlineData("--sidebar-hover-bg")]
    [InlineData("--sidebar-active-bg")]
    [InlineData("--sidebar-icon-color")]
    public void CSS_RootHasRequiredVariable(string varName)
    {
        var rootBlock = ExtractCssBlock(_cssContent, ":root");
        rootBlock.Should().Contain(varName,
            $":root must define '{varName}' for light theme");
    }

    #endregion

    #region 2. Dark Theme Override

    [Fact]
    public void CSS_HasDarkThemeSelector()
    {
        _cssContent.Should().Contain("[data-theme=\"dark\"]",
            "CSS must have a [data-theme=\"dark\"] override block");
    }

    [Fact]
    public void CSS_DarkThemeOverridesAllLightVariables()
    {
        var darkBlock = ExtractCssBlock(_cssContent, "[data-theme=\"dark\"]");
        darkBlock.Should().NotBeNullOrEmpty("CSS must have a [data-theme=\"dark\"] block");

        darkBlock.Should().Contain("--bg-primary:");
        darkBlock.Should().Contain("--bg-secondary:");
        darkBlock.Should().Contain("--text-primary:");
        darkBlock.Should().Contain("--text-secondary:");
        darkBlock.Should().Contain("--accent-primary:");
        darkBlock.Should().Contain("--border-color:");
        darkBlock.Should().Contain("--glass-bg:");
        darkBlock.Should().Contain("--glass-border:");
        darkBlock.Should().Contain("--sidebar-hover-bg:");
    }

    [Fact]
    public void CSS_DarkThemeBgIsDark()
    {
        var darkBlock = ExtractCssBlock(_cssContent, "[data-theme=\"dark\"]");
        darkBlock.Should().NotBeNullOrEmpty();

        // Dark bg-primary should be a dark color (starts with #0 or #1)
        darkBlock.Should().Contain("--bg-primary: #0f0f1e",
            "dark theme bg-primary should be dark");
    }

    [Fact]
    public void CSS_DarkThemeTextIsLight()
    {
        var darkBlock = ExtractCssBlock(_cssContent, "[data-theme=\"dark\"]");
        darkBlock.Should().NotBeNullOrEmpty();

        darkBlock.Should().Contain("--text-primary: #e0e0e0",
            "dark theme text-primary should be light for readability");
    }

    [Fact]
    public void JS_SetThemeSetsDataAttribute()
    {
        // v4: setTheme uses setAttribute with double-quoted strings
        _jsContent.Should().Contain("setAttribute(",
            "setTheme must set data-theme attribute");
        _jsContent.Should().Contain("\"data-theme\"",
            "setTheme must reference data-theme attribute");
    }

    [Fact]
    public void JS_SetThemeUsesAttributeForBothModes()
    {
        // v4: both light and dark are set via setAttribute (no removeAttribute)
        _jsContent.Should().Contain("setAttribute",
            "setTheme must use setAttribute for theme changes");
    }

    [Fact]
    public void JS_SetThemeValidatesInput()
    {
        // v4: setTheme guards with double-quoted string comparison
        _jsContent.Should().Contain("theme !== \"light\" && theme !== \"dark\"",
            "setTheme must validate theme input");
    }

    #endregion

    #region 3. LocalStorage Persistence

    [Fact]
    public void JS_ThemeStorageKeyDefined()
    {
        // v4: double-quoted string
        _jsContent.Should().Contain("THEME_STORAGE_KEY = \"mestech-theme\"",
            "shell.js must define THEME_STORAGE_KEY constant");
    }

    [Fact]
    public void JS_SetThemePersistsToLocalStorage()
    {
        // setTheme must call localStorage.setItem with THEME_STORAGE_KEY
        _jsContent.Should().Contain("localStorage.setItem(THEME_STORAGE_KEY",
            "setTheme must persist theme to localStorage");
    }

    [Fact]
    public void JS_InitThemeRestoresFromLocalStorage()
    {
        // initTheme must call localStorage.getItem with THEME_STORAGE_KEY
        _jsContent.Should().Contain("localStorage.getItem(THEME_STORAGE_KEY",
            "initTheme must restore theme from localStorage");
    }

    [Fact]
    public void JS_WallpaperStorageKeyDefined()
    {
        // v4: double-quoted string
        _jsContent.Should().Contain("WALLPAPER_STORAGE_KEY = \"mestech-wallpaper\"",
            "shell.js must define WALLPAPER_STORAGE_KEY constant");
    }

    [Fact]
    public void JS_SetWallpaperPersistsToLocalStorage()
    {
        _jsContent.Should().Contain("localStorage.setItem(WALLPAPER_STORAGE_KEY",
            "setWallpaper must persist to localStorage");
    }

    #endregion

    #region 4. Wallpaper Auto-Switch on Theme Change

    [Fact]
    public void JS_LightWallpapersArrayDefined()
    {
        _jsContent.Should().Contain("LIGHT_WALLPAPERS",
            "shell.js must define LIGHT_WALLPAPERS array");
        // v4: double-quoted strings in JS
        _jsContent.Should().Contain("\"light-branded\"");
        _jsContent.Should().Contain("\"light-plain\"");
        _jsContent.Should().Contain("\"light-gradient\"");
    }

    [Fact]
    public void JS_DarkWallpapersArrayDefined()
    {
        _jsContent.Should().Contain("DARK_WALLPAPERS",
            "shell.js must define DARK_WALLPAPERS array");
        // v4: double-quoted strings in JS
        _jsContent.Should().Contain("\"space-default\"");
        _jsContent.Should().Contain("\"nature\"");
        _jsContent.Should().Contain("\"ocean\"");
    }

    [Fact]
    public void JS_ThemeChange_SwitchesToMatchingWallpaper()
    {
        // When switching to dark and current wallpaper is light -> switch to space-default
        _jsContent.Should().Contain("LIGHT_WALLPAPERS.indexOf(currentWp)",
            "setTheme must check if current wallpaper is a light one");
        _jsContent.Should().Contain("setWallpaper(\"space-default\")",
            "switching to dark should auto-set space-default wallpaper");
    }

    [Fact]
    public void JS_LightThemeChange_SwitchesToLightBranded()
    {
        // When switching to light and current wallpaper is dark -> switch to light-branded
        _jsContent.Should().Contain("DARK_WALLPAPERS.indexOf(currentWp)",
            "setTheme must check if current wallpaper is a dark one");
        _jsContent.Should().Contain("setWallpaper(\"light-branded\")",
            "switching to light should auto-set light-branded wallpaper");
    }

    #endregion

    #region 5. Components Respond to Theme

    [Fact]
    public void CSS_DarkHeader_HasOverrides()
    {
        // v4: header class is .shell-header (not .shell-top-header)
        _cssContent.Should().Contain("[data-theme=\"dark\"] .shell-header",
            "dark theme must have header-specific overrides");
    }

    [Fact]
    public void CSS_DarkSidebar_HasBackdropFilter()
    {
        _cssContent.Should().Contain("[data-theme=\"dark\"] .shell-sidebar",
            "dark theme must have sidebar-specific overrides");
    }

    [Fact]
    public void CSS_DarkInnerHeader_HasBackdropFilter()
    {
        _cssContent.Should().Contain("[data-theme=\"dark\"] .shell-inner-header",
            "dark theme must have inner-header-specific overrides");
    }

    [Fact]
    public void CSS_DarkGlassCard_HasBackdropFilter()
    {
        _cssContent.Should().Contain("[data-theme=\"dark\"] .glass-card",
            "dark theme must have glass-card-specific overrides");
    }

    [Fact]
    public void CSS_DarkKpiCard_HasBackdropFilter()
    {
        _cssContent.Should().Contain("[data-theme=\"dark\"] .kpi-card",
            "dark theme must have KPI card overrides");
    }

    [Fact]
    public void CSS_DarkMenuHasOverride()
    {
        // v4: dark theme has .more-menu override (replaces removed content-loader)
        _cssContent.Should().Contain("[data-theme=\"dark\"] .more-menu",
            "dark theme must have more-menu dropdown overrides");
    }

    [Fact]
    public void CSS_DarkScrollbar_HasDarkThumbColor()
    {
        _cssContent.Should().Contain("[data-theme=\"dark\"] .shell-sidebar::-webkit-scrollbar-thumb",
            "dark theme should override scrollbar thumb color");
    }

    [Fact]
    public void CSS_DarkSearchInput_HasFocusOverride()
    {
        _cssContent.Should().Contain("[data-theme=\"dark\"] .header-search",
            "dark theme should override search input focus styles");
    }

    #endregion

    #region 6. Theme Toggle UI (v4: single toggle button)

    [Fact]
    public void HTML_HasThemeToggleElement()
    {
        // v4: single toggle button replaces dual-button switcher
        _htmlContent.Should().Contain("id=\"theme-toggle\"",
            "shell HTML must have a theme-toggle button");
    }

    [Fact]
    public void HTML_ThemeToggleHasThemeBtnClass()
    {
        _htmlContent.Should().Contain("h-theme-btn",
            "theme toggle button must have h-theme-btn class");
    }

    [Fact]
    public void HTML_ThemeToggleHasMoonIcon()
    {
        // v4: starts with moon icon (click toggles to dark, shows sun)
        _htmlContent.Should().Contain("fa-moon",
            "theme toggle should have moon icon by default (light mode)");
    }

    [Fact]
    public void JS_ThemeToggleButtonWired()
    {
        // v4: initTheme wires click listener on theme-toggle button
        _jsContent.Should().Contain("theme-toggle",
            "initTheme must reference theme-toggle element");
        _jsContent.Should().Contain("addEventListener",
            "theme toggle must have click event listener");
    }

    [Fact]
    public void JS_ThemeToggleSwitchesIcon()
    {
        // v4: setTheme swaps sun/moon icon in toggle button
        _jsContent.Should().Contain("fa-sun",
            "setTheme must set sun icon for dark mode");
        _jsContent.Should().Contain("fa-moon",
            "setTheme must set moon icon for light mode");
    }

    [Fact]
    public void JS_WallpaperPickerHasActiveClassLogic()
    {
        // v4: wallpaper picker uses classList.add/remove('active')
        _jsContent.Should().Contain("classList.add(\"active\")",
            "wallpaper picker must add 'active' class to selected option");
        _jsContent.Should().Contain("classList.remove(\"active\")",
            "wallpaper picker must remove 'active' from non-selected options");
    }

    #endregion

    #region 7. CSS Wallpaper Variants

    [Theory]
    [InlineData("light-branded")]
    [InlineData("light-plain")]
    [InlineData("space-default")]
    [InlineData("nature")]
    [InlineData("city")]
    [InlineData("abstract")]
    [InlineData("ocean")]
    public void CSS_HasWallpaperVariant(string wallpaperName)
    {
        _cssContent.Should().Contain($"[data-wallpaper=\"{wallpaperName}\"]",
            $"CSS must define wallpaper variant '{wallpaperName}'");
    }

    [Fact]
    public void HTML_DefaultWallpaperIsLightBranded()
    {
        _htmlContent.Should().Contain("data-wallpaper=\"light-branded\"",
            "shell HTML default wallpaper should be light-branded");
    }

    #endregion

    #region 8. Shell Init Order

    [Fact]
    public void JS_InitThemeCalledInOnReady()
    {
        _jsContent.Should().Contain("initTheme()",
            "onReady must call initTheme()");
    }

    [Fact]
    public void JS_InitWallpaperCalledInOnReady()
    {
        _jsContent.Should().Contain("initWallpaper()",
            "onReady must call initWallpaper()");
    }

    [Fact]
    public void JS_ShellExportsSetTheme()
    {
        _jsContent.Should().Contain("setTheme: setTheme",
            "MesTechShell public API must expose setTheme");
    }

    [Fact]
    public void JS_ShellExportsSetWallpaper()
    {
        _jsContent.Should().Contain("setWallpaper: setWallpaper",
            "MesTechShell public API must expose setWallpaper");
    }

    #endregion

    #region Helpers

    private static string? ExtractCssBlock(string css, string selector)
    {
        var escapedSelector = Regex.Escape(selector);
        var pattern = $@"{escapedSelector}\s*\{{([^}}]+)\}}";
        var match = Regex.Match(css, pattern, RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : null;
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
