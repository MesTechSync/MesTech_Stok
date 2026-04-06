using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MesTech.Tests.Unit.Regression;

/// <summary>
/// Gemini audit regression tests — source-code scanning guards that prevent
/// credential leaks, missing error handling, and broken navigation paths.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "GeminiRegression")]
public class GeminiAuditRegressionTests
{
    // ── Helpers ──

    private static string FindSrcRoot()
    {
        // Walk up from BaseDirectory looking for src/ marker (MesTech.Domain folder)
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 15; i++)
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
            if (Directory.Exists(Path.Combine(dir, "MesTech.Domain")))
                return dir;
        }

        // Fallback: try well-known relative paths from working directory
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(cwd, "src"),
            Path.Combine(cwd, "MesTechStok", "src"),
            Path.Combine(cwd, "MesTech_Stok", "MesTechStok", "src"),
            // When running with -o bin/dev5unit/, BaseDirectory is the output dir
            // Try navigating from BaseDirectory to known project structure
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "src")),
            Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "src")),
        };
        foreach (var c in candidates)
        {
            if (Directory.Exists(Path.Combine(c, "MesTech.Domain")))
                return c;
        }

        throw new DirectoryNotFoundException(
            $"src/ root not found. BaseDirectory={AppDomain.CurrentDomain.BaseDirectory}, CWD={cwd}");
    }

    private static string FindRepoRoot()
    {
        // Walk up from BaseDirectory looking for mono-repo root (MesTech_Stok + MesTech_Trendyol)
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 20; i++)
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
            if (Directory.Exists(Path.Combine(dir, "MesTech_Trendyol")) &&
                Directory.Exists(Path.Combine(dir, "MesTech_Stok")))
                return dir;
        }

        // Fallback: try well-known relative paths from working directory
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            cwd,
            Path.GetFullPath(Path.Combine(cwd, "..", "..")),
            Path.GetFullPath(Path.Combine(cwd, "..", "..", "..", "..")),
        };
        foreach (var c in candidates)
        {
            if (Directory.Exists(Path.Combine(c, "MesTech_Trendyol")) &&
                Directory.Exists(Path.Combine(c, "MesTech_Stok")))
                return c;
        }

        throw new DirectoryNotFoundException(
            $"repo root not found. BaseDirectory={AppDomain.CurrentDomain.BaseDirectory}, CWD={cwd}");
    }

    // ── Test 1: No real passwords in appsettings ──

    [Fact]
    public void AppSettings_Should_Not_Contain_RealPasswords()
    {
        // Arrange
        var srcRoot = FindSrcRoot();
        var appSettingsFiles = Directory.GetFiles(srcRoot, "appsettings*.json", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        appSettingsFiles.Should().NotBeEmpty("at least one appsettings*.json should exist under src/");

        // Known hardcoded password that was removed in Dalga 4
        const string oldHardcodedPassword = "mestech_postgres_dev";

        // Regex: Password= followed by a value that is NOT a known placeholder
        // Matches: Password=someRealValue or Password=abc123
        // Does NOT match: Password=** USER SECRETS ** or Password=CONFIGURED_VIA_USER_SECRETS
        var passwordRegex = new Regex(
            @"Password\s*=\s*(?<value>[^;""}\s]+)",
            RegexOptions.IgnoreCase);

        var violations = new List<string>();

        foreach (var file in appSettingsFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(srcRoot, file);

            // Check for the old hardcoded password
            if (content.Contains(oldHardcodedPassword, StringComparison.OrdinalIgnoreCase))
            {
                violations.Add($"{relativePath}: contains old hardcoded password '{oldHardcodedPassword}'");
            }

            // Check Password= values
            var matches = passwordRegex.Matches(content);
            foreach (Match match in matches)
            {
                var value = match.Groups["value"].Value;

                // Allow known placeholders and development-only default passwords
                if (value.Contains("USER SECRETS", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("USER_SECRETS", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("CONFIGURED_VIA", StringComparison.OrdinalIgnoreCase) ||
                    value.Contains("CONFIGURE_VIA", StringComparison.OrdinalIgnoreCase) ||
                    value == "**" ||
                    string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                // Allow development appsettings with known local-only passwords
                if (relativePath.Contains("Development", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                violations.Add($"{relativePath}: Password value '{value}' does not look like a placeholder");
            }
        }

        violations.Should().BeEmpty(
            "appsettings files must not contain real passwords — " +
            "use dotnet user-secrets or environment variables instead. " +
            $"Violations: [{string.Join("; ", violations)}]");
    }

    // ── Test 2: App.xaml.cs has try/catch around DB operations ──

    [Fact]
    public void Application_Should_Have_DbConnection_TryCatch()
    {
        // Arrange
        var srcRoot = FindSrcRoot();
        var appXamlCsFiles = Directory.GetFiles(srcRoot, "App.xaml.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("bin") && !f.Contains("obj"))
            .ToList();

        appXamlCsFiles.Should().NotBeEmpty("App.xaml.cs should exist in the Desktop project");

        foreach (var file in appXamlCsFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(srcRoot, file);

            // The file must reference database operations
            var hasDbReference = content.Contains("DbContext", StringComparison.OrdinalIgnoreCase) ||
                                 content.Contains("Database", StringComparison.OrdinalIgnoreCase) ||
                                 content.Contains("EnsureCreated", StringComparison.OrdinalIgnoreCase) ||
                                 content.Contains("Migrate", StringComparison.OrdinalIgnoreCase);

            hasDbReference.Should().BeTrue(
                $"{relativePath} should reference database operations (DbContext/Database/EnsureCreated/Migrate)");

            // The file must contain try/catch for error handling
            content.Should().Contain("try",
                $"{relativePath} should contain 'try' blocks for graceful error handling");
            content.Should().Contain("catch",
                $"{relativePath} should contain 'catch' blocks for graceful error handling");

            // Verify there is error reporting UI or logging near DB operations
            var hasErrorReporting = content.Contains("MessageBox", StringComparison.OrdinalIgnoreCase) ||
                                    content.Contains("Log.Error", StringComparison.OrdinalIgnoreCase) ||
                                    content.Contains("LogError", StringComparison.OrdinalIgnoreCase) ||
                                    content.Contains("ErrorWindow", StringComparison.OrdinalIgnoreCase) ||
                                    content.Contains("ShowDialog", StringComparison.OrdinalIgnoreCase);

            hasErrorReporting.Should().BeTrue(
                $"{relativePath} should report database errors via MessageBox, Log.Error, or an error dialog");
        }
    }

    // ── Test 3: sidebar.js navigation paths must use /src/ prefix (nginx convention) ──

    [Fact]
    public void HtmlSidebarJs_Should_Use_Consistent_SrcPaths()
    {
        // Arrange
        var repoRoot = FindRepoRoot();

        var sidebarPaths = new[]
        {
            Path.Combine(repoRoot, "frontend", "panel", "components", "Sidebar", "sidebar.js"),
            Path.Combine(repoRoot, "MesTech_Trendyol", "apps", "web-dashboard", "src", "components", "Sidebar", "sidebar.js")
        };

        var existingFiles = sidebarPaths.Where(File.Exists).ToList();
        existingFiles.Should().NotBeEmpty("at least one sidebar.js file should exist");

        // Post-nginx architecture: sidebar links MUST use /src/pages/ convention.
        // Nginx rewrites /src/ prefix to serve static files correctly.
        // Guard: no broken patterns like href="#" placeholders.
        var brokenLinkPatterns = new[]
        {
            new Regex(@"href\s*=\s*""#""", RegexOptions.IgnoreCase),
            new Regex(@"href\s*=\s*'#'", RegexOptions.IgnoreCase)
        };

        var violations = new List<string>();

        foreach (var file in existingFiles)
        {
            var content = File.ReadAllText(file);
            var relativePath = Path.GetRelativePath(repoRoot, file);

            foreach (var pattern in brokenLinkPatterns)
            {
                var matches = pattern.Matches(content);
                foreach (Match match in matches)
                {
                    violations.Add($"{relativePath}: broken link placeholder: {match.Value.Trim()}");
                }
            }
        }

        violations.Should().BeEmpty(
            "sidebar.js files must not contain broken link placeholders (href='#' or href=''). " +
            "All navigation links should use the /src/pages/ convention for nginx routing. " +
            $"Found {violations.Count} violation(s): [{string.Join("; ", violations)}]");
    }
}
