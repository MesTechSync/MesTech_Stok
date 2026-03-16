namespace MesTech.Tests.Integration.E2E.Blazor;

/// <summary>
/// Configuration and helper utilities for Blazor Playwright E2E tests.
/// Dalga 12 — infrastructure scaffold for future Playwright activation.
/// </summary>
public static class BlazorTestConfiguration
{
    // ══════════════════════════════════════════════════════════════════════════
    // Base URL — Blazor app launch settings (launchSettings.json)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Base URL for the Blazor SaaS application.
    /// Matches Properties/launchSettings.json: http://localhost:3200
    /// </summary>
    public const string BaseUrl = "http://localhost:3200";

    /// <summary>
    /// Default timeout for page navigation in milliseconds.
    /// </summary>
    public const int DefaultNavigationTimeoutMs = 30_000;

    /// <summary>
    /// Default timeout for element assertions in milliseconds.
    /// </summary>
    public const int DefaultAssertionTimeoutMs = 5_000;

    // ══════════════════════════════════════════════════════════════════════════
    // Known Blazor Routes (from actual @page directives)
    // ══════════════════════════════════════════════════════════════════════════

    public static class Routes
    {
        public const string Home = "/";
        public const string Dashboard = "/dashboard";
        public const string Login = "/login";

        // CRM
        public const string CrmLeads = "/crm/leads";
        public const string CrmDeals = "/crm/deals";
        public const string CrmContacts = "/crm/contacts";

        // Satis
        public const string Orders = "/siparisler";
        public const string Marketplaces = "/pazaryerleri";
        public const string Stock = "/stok";

        // Finans
        public const string ProfitLoss = "/finans/kar-zarar";
        public const string Expenses = "/finans/giderler";
        public const string BankAccounts = "/finans/banka";

        // IK
        public const string Employees = "/hr/calisanlar";
        public const string LeaveRequests = "/hr/izinler";

        // Diger
        public const string Documents = "/belgeler";
        public const string Reports = "/raporlar";
        public const string Settings = "/ayarlar";
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CI Skip Helper
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns true if the Blazor E2E tests should be skipped.
    /// Checks BLAZOR_E2E_SKIP and CI environment variables.
    /// In CI, tests are skipped unless BLAZOR_E2E_ENABLED=true is set.
    /// </summary>
    public static bool ShouldSkip()
    {
        // Explicit skip override
        if (Environment.GetEnvironmentVariable("BLAZOR_E2E_SKIP") == "true")
            return true;

        // In CI environments, skip unless explicitly enabled
        var isCi = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"))
                || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TF_BUILD"));

        if (isCi)
        {
            return Environment.GetEnvironmentVariable("BLAZOR_E2E_ENABLED") != "true";
        }

        return false;
    }

    /// <summary>
    /// Returns the skip reason string for xUnit, or null if tests should run.
    /// Usage: [SkippableFact] with Skip.If(BlazorTestConfiguration.GetSkipReason() != null)
    /// </summary>
    public static string? GetSkipReason()
    {
        if (ShouldSkip())
            return "Blazor E2E tests skipped: set BLAZOR_E2E_ENABLED=true and ensure Blazor is running on :5200";

        return null;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Playwright Browser Setup (future activation)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Placeholder for Playwright browser setup.
    /// When Microsoft.Playwright NuGet is added:
    /// <code>
    /// public static async Task&lt;IBrowser&gt; LaunchBrowserAsync(bool headless = true)
    /// {
    ///     var playwright = await Playwright.CreateAsync();
    ///     return await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
    ///     {
    ///         Headless = headless
    ///     });
    /// }
    ///
    /// public static async Task&lt;IPage&gt; CreatePageAsync(IBrowser browser, int width = 1920, int height = 1080)
    /// {
    ///     var context = await browser.NewContextAsync(new BrowserNewContextOptions
    ///     {
    ///         ViewportSize = new ViewportSize { Width = width, Height = height }
    ///     });
    ///     return await context.NewPageAsync();
    /// }
    /// </code>
    /// </summary>
    public static void EnsurePlaywrightInstalled()
    {
        // TODO: When Playwright NuGet is added, call:
        // Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Viewport Presets
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Mobile viewport: iPhone SE (375x812)</summary>
    public static (int Width, int Height) ViewportMobile => (375, 812);

    /// <summary>Tablet viewport: iPad (768x1024)</summary>
    public static (int Width, int Height) ViewportTablet => (768, 1024);

    /// <summary>Desktop viewport: Full HD (1920x1080)</summary>
    public static (int Width, int Height) ViewportDesktop => (1920, 1080);
}
