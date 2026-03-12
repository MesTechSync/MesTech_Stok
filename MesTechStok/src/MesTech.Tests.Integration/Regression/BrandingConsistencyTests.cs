using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace MesTech.Tests.Integration.Regression;

/// <summary>
/// DEV 5 — Dalga 7 Task 5.P1-03: Branding consistency meta-tests.
/// Five targeted checks ensuring MesTech branding is correct across
/// HTML titles, package scopes, footer, header, and panel domain.
/// </summary>
[Trait("Category", "Regression")]
[Trait("Category", "BrandingConsistency")]
public class BrandingConsistencyTests
{
    private static readonly Regex TitlePattern = new(
        @"<title>[^<]*</title>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [Fact]
    public void HtmlTitles_ShouldNotContain_GmoPlus()
    {
        var repoRoot = FindRepoRoot();
        var htmlDirs = new[]
        {
            Path.Combine(repoRoot, "MesTech_Trendyol", "apps", "web-dashboard", "src"),
            Path.Combine(repoRoot, "MesTech_Trendyol", "apps", "web-dashboard", "public"),
            Path.Combine(repoRoot, "frontend", "panel")
        }.Where(Directory.Exists);

        var violations = htmlDirs
            .SelectMany(d => Directory.GetFiles(d, "*.html", SearchOption.AllDirectories))
            .Where(f => !f.Contains("node_modules"))
            .Select(f => new { Path = f, Content = File.ReadAllText(f) })
            .SelectMany(f => TitlePattern.Matches(f.Content)
                .Where(m => m.Value.Contains("GMO", StringComparison.OrdinalIgnoreCase) ||
                            m.Value.Contains("gmoplus", StringComparison.OrdinalIgnoreCase))
                .Select(m => $"{Path.GetRelativePath(repoRoot, f.Path)}: {m.Value}"))
            .ToList();

        violations.Should().BeEmpty(
            "HTML <title> tags must not contain GMO/GMOPlus — should reference MesTech");
    }

    [Fact]
    public void PackageJson_ShouldNotContain_GmoPlusScope()
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
            "package.json files must use @mestech/ scope, not @gmoplus/");
    }

    [Fact]
    public void FooterHtml_ShouldReference_MesTech_NotGmoPlus()
    {
        var repoRoot = FindRepoRoot();
        var footerPaths = new[]
        {
            Path.Combine(repoRoot, "MesTech_Trendyol", "apps", "web-dashboard", "src",
                "components", "Footer", "footer.html"),
            Path.Combine(repoRoot, "frontend", "panel", "components", "Footer", "footer.html")
        };

        foreach (var footerPath in footerPaths.Where(File.Exists))
        {
            var content = File.ReadAllText(footerPath);
            var relativePath = Path.GetRelativePath(repoRoot, footerPath);

            content.Should().NotContainEquivalentOf("GMOPlus",
                $"footer ({relativePath}) must reference MesTech, not GMOPlus");

            content.Should().Contain("MesTech",
                $"footer ({relativePath}) should contain MesTech branding");
        }
    }

    [Fact]
    public void PanelCode_ShouldNotReference_DropmesTr()
    {
        var repoRoot = FindRepoRoot();
        var panelRoot = Path.Combine(repoRoot, "frontend", "panel");
        if (!Directory.Exists(panelRoot))
            return;

        var codeFiles = Directory.GetFiles(panelRoot, "*.*", SearchOption.AllDirectories)
            .Where(f => new[] { ".html", ".js", ".json", ".css" }
                .Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Where(f => !f.Contains("node_modules"))
            .ToArray();

        var violations = codeFiles
            .Where(f => File.ReadAllText(f).Contains("dropmes.tr", StringComparison.OrdinalIgnoreCase))
            .Select(f => Path.GetRelativePath(repoRoot, f))
            .ToList();

        violations.Should().BeEmpty(
            "panel code must use mestech.tr domain, not dropmes.tr");
    }

    [Fact]
    public void HeaderHtml_ShouldReference_MesTech_NotGmoPlus()
    {
        var repoRoot = FindRepoRoot();
        var headerPaths = new[]
        {
            Path.Combine(repoRoot, "MesTech_Trendyol", "apps", "web-dashboard", "src",
                "components", "Header", "header.html"),
            Path.Combine(repoRoot, "frontend", "panel", "components", "Header", "header.html")
        };

        foreach (var headerPath in headerPaths.Where(File.Exists))
        {
            var content = File.ReadAllText(headerPath);
            var relativePath = Path.GetRelativePath(repoRoot, headerPath);

            content.Should().NotContainEquivalentOf("GMOPlus",
                $"header ({relativePath}) must reference MesTech, not GMOPlus");

            content.Should().NotContainEquivalentOf("GMO Plus",
                $"header ({relativePath}) must not contain 'GMO Plus'");
        }
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
