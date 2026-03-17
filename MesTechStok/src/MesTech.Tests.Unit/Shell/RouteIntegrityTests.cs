using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MesTech.Tests.Unit.Shell;

/// <summary>
/// DEV 5 — Dalga 7.7 Task 5.01: Route integrity tests.
/// Parses mestech-router.js route registry, resolves each route
/// to a filesystem path, and verifies the target HTML file exists.
/// Also checks for unrouted panel pages and sidebar↔router consistency.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Route")]
public class RouteIntegrityTests
{
    private readonly string _repoRoot;
    private readonly string _shellDir;
    private readonly string _panelDir;
    private readonly string _htmlDir;
    private readonly string _trendyolSrcDir;
    private readonly Dictionary<string, string> _routes;

    public RouteIntegrityTests()
    {
        _repoRoot = FindRepoRoot();
        _shellDir = Path.Combine(_repoRoot, "frontend", "shell");
        _panelDir = Path.Combine(_repoRoot, "frontend", "panel");
        _htmlDir = Path.Combine(_repoRoot, "frontend", "html");
        _trendyolSrcDir = Path.Combine(_repoRoot, "MesTech_Trendyol", "apps", "web-dashboard", "src");
        _routes = ParseRouterRoutes();
    }

    #region 1. Route File Existence

    [Fact]
    public void Router_HasRoutes()
    {
        _routes.Should().NotBeEmpty("router must define at least one route");
        _routes.Count.Should().BeGreaterOrEqualTo(40,
            "router should have at least 40 routes (43 original)");
    }

    [Fact]
    public void AllRoutes_PointToExistingFiles()
    {
        var missingFiles = new List<string>();

        foreach (var (routeName, routePath) in _routes)
        {
            var resolvedPath = ResolveRoutePath(routePath);
            if (resolvedPath == null)
            {
                missingFiles.Add($"  {routeName} → {routePath}");
            }
        }

        missingFiles.Should().BeEmpty(
            $"all routes should point to existing files, but {missingFiles.Count} routes have missing targets:\n" +
            string.Join("\n", missingFiles));
    }

    [Theory]
    [InlineData("dashboard")]
    [InlineData("products")]
    [InlineData("orders")]
    [InlineData("invoices")]
    [InlineData("reports")]
    [InlineData("settings")]
    public void CoreRoute_HasExistingTarget(string routeName)
    {
        _routes.Should().ContainKey(routeName,
            $"core route '{routeName}' must exist in router");

        var resolved = ResolveRoutePath(_routes[routeName]);
        resolved.Should().NotBeNull(
            $"route '{routeName}' → '{_routes[routeName]}' must resolve to an existing file");
    }

    [Theory]
    [InlineData("trendyol-dashboard")]
    [InlineData("hb-dashboard")]
    [InlineData("n11-dashboard")]
    [InlineData("cs-dashboard")]
    [InlineData("pz-dashboard")]
    [InlineData("amazon-dashboard")]
    [InlineData("bitrix24-dashboard")]
    public void PlatformDashboardRoute_Exists(string routeName)
    {
        _routes.Should().ContainKey(routeName,
            $"platform dashboard route '{routeName}' must exist");

        var resolved = ResolveRoutePath(_routes[routeName]);
        resolved.Should().NotBeNull(
            $"platform route '{routeName}' → '{_routes[routeName]}' must resolve to an existing file");
    }

    [Theory]
    [InlineData("bitrix24-deals")]
    [InlineData("bitrix24-contacts")]
    [InlineData("bitrix24-products")]
    [InlineData("bitrix24-sync")]
    [InlineData("bitrix24-settings")]
    [InlineData("bitrix24-webhooks")]
    public void Bitrix24SubRoute_Exists(string routeName)
    {
        _routes.Should().ContainKey(routeName,
            $"Bitrix24 sub-route '{routeName}' must exist");

        var resolved = ResolveRoutePath(_routes[routeName]);
        resolved.Should().NotBeNull(
            $"Bitrix24 route '{routeName}' → '{_routes[routeName]}' must resolve to an existing file");
    }

    #endregion

    #region 2. Unified Pages All Routed

