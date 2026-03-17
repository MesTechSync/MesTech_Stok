using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MesTech.Tests.Unit.Shell;

/// <summary>
/// DEV 5 — Dalga 7.7.1 Task 5.01: Route file quality tests.
/// Verifies that every route target file:
/// 1. Exists on disk
/// 2. Is not empty (>100 bytes)
/// 3. Contains valid HTML markers (&lt;!DOCTYPE or &lt;html)
/// Also verifies distinct file count and route-to-file ratio.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Route")]
public class RouteFileQualityTests
{
    private readonly string _repoRoot;
    private readonly string _shellDir;
    private readonly string _panelDir;
    private readonly string _htmlDir;
    private readonly string _trendyolSrcDir;
    private readonly Dictionary<string, string> _routes;
    private readonly Dictionary<string, string> _resolvedFiles;

    public RouteFileQualityTests()
    {
        _repoRoot = FindRepoRoot();
        _shellDir = Path.Combine(_repoRoot, "frontend", "shell");
        _panelDir = Path.Combine(_repoRoot, "frontend", "panel");
        _htmlDir = Path.Combine(_repoRoot, "frontend", "html");
        _trendyolSrcDir = Path.Combine(_repoRoot, "MesTech_Trendyol", "apps", "web-dashboard", "src");
        _routes = ParseRouterRoutes();
        _resolvedFiles = ResolveAllRoutes();
    }

    #region 1. File Existence

    [Fact]
    public void AllRoutes_ResolveToExistingFiles()
    {
        var unresolved = _routes
            .Where(r => !_resolvedFiles.ContainsKey(r.Key))
            .Select(r => $"  {r.Key} → {r.Value}")
            .ToList();

        unresolved.Should().BeEmpty(
            $"all {_routes.Count} routes must resolve to existing files, but {unresolved.Count} are missing:\n" +
            string.Join("\n", unresolved));
    }

    [Fact]
    public void RouteCount_IsAtLeast200()
    {
        _routes.Count.Should().BeGreaterOrEqualTo(200,
            "router should have at least 200 routes (232 in v2.0)");
    }

    [Fact]
    public void DistinctFileCount_IsAtLeast80()
    {
        var distinctFiles = _resolvedFiles.Values
            .Select(p => Path.GetFullPath(p).ToLowerInvariant())
            .Distinct()
            .Count();

        distinctFiles.Should().BeGreaterOrEqualTo(80,
            "router should map to at least 80 distinct HTML files");
    }

    #endregion

    #region 2. File Size (>100 bytes)

    [Fact]
    public void AllResolvedFiles_AreNotEmpty()
    {
        var tooSmall = new List<string>();

        foreach (var (routeName, filePath) in _resolvedFiles)
        {
            var info = new FileInfo(filePath);
            if (info.Length <= 100)
            {
                tooSmall.Add($"  {routeName} → {filePath} ({info.Length} bytes)");
            }
        }

        tooSmall.Should().BeEmpty(
            $"all route files should be >100 bytes, but {tooSmall.Count} are too small:\n" +
            string.Join("\n", tooSmall));
    }

    [Theory]
    [InlineData("dashboard")]
    [InlineData("products")]
    [InlineData("orders")]
    [InlineData("trendyol-dashboard")]
    [InlineData("hb-dashboard")]
    [InlineData("n11-dashboard")]
    [InlineData("cs-dashboard")]
    [InlineData("bitrix24-dashboard")]
    [InlineData("settings")]
    [InlineData("reports")]
    public void CoreRoute_FileIsSubstantial(string routeName)
    {
        _resolvedFiles.Should().ContainKey(routeName,
            $"core route '{routeName}' must resolve");

        var info = new FileInfo(_resolvedFiles[routeName]);
        info.Length.Should().BeGreaterThan(1000,
            $"core route '{routeName}' file should be >1KB (actual: {info.Length} bytes)");
    }

    #endregion

    #region 3. HTML Validity

    [Fact]
    public void AllResolvedFiles_AreValidHtml()
    {
        var invalidHtml = new List<string>();

        // Deduplicate by resolved path to avoid testing same file multiple times
        var distinctFiles = _resolvedFiles
            .GroupBy(r => Path.GetFullPath(r.Value).ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        foreach (var (routeName, filePath) in distinctFiles)
        {
            var content = File.ReadAllText(filePath);
            var hasDoctype = content.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase);
            var hasHtmlTag = content.Contains("<html", StringComparison.OrdinalIgnoreCase);

            if (!hasDoctype && !hasHtmlTag)
            {
                invalidHtml.Add($"  {routeName} → {Path.GetFileName(filePath)}");
            }
        }

        invalidHtml.Should().BeEmpty(
            $"all route files should contain <!DOCTYPE or <html>, but {invalidHtml.Count} are invalid:\n" +
            string.Join("\n", invalidHtml));
    }

