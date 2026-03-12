using System;
using System.IO;
using System.Linq;
using FluentAssertions;

namespace MesTech.Tests.Integration.Regression;

/// <summary>
/// DEV 5 — Dalga 7 Task 5.P1-02: Domain name configuration regression guard.
/// Ensures panel domain is mestech.tr (not dropmes.tr) across all non-OpenCart files.
/// </summary>
[Trait("Category", "Regression")]
[Trait("Category", "DomainConfig")]
public class DomainConfigurationTests
{
    private static readonly string[] ScanExtensions =
    {
        ".html", ".js", ".ts", ".json", ".css",
        ".cs", ".yml", ".yaml", ".md", ".env",
        ".example", ".toml", ".bat", ".sh"
    };

    private static readonly string[] ExcludedPaths =
    {
        "node_modules",
        "bin",
        "obj",
        "DomainConfigurationTests.cs",
        "BrandingConsistencyTests.cs"
    };

    [Fact]
    public void PanelDomain_ShouldBe_MesTechTr_NotDropmesTr()
    {
        var repoRoot = FindRepoRoot();

        var dirsToScan = new[]
        {
            Path.Combine(repoRoot, "frontend", "panel"),
            Path.Combine(repoRoot, "Docs"),
            Path.Combine(repoRoot, "MesTech_Stok")
        }.Where(Directory.Exists).ToArray();

        var violations = dirsToScan
            .SelectMany(d => Directory.GetFiles(d, "*.*", SearchOption.AllDirectories))
            .Where(IsCodeFile)
            .Where(f => !IsExcluded(f))
            .Where(f => File.ReadAllText(f).Contains("dropmes.tr", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetRelativePath(repoRoot, f))
            .ToList();

        violations.Should().BeEmpty(
            "panel domain must be mestech.tr, not dropmes.tr (OpenCart files are excluded from this check)");
    }

    [Fact]
    public void ReadmeFiles_ShouldNotReference_DropmesTr()
    {
        var repoRoot = FindRepoRoot();

        var readmeFiles = Directory.GetFiles(repoRoot, "README*.md")
            .Concat(Directory.GetFiles(repoRoot, "README*.md", SearchOption.TopDirectoryOnly))
            .Distinct()
            .ToArray();

        var violations = readmeFiles
            .Where(f => File.ReadAllText(f).Contains("dropmes.tr", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetFileName(f))
            .ToList();

        violations.Should().BeEmpty(
            "README files must reference mestech.tr, not dropmes.tr");
    }

    [Fact]
    public void UnifiedDataService_ProductionUrl_ShouldUse_MesTechTr()
    {
        var repoRoot = FindRepoRoot();
        var udsPath = Path.Combine(repoRoot, "frontend", "panel", "components",
            "UnifiedDataService", "unified-data-service.js");

        if (!File.Exists(udsPath))
            return;

        var content = File.ReadAllText(udsPath);
        content.Should().NotContain("dropmes.tr",
            "unified-data-service.js production URL must use mestech.tr domain");
        content.Should().Contain("mestech.tr",
            "unified-data-service.js should reference mestech.tr production domain");
    }

    private static bool IsCodeFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ScanExtensions.Contains(ext);
    }

    private static bool IsExcluded(string path)
    {
        return ExcludedPaths.Any(e => path.Contains(e, StringComparison.OrdinalIgnoreCase));
    }

    private static string FindRepoRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 15; i++)
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
            if (Directory.Exists(Path.Combine(dir, "MesTech_Trendyol")) ||
                Directory.Exists(Path.Combine(dir, "MesTech_Stok")))
                return dir;
        }
        throw new DirectoryNotFoundException("repo root not found");
    }
}