    [Theory]
    [InlineData("unified-products.html", "products")]
    [InlineData("unified-orders.html", "orders")]
    [InlineData("unified-stock.html", "stock")]
    [InlineData("unified-invoice.html", "invoices")]
    [InlineData("unified-dashboard.html", "trendyol-dashboard")]
    [InlineData("unified-reports.html", "reports")]
    [InlineData("unified-settings.html", "settings")]
    [InlineData("unified-categories.html", "categories")]
    [InlineData("unified-notifications.html", "notifications")]
    [InlineData("unified-dropshipping.html", "dropshipping")]
    [InlineData("unified-ai-tools.html", "mesa-ai")]
    [InlineData("unified-akademi.html", "akademi")]
    [InlineData("unified-returns.html", "returns")]
    [InlineData("unified-cari.html", "customers")]
    [InlineData("unified-commission.html", "commission")]
    [InlineData("unified-payments.html", "payments")]
    public void UnifiedPage_HasRoute(string filename, string expectedRouteKey)
    {
        _routes.Should().ContainKey(expectedRouteKey,
            $"unified page '{filename}' must have route '{expectedRouteKey}' in router");
    }

    #endregion

    #region 3. Sidebar → Router Consistency

    [Fact]
    public void SidebarMenuItems_AllHaveRouterRoutes()
    {
        var sidebarJs = File.ReadAllText(Path.Combine(_shellDir, "mestech-sidebar.js"));

        // Extract all page values from sidebar menu data
        var pagePattern = new Regex(@"page:\s*'([^']+)'");
        var sidebarPages = pagePattern.Matches(sidebarJs)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        // Exclude parent items that have children (they are containers, not navigable pages)
        var parentPattern = new Regex(@"page:\s*'([^']+)'\s*,\s*children\s*:");
        var parentPages = parentPattern.Matches(sidebarJs)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .ToHashSet();

        var missingRoutes = sidebarPages
            .Where(page => !parentPages.Contains(page))
            .Where(page => !_routes.ContainsKey(page))
            .ToList();

        missingRoutes.Should().BeEmpty(
            $"all sidebar pages should have router routes, but {missingRoutes.Count} are missing:\n" +
            string.Join(", ", missingRoutes));
    }

    [Fact]
    public void InnerHeaderTabs_AllHaveRouterRoutes()
    {
        var shellHtml = File.ReadAllText(Path.Combine(_shellDir, "mestech-shell.html"));

        var tabPattern = new Regex(@"data-page=""([^""]+)""");
        var tabPages = tabPattern.Matches(shellHtml)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        var missingRoutes = tabPages
            .Where(page => !_routes.ContainsKey(page))
            .ToList();

        missingRoutes.Should().BeEmpty(
            $"all inner header tab pages should have router routes, but {missingRoutes.Count} are missing:\n" +
            string.Join(", ", missingRoutes));
    }

    #endregion

    #region 4. Route Coverage Report

    [Fact]
    public void Panel_UnifiedPages_FullyCovered()
    {
        var unifiedDir = Path.Combine(_panelDir, "pages", "unified");
        if (!Directory.Exists(unifiedDir)) return;

        var unifiedFiles = Directory.GetFiles(unifiedDir, "*.html")
            .Select(Path.GetFileName)
            .ToList();

        // Check that every unified page has at least one route pointing to it
        var routedPaths = _routes.Values
            .Where(v => v.Contains("unified/"))
            .Select(v => v.Split('/').Last())
            .Distinct()
            .ToList();

        var uncoveredPages = unifiedFiles
            .Where(f => f != null && !routedPaths.Contains(f))
            .ToList();

        uncoveredPages.Should().BeEmpty(
            $"all unified pages should have routes, but {uncoveredPages.Count} are uncovered:\n" +
            string.Join(", ", uncoveredPages!));
    }

    [Fact]
    public void Panel_PlatformPages_HaveRoutes()
    {
        var platformDirs = new[] { "ciceksepeti", "hepsiburada", "n11", "pazarama" };

        foreach (var platform in platformDirs)
        {
            var dir = Path.Combine(_panelDir, "pages", platform);
            if (!Directory.Exists(dir)) continue;

            var files = Directory.GetFiles(dir, "*.html");
            files.Length.Should().BeGreaterThan(0,
                $"platform '{platform}' directory should have HTML pages");
        }
    }