    [Fact]
    public void AllResolvedFiles_HaveHtmlExtension()
    {
        var nonHtml = _resolvedFiles
            .Where(r => !r.Value.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
            .Select(r => $"  {r.Key} → {r.Value}")
            .ToList();

        nonHtml.Should().BeEmpty("all route targets should be .html files");
    }

    [Theory]
    [InlineData("dashboard", "shell-dashboard.html")]
    [InlineData("products", "unified-products.html")]
    [InlineData("orders", "unified-orders.html")]
    [InlineData("invoices", "unified-invoice.html")]
    [InlineData("reports", "unified-reports.html")]
    [InlineData("settings", "unified-settings.html")]
    [InlineData("stock", "unified-stock.html")]
    [InlineData("categories", "unified-categories.html")]
    public void CoreRoute_PathEndsWithExpectedFile(string routeName, string expectedFilename)
    {
        _routes.Should().ContainKey(routeName);
        _routes[routeName].Should().EndWith(expectedFilename,
            $"core route '{routeName}' should point to a path ending with '{expectedFilename}'");
    }

    #endregion

    #region 4. Platform Route Coverage

    [Theory]
    [InlineData("hepsiburada", "hb-dashboard", "hb-products", "hb-orders", "hb-stock", "hb-categories", "hb-returns", "hb-settings")]
    [InlineData("n11", "n11-dashboard", "n11-products", "n11-orders", "n11-stock", "n11-categories", "n11-pricing", "n11-shipping")]
    [InlineData("ciceksepeti", "cs-dashboard", "cs-products", "cs-orders", "cs-stock", "cs-categories", "cs-returns", "cs-settings")]
    [InlineData("pazarama", "pz-dashboard", "pz-products", "pz-orders", "pz-stock", "pz-categories", "pz-returns", "pz-settings")]
    [InlineData("bitrix24", "bitrix24-dashboard", "bitrix24-deals", "bitrix24-contacts", "bitrix24-products", "bitrix24-sync", "bitrix24-settings", "bitrix24-webhooks")]
    public void Platform_AllSubRoutesExistAndResolve(string platform, params string[] routeNames)
    {
        foreach (var routeName in routeNames)
        {
            _routes.Should().ContainKey(routeName,
                $"platform '{platform}' must have route '{routeName}'");
            _resolvedFiles.Should().ContainKey(routeName,
                $"platform route '{routeName}' must resolve to an existing file");
        }
    }

    #endregion

    #region 5. Alias Consistency

    [Theory]
    [InlineData("products", "unified-products", "urunler")]
    [InlineData("orders", "unified-orders", "siparisler")]
    [InlineData("stock", "unified-stock", "stok")]
    [InlineData("invoices", "unified-invoice", "faturalar")]
    [InlineData("reports", "unified-reports", "raporlar")]
    [InlineData("settings", "unified-settings", "ayarlar")]
    [InlineData("categories", "unified-categories", "kategoriler")]
    [InlineData("notifications", "unified-notifications", "bildirimler")]
    [InlineData("customers", "unified-cari", "musteriler")]
    public void Aliases_PointToSameFile(string primary, params string[] aliases)
    {
        _routes.Should().ContainKey(primary);
        var primaryPath = _routes[primary];

        foreach (var alias in aliases)
        {
            _routes.Should().ContainKey(alias,
                $"alias '{alias}' for '{primary}' must exist");
            _routes[alias].Should().Be(primaryPath,
                $"alias '{alias}' must point to same path as '{primary}'");
        }
    }

    #endregion

    #region 6. Route-to-File Ratio

    [Fact]
    public void RouteToFileRatio_IsReasonable()
    {
        var distinctFiles = _resolvedFiles.Values
            .Select(p => Path.GetFullPath(p).ToLowerInvariant())
            .Distinct()
            .Count();

        var ratio = (double)_routes.Count / distinctFiles;

        // ~232 routes / ~109 files ≈ 2.1 ratio (due to aliases)
        ratio.Should().BeGreaterThan(1.5,
            "route-to-file ratio indicates aliases are being used");
        ratio.Should().BeLessThan(5.0,
            "ratio shouldn't be too high — routes should map to diverse files");
    }

    #endregion

    #region Helpers

    private Dictionary<string, string> ParseRouterRoutes()
    {
        var routerPath = Path.Combine(_shellDir, "mestech-router.js");
        if (!File.Exists(routerPath))
            return new Dictionary<string, string>();

        var js = File.ReadAllText(routerPath);
        // Match both quoted and unquoted route keys with path: B.xxx + "relative/path.html"
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

    private Dictionary<string, string> ResolveAllRoutes()
    {
        var resolved = new Dictionary<string, string>();
        foreach (var (name, path) in _routes)
        {
            var filePath = ResolveRoutePath(path);
            if (filePath != null)
                resolved[name] = filePath;
        }
        return resolved;
    }

    private string? ResolveRoutePath(string routePath)
    {
        var rel = routePath.Replace('/', Path.DirectorySeparatorChar);

        // Primary: relative path from B.panel → panel/pages/
        var panelPagesPath = Path.Combine(_panelDir, "pages", rel);
        if (File.Exists(panelPagesPath)) return panelPagesPath;

        // Top-level panel dir (for dashboard.html etc.)
        var panelDirPath = Path.Combine(_panelDir, rel);
        if (File.Exists(panelDirPath)) return panelDirPath;

        // HTML dir path (B.html → frontend/html/)
        var htmlPath = Path.Combine(_htmlDir, rel);
        if (File.Exists(htmlPath)) return htmlPath;

        // Shell dir (for B.shell paths like pages/shell-dashboard.html)
        var shellRelPath = Path.Combine(_shellDir, rel);
        if (File.Exists(shellRelPath)) return shellRelPath;

        // Trendyol src dir
        var trendyolPath = Path.Combine(_trendyolSrcDir, rel);
        if (File.Exists(trendyolPath)) return trendyolPath;

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
