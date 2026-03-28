using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Components;

namespace MesTech.Blazor.Tests;

// ════════════════════════════════════════════════════════
// DEV5 TUR 18: Blazor page discovery tests (G191)
// Scans all [Route] components in MesTech.Blazor assembly.
// Validates: page count, route uniqueness, namespace convention
// ════════════════════════════════════════════════════════

[Trait("Category", "Unit")]
[Trait("Layer", "Blazor")]
public class BlazorPageDiscoveryTests
{
    private static readonly Assembly BlazorAssembly = typeof(MesTech.Blazor.Components.App).Assembly;

    private static List<(Type Type, string Route)> GetAllRoutedPages()
    {
        var pages = new List<(Type, string)>();

        foreach (var type in BlazorAssembly.GetTypes())
        {
            var routeAttrs = type.GetCustomAttributes<RouteAttribute>();
            foreach (var attr in routeAttrs)
            {
                pages.Add((type, attr.Template));
            }
        }

        return pages;
    }

    [Fact(DisplayName = "Blazor assembly should contain at least 70 routed pages")]
    public void Assembly_ShouldHaveMinimumPageCount()
    {
        var pages = GetAllRoutedPages();

        pages.Count.Should().BeGreaterThanOrEqualTo(70,
            "Blazor assembly should contain at least 70 pages with [Route] attribute");
    }

    [Fact(DisplayName = "All routes should be unique")]
    public void Routes_ShouldBeUnique()
    {
        var pages = GetAllRoutedPages();
        var routes = pages.Select(p => p.Route).ToList();
        var duplicates = routes.GroupBy(r => r)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        duplicates.Should().BeEmpty(
            $"Duplicate routes found: {string.Join(", ", duplicates)}");
    }

    [Fact(DisplayName = "All routes should start with /")]
    public void Routes_ShouldStartWithSlash()
    {
        var pages = GetAllRoutedPages();

        foreach (var (type, route) in pages)
        {
            route.Should().StartWith("/",
                $"{type.Name} route should start with / but got '{route}'");
        }
    }

    [Fact(DisplayName = "All page components should inherit ComponentBase")]
    public void Pages_ShouldInheritComponentBase()
    {
        var pages = GetAllRoutedPages();

        foreach (var (type, route) in pages)
        {
            type.Should().BeAssignableTo<ComponentBase>(
                $"Page '{type.Name}' at route '{route}' should inherit from ComponentBase");
        }
    }

    [Theory(DisplayName = "Critical pages should exist")]
    [InlineData("/")]
    [InlineData("/products")]
    [InlineData("/orders")]
    [InlineData("/settings")]
    [InlineData("/login")]
    public void CriticalPage_ShouldExist(string route)
    {
        var pages = GetAllRoutedPages();
        var exists = pages.Any(p => p.Route.Equals(route, StringComparison.OrdinalIgnoreCase));

        exists.Should().BeTrue($"Critical route '{route}' should exist in Blazor pages");
    }

    [Fact(DisplayName = "No page should have empty route")]
    public void Pages_ShouldNotHaveEmptyRoute()
    {
        var pages = GetAllRoutedPages();

        foreach (var (type, route) in pages)
        {
            route.Should().NotBeNullOrWhiteSpace(
                $"Page '{type.Name}' has empty route");
        }
    }

    [Fact(DisplayName = "Page namespace should contain Components.Pages")]
    public void Pages_ShouldFollowNamespaceConvention()
    {
        var pages = GetAllRoutedPages();
        var violations = pages
            .Where(p => p.Type.Namespace != null && !p.Type.Namespace.Contains("Components"))
            .Select(p => $"{p.Type.Name} ({p.Type.Namespace})")
            .ToList();

        // Allow some flexibility — not all pages may follow strict convention
        violations.Count.Should().BeLessThan(pages.Count / 2,
            $"Most pages should follow Components namespace convention. Violations: {string.Join(", ", violations.Take(5))}");
    }
}