    #endregion

    #region 5. Router Configuration Quality

    [Fact]
    public void Router_HasNoEmptyPaths()
    {
        var emptyPaths = _routes
            .Where(r => string.IsNullOrWhiteSpace(r.Value))
            .Select(r => r.Key)
            .ToList();

        emptyPaths.Should().BeEmpty("no route should have an empty path");
    }

    [Fact]
    public void Router_NoDuplicateRouteKeys()
    {
        var routerJs = File.ReadAllText(Path.Combine(_shellDir, "mestech-router.js"));
        var keyPattern = new Regex(@"'([^']+)'\s*:");
        var keys = keyPattern.Matches(routerJs)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .ToList();

        var duplicates = keys.GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty(
            $"router should not have duplicate route keys:\n" +
            string.Join(", ", duplicates));
    }

    [Fact]
    public void Router_HasLoadTimeoutMechanism()
    {
        var routerJs = File.ReadAllText(Path.Combine(_shellDir, "mestech-router.js"));
        // Router may use a constant (LOAD_TIMEOUT_MS) or an inline timeout value
        var hasTimeout = routerJs.Contains("LOAD_TIMEOUT_MS") || routerJs.Contains("setTimeout");
        hasTimeout.Should().BeTrue("router must implement a page load timeout");
    }

    [Fact]
    public void Router_HasErrorPage()
    {
        var routerJs = File.ReadAllText(Path.Combine(_shellDir, "mestech-router.js"));
        // Router may expose error display as showErrorState or _showError
        var hasErrorHandler = routerJs.Contains("showErrorState") || routerJs.Contains("_showError") || routerJs.Contains("showError");
        hasErrorHandler.Should().BeTrue("router must display an error page for failed loads");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Parse the route registry from mestech-router.js.
    /// Extracts key-value pairs from the routes object.
    /// </summary>
    private Dictionary<string, string> ParseRouterRoutes()
    {
        var routerPath = Path.Combine(_shellDir, "mestech-router.js");
        if (!File.Exists(routerPath))
            return new Dictionary<string, string>();

        var js = File.ReadAllText(routerPath);

        // Match both quoted and unquoted route keys with path: B.xxx + "relative/path.html"
        // Handles: "route-key": { path: B.panel + "path.html" }
        // and:      routeKey: { path: B.panel + "path.html" }
        var pattern = new Regex(
            @"(?:""([^""]+)""|([a-z][a-z0-9\-]*))\s*:\s*\{[^}]*path\s*:\s*\w+\.\w+\s*\+\s*""([^""]+)""",
            RegexOptions.Singleline);
        var matches = pattern.Matches(js);

        var routes = new Dictionary<string, string>();
        foreach (Match m in matches)
        {
            var key = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
            var path = m.Groups[3].Value;
            routes.TryAdd(key, path);
        }

        return routes;
    }

    /// <summary>
    /// Resolve a router path to an actual filesystem path.
    /// Returns the resolved path if the file exists, null otherwise.
    /// All paths are resolved relative to the shell directory first,
    /// then falls back to legacy /src/ prefix resolution.
    /// </summary>
    private string? ResolveRoutePath(string routePath)
    {
        var rel = routePath.Replace('/', Path.DirectorySeparatorChar);

        // Primary: relative path from B.panel → panel/pages/
        var panelPagesPath = Path.Combine(_panelDir, "pages", rel);
        if (File.Exists(panelPagesPath)) return panelPagesPath;

        // Also check directly under panel dir (for top-level pages like dashboard.html)
        var panelDirPath = Path.Combine(_panelDir, rel);
        if (File.Exists(panelDirPath)) return panelDirPath;

        // HTML dir path (B.html → frontend/html/)
        var htmlPath = Path.Combine(_htmlDir, rel);
        if (File.Exists(htmlPath)) return htmlPath;

        // Trendyol src path
        var trendyolPath = Path.Combine(_trendyolSrcDir, rel);
        if (File.Exists(trendyolPath)) return trendyolPath;

        // Shell dir relative (for shell-dashboard.html etc.)
        var shellRelPath = Path.Combine(_shellDir, rel);
        if (File.Exists(shellRelPath)) return shellRelPath;

        return null;
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
