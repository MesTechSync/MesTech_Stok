using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace MesTech.Integration.Tests.Unit.I18n;

/// <summary>
/// Validates .resx i18n resource files for consistency, coverage,
/// and naming conventions across all 4 supported languages (EMR-17).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "I18n")]
public sealed class SharedResourceTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();

    private static readonly string ResourceDir = Path.Combine(
        SolutionRoot, "src", "MesTech.Blazor", "Resources");

    private static readonly string TrPath = Path.Combine(ResourceDir, "SharedResource.tr.resx");
    private static readonly string EnPath = Path.Combine(ResourceDir, "SharedResource.en.resx");
    private static readonly string ArPath = Path.Combine(ResourceDir, "SharedResource.ar.resx");
    private static readonly string DePath = Path.Combine(ResourceDir, "SharedResource.de.resx");

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MesTechStok.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Solution root not found");
    }

    private static Dictionary<string, string> ReadResxKeys(string path)
    {
        var doc = XDocument.Load(path);
        return doc.Descendants("data")
            .Where(d => d.Attribute("name") != null)
            .ToDictionary(
                d => d.Attribute("name")!.Value,
                d => d.Element("value")?.Value ?? "");
    }

    // --- Test 1: All 4 .resx files load correctly ---
    [Theory]
    [InlineData("SharedResource.tr.resx")]
    [InlineData("SharedResource.en.resx")]
    [InlineData("SharedResource.ar.resx")]
    [InlineData("SharedResource.de.resx")]
    public void ResxFile_LoadsCorrectly(string fileName)
    {
        // Arrange
        var path = Path.Combine(ResourceDir, fileName);

        // Act
        var exists = File.Exists(path);
        var keys = ReadResxKeys(path);

        // Assert
        exists.Should().BeTrue($"{fileName} must exist in Resources directory");
        keys.Should().NotBeEmpty($"{fileName} must contain at least one key");
    }

    // --- Test 2: TR and EN key count consistency ---
    [Fact]
    public void TrAndEn_HaveSameKeyCount()
    {
        // Arrange
        var trKeys = ReadResxKeys(TrPath);
        var enKeys = ReadResxKeys(EnPath);

        // Assert
        trKeys.Count.Should().Be(enKeys.Count,
            "TR and EN resx files must have identical key counts to prevent missing translations");
    }

    // --- Test 3: No empty values in tr.resx ---
    [Fact]
    public void NoEmptyValues_InTrResx()
    {
        // Arrange
        var trKeys = ReadResxKeys(TrPath);

        // Act
        var emptyKeys = trKeys.Where(kv => string.IsNullOrWhiteSpace(kv.Value))
                              .Select(kv => kv.Key)
                              .ToList();

        // Assert
        emptyKeys.Should().BeEmpty(
            "no key in SharedResource.tr.resx should have an empty value — found: [{0}]",
            string.Join(", ", emptyKeys));
    }

    // --- Test 4: No empty values in en.resx ---
    [Fact]
    public void NoEmptyValues_InEnResx()
    {
        // Arrange
        var enKeys = ReadResxKeys(EnPath);

        // Act
        var emptyKeys = enKeys.Where(kv => string.IsNullOrWhiteSpace(kv.Value))
                              .Select(kv => kv.Key)
                              .ToList();

        // Assert
        emptyKeys.Should().BeEmpty(
            "no key in SharedResource.en.resx should have an empty value — found: [{0}]",
            string.Join(", ", emptyKeys));
    }

    // --- Test 5: Placeholder format consistency ({0}, {1}) ---
    [Fact]
    public void PlaceholderFormats_AreConsistentBetweenTrAndEn()
    {
        // Arrange
        var trKeys = ReadResxKeys(TrPath);
        var enKeys = ReadResxKeys(EnPath);
        var placeholderRegex = new Regex(@"\{(\d+)\}");

        // Act
        var mismatches = new List<string>();
        foreach (var (key, trValue) in trKeys)
        {
            if (!enKeys.TryGetValue(key, out var enValue))
                continue;

            var trPlaceholders = placeholderRegex.Matches(trValue)
                .Select(m => m.Value)
                .OrderBy(p => p)
                .ToList();

            var enPlaceholders = placeholderRegex.Matches(enValue)
                .Select(m => m.Value)
                .OrderBy(p => p)
                .ToList();

            if (!trPlaceholders.SequenceEqual(enPlaceholders))
                mismatches.Add(key);
        }

        // Assert
        mismatches.Should().BeEmpty(
            "placeholder formats must match between TR and EN — mismatched keys: [{0}]",
            string.Join(", ", mismatches));
    }

    // --- Test 6: Key naming convention (PascalCase with dots) ---
    [Fact]
    public void KeyNaming_DotSeparatedPascalCase()
    {
        // Arrange
        var trKeys = ReadResxKeys(TrPath);
        var validKeyPattern = new Regex(@"^[A-Z][a-zA-Z0-9]*(\.[A-Z][a-zA-Z0-9]*)*$");

        // Act
        var invalidKeys = trKeys.Keys
            .Where(k => !validKeyPattern.IsMatch(k))
            .ToList();

        // Assert
        invalidKeys.Should().BeEmpty(
            "all i18n keys must follow Dot.Separated.PascalCase convention — invalid: [{0}]",
            string.Join(", ", invalidKeys.Take(10)));
    }

    // --- Test 7: SharedResource marker class exists ---
    [Fact]
    public void SharedResourceMarkerClass_Exists()
    {
        // Arrange
        var markerPath = Path.Combine(ResourceDir, "SharedResource.cs");

        // Act
        var exists = File.Exists(markerPath);

        // Assert
        exists.Should().BeTrue("SharedResource.cs marker class must exist for IStringLocalizer<SharedResource>");

        if (exists)
        {
            var content = File.ReadAllText(markerPath);
            content.Should().Contain("class SharedResource",
                "marker file must contain the SharedResource class definition");
        }
    }

    // --- Test 8: Critical keys present ---
    [Theory]
    [InlineData("Dashboard.Title")]
    [InlineData("Dashboard.Welcome")]
    [InlineData("Common.Button.Save")]
    [InlineData("Common.Button.Cancel")]
    [InlineData("Common.Button.Back")]
    [InlineData("Common.Button.Next")]
    [InlineData("Nav.Dashboard")]
    [InlineData("Nav.Stock")]
    [InlineData("Settings.Language")]
    [InlineData("Form.CompanyName")]
    public void CriticalKey_ExistsInTrResx(string key)
    {
        // Arrange
        var trKeys = ReadResxKeys(TrPath);

        // Assert
        trKeys.Should().ContainKey(key,
            $"critical i18n key '{key}' must exist in SharedResource.tr.resx");
        trKeys[key].Should().NotBeNullOrWhiteSpace(
            $"critical i18n key '{key}' must have a non-empty value");
    }

    // --- Test 9: All TR keys have EN counterpart ---
    [Fact]
    public void AllTrKeys_HaveEnCounterpart()
    {
        // Arrange
        var trKeys = ReadResxKeys(TrPath);
        var enKeys = ReadResxKeys(EnPath);

        // Act
        var missingInEn = trKeys.Keys.Except(enKeys.Keys).ToList();

        // Assert
        missingInEn.Should().BeEmpty(
            "every key in tr.resx must have a corresponding key in en.resx — missing: [{0}]",
            string.Join(", ", missingInEn));
    }

    // --- Test 10: TR resx has minimum key coverage ---
    [Fact]
    public void TrResx_HasMinimumKeyCoverage()
    {
        // Arrange
        var trKeys = ReadResxKeys(TrPath);

        // Assert — resx must have broad UI coverage
        trKeys.Count.Should().BeGreaterThanOrEqualTo(200,
            "SharedResource.tr.resx must contain at least 200 keys for full UI coverage");
    }
}
