using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MesTech.Tests.Integration.Regression;

/// <summary>
/// DEV 5 — Dalga 7 Task 5.P1-01: GMO Plus branding elimination regression guard.
/// Ensures no GMOPlus / @gmoplus references exist in runtime code.
/// file_inventory.csv is excluded (historical migration data).
/// </summary>
[Trait("Category", "Regression")]
[Trait("Category", "BrandingCleanup")]
public class GmoReferenceEliminationTests
{
    private static readonly Regex GmoPattern = new(
        @"gmoplus|gmo\s*plus|@gmoplus",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] ExcludedFiles =
    {
        "file_inventory.csv",
        "file_inventory.md",
        "GmoReferenceEliminationTests.cs",
        "BrandingConsistencyTests.cs"
    };

    private static readonly string[] ScanExtensions =
    {
        ".html", ".js", ".ts", ".json", ".css",
        ".cs", ".csproj", ".sln", ".yml", ".yaml",
        ".md", ".bat", ".sh", ".toml", ".prisma",
        ".sql", ".svg", ".env", ".example"
    };

    [Fact]
    public void DotNetSource_ShouldNotContain_GmoReferences()
    {
        var srcRoot = FindSrcRoot();
        var files = Directory.GetFiles(srcRoot, "*.*", SearchOption.AllDirectories)
            .Where(IsCodeFile)
            .Where(f => !IsExcluded(f))
            .ToArray();

        var violations = files
            .Where(f => GmoPattern.IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(srcRoot, f))
            .ToList();

        violations.Should().BeEmpty(
            "all GMO Plus references must be replaced with MesTech/MesChain in .NET source");
    }

    [Fact]
    public void TrendyolWebDashboard_ShouldNotContain_GmoReferences()
    {
        var repoRoot = FindRepoRoot();
        var webDashSrc = Path.Combine(repoRoot, "MesTech_Trendyol", "apps", "web-dashboard", "src");
        if (!Directory.Exists(webDashSrc))
            return; // Skip if Trendyol not present in this checkout

        var files = Directory.GetFiles(webDashSrc, "*.*", SearchOption.AllDirectories)
            .Where(IsCodeFile)
            .Where(f => !IsExcluded(f))
            .ToArray();

        var violations = files
            .Where(f => GmoPattern.IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(repoRoot, f))
            .ToList();

        violations.Should().BeEmpty(
            "all GMO Plus references must be replaced with MesTech in Trendyol web dashboard");
    }

    [Fact]
    public void FrontendPanel_ShouldNotContain_GmoReferences()
    {
        var repoRoot = FindRepoRoot();
        var panelRoot = Path.Combine(repoRoot, "frontend", "panel");
        if (!Directory.Exists(panelRoot))
            return; // Skip if frontend/panel not present in this checkout

        var files = Directory.GetFiles(panelRoot, "*.*", SearchOption.AllDirectories)
            .Where(IsCodeFile)
            .Where(f => !IsExcluded(f))
            .ToArray();

        var violations = files
            .Where(f => GmoPattern.IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(repoRoot, f))
            .ToList();

        violations.Should().BeEmpty(
            "all GMO Plus references must be replaced with MesTech in frontend panel");
    }

    [Fact]
    public void PackageJsonFiles_ShouldNotContain_GmoPlusScope()
    {
        var repoRoot = FindRepoRoot();
        var trendyolRoot = Path.Combine(repoRoot, "MesTech_Trendyol");
        if (!Directory.Exists(trendyolRoot))
            return;

        var packageFiles = Directory.GetFiles(trendyolRoot, "package.json", SearchOption.AllDirectories)
            .Where(f => !f.Contains("node_modules"))
            .ToArray();

        var violations = packageFiles
            .Where(f => File.ReadAllText(f).Contains("@gmoplus/", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetRelativePath(repoRoot, f))
            .ToList();

        violations.Should().BeEmpty(
            "@gmoplus/ npm scope must be replaced with @mestech/ in all package.json files");
    }

    [Fact]
    public void DockerAndInfraFiles_ShouldNotContain_GmoReferences()
    {
        var repoRoot = FindRepoRoot();
        var infraRoot = Path.Combine(repoRoot, "MesTech_Trendyol", "infra");
        if (!Directory.Exists(infraRoot))
            return;

        var files = Directory.GetFiles(infraRoot, "*.*", SearchOption.AllDirectories)
            .Where(IsCodeFile)
            .Where(f => !IsExcluded(f))
            .ToArray();

        var dockerRoot = new[] {
            Path.Combine(repoRoot, "MesTech_Trendyol", "Dockerfile"),
            Path.Combine(repoRoot, "MesTech_Trendyol", "docker-compose.prod.yml")
        }.Where(File.Exists);

        var allFiles = files.Concat(dockerRoot).ToArray();

        var violations = allFiles
            .Where(f => GmoPattern.IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(repoRoot, f))
            .ToList();

        violations.Should().BeEmpty(
            "all GMO Plus references must be replaced with MesTech in Docker/infra files");
    }

    private static bool IsCodeFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ScanExtensions.Contains(ext);
    }

    private static bool IsExcluded(string path)
    {
        var fileName = Path.GetFileName(path);
        if (ExcludedFiles.Any(e => fileName.Equals(e, StringComparison.OrdinalIgnoreCase)))
            return true;
        if (path.Contains("node_modules") || path.Contains("package-lock"))
            return true;
        if (path.Contains(Path.Combine("bin", "")) || path.Contains(Path.Combine("obj", "")))
            return true;
        return false;
    }

    private static string FindSrcRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
            if (Directory.Exists(Path.Combine(dir, "MesTech.Domain")))
                return dir;
        }
        throw new DirectoryNotFoundException("src/ root not found");
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
        throw new DirectoryNotFoundException("repo root not found (expected MesTech_Trendyol or MesTech_Stok)");
    }
}
