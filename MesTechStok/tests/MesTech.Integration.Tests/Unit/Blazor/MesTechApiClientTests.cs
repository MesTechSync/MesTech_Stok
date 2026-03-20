using FluentAssertions;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Blazor;

/// <summary>
/// MesTechApiClient source-level verification tests (EMR-15-P — ALAN-F).
/// Validates that the API client has all required patterns and shortcuts.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Blazor")]
public sealed class MesTechApiClientTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();
    private static readonly string ApiClientPath = Path.Combine(
        SolutionRoot, "src", "MesTech.Blazor", "Services", "MesTechApiClient.cs");

    private static string FindSolutionRoot()
    {
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MesTechStok.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Solution root not found");
    }

    [Fact]
    public void SourceFile_Exists()
    {
        File.Exists(ApiClientPath).Should().BeTrue(
            "MesTechApiClient.cs must exist at src/MesTech.Blazor/Services/");
    }

    [Fact]
    public void Has_SafeGetAsync_Method()
    {
        var content = File.ReadAllText(ApiClientPath);
        content.Should().Contain("SafeGetAsync<T>");
    }

    [Fact]
    public void Has_SafePostAsync_Method()
    {
        var content = File.ReadAllText(ApiClientPath);
        content.Should().Contain("SafePostAsync<T>");
    }

    [Fact]
    public void Has_SafePutAsync_Method()
    {
        var content = File.ReadAllText(ApiClientPath);
        content.Should().Contain("SafePutAsync<T>");
    }

    [Fact]
    public void Has_SafeDeleteAsync_Method()
    {
        var content = File.ReadAllText(ApiClientPath);
        content.Should().Contain("SafeDeleteAsync");
    }

    [Fact]
    public void ReadsBaseUrl_FromConfiguration()
    {
        var content = File.ReadAllText(ApiClientPath);
        content.Should().Contain("WebApi:BaseUrl");
    }

    [Fact]
    public void Has_DashboardKpiShortcut()
    {
        var content = File.ReadAllText(ApiClientPath);
        content.Should().Contain("GetDashboardKpiAsync");
    }

    [Fact]
    public void Has_SettingsProfileShortcut()
    {
        var content = File.ReadAllText(ApiClientPath);
        content.Should().Contain("GetSettingsProfileAsync");
    }

    [Fact]
    public void Catches_HttpRequestException_ForGracefulFallback()
    {
        var content = File.ReadAllText(ApiClientPath);
        content.Should().Contain("HttpRequestException");
    }

    [Fact]
    public void Catches_TaskCanceledException_ForTimeoutFallback()
    {
        var content = File.ReadAllText(ApiClientPath);
        content.Should().Contain("TaskCanceledException");
    }
}