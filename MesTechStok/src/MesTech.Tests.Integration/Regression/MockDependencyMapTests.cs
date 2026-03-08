using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;

namespace MesTech.Tests.Integration.Regression;

/// <summary>
/// 5.D1-02: Mock Bagimlilik Haritasi — documents and verifies the mock service landscape.
/// Active mocks: 6 (3 Desktop + 2 Infrastructure MESA + 1 Integration Invoice)
/// Obsolete mocks: 4 (Desktop Services, [Obsolete] marked)
/// If a new mock is added or an existing one removed, these tests must be updated.
/// </summary>
[Trait("Category", "Regression")]
[Trait("Category", "MockMap")]
public class MockDependencyMapTests
{
    // ── Active Mocks (registered in DI) ──
    // Desktop App.xaml.cs lines 379-385:
    //   ILocationService → MockLocationService
    //   IWarehouseOptimizationService → MockWarehouseOptimizationService
    //   IMobileWarehouseService → MockMobileWarehouseService
    // InfrastructureServiceRegistration.cs lines 92-93:
    //   IMesaAIService → MockMesaAIService
    //   IMesaBotService → MockMesaBotService
    // IntegrationServiceRegistration.cs line 40:
    //   IInvoiceProvider → MockInvoiceProvider

    private static readonly string[] ActiveMockFiles =
    {
        "MockLocationService.cs",
        "MockWarehouseOptimizationService.cs",
        "MockMobileWarehouseService.cs",
        "MockMesaAIService.cs",
        "MockMesaBotService.cs",
        "MockInvoiceProvider.cs",
    };

    // ── Obsolete Mocks (NOT in DI, [Obsolete] marked) ──
    private static readonly string[] ObsoleteMockFiles =
    {
        "MockBarcodeService.cs",
        "MockInventoryService.cs",
        "MockProductService.cs",
        "MockOpenCartService.cs",
    };

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
        throw new DirectoryNotFoundException("src/ root not found from test output directory");
    }

    [Fact]
    public void ActiveMockFiles_ShouldExistInSourceTree()
    {
        var srcRoot = FindSrcRoot();
        foreach (var mockFile in ActiveMockFiles)
        {
            var found = Directory.GetFiles(srcRoot, mockFile, SearchOption.AllDirectories);
            found.Should().NotBeEmpty($"Active mock '{mockFile}' should exist in source tree");
        }
    }

    [Fact]
    public void ObsoleteMockFiles_ShouldExistAndBeMarkedObsolete()
    {
        var srcRoot = FindSrcRoot();
        foreach (var mockFile in ObsoleteMockFiles)
        {
            var files = Directory.GetFiles(srcRoot, mockFile, SearchOption.AllDirectories)
                .Where(f => !f.Contains("test", StringComparison.OrdinalIgnoreCase)
                         && !f.Contains("Test", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            files.Should().NotBeEmpty($"Obsolete mock '{mockFile}' should exist");

            var content = File.ReadAllText(files.First());
            content.Should().Contain("[Obsolete",
                $"'{mockFile}' must be marked [Obsolete] since it is not used in DI");
        }
    }

    [Fact]
    public void TotalMockCount_ShouldBe9()
    {
        // 5 active + 4 obsolete = 9 mock service files
        // If this fails, someone added/removed a mock without updating this map
        var srcRoot = FindSrcRoot();
        var allMocks = Directory.GetFiles(srcRoot, "Mock*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("test", StringComparison.OrdinalIgnoreCase)
                     && !f.Contains("Test", StringComparison.OrdinalIgnoreCase)
                     && !f.Contains("obj", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        allMocks.Length.Should().Be(ActiveMockFiles.Length + ObsoleteMockFiles.Length,
            $"Expected {ActiveMockFiles.Length} active + {ObsoleteMockFiles.Length} obsolete = " +
            $"{ActiveMockFiles.Length + ObsoleteMockFiles.Length} total mock files. " +
            $"Found: {string.Join(", ", allMocks.Select(Path.GetFileName))}");
    }

    [Fact]
    public void ObsoleteMocks_ShouldNotBeRegisteredInDI()
    {
        // Verify obsolete mock class names don't appear in DI registration files
        var srcRoot = FindSrcRoot();

        var diFiles = new List<string>();
        // App.xaml.cs
        var appXamlCs = Directory.GetFiles(srcRoot, "App.xaml.cs", SearchOption.AllDirectories);
        diFiles.AddRange(appXamlCs);
        // ServiceRegistration files
        var regFiles = Directory.GetFiles(srcRoot, "*Registration*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("test", StringComparison.OrdinalIgnoreCase)
                     && !f.Contains("Test", StringComparison.OrdinalIgnoreCase));
        diFiles.AddRange(regFiles);

        foreach (var diFile in diFiles)
        {
            var content = File.ReadAllText(diFile);
            foreach (var obsoleteMock in ObsoleteMockFiles)
            {
                var className = Path.GetFileNameWithoutExtension(obsoleteMock);
                content.Should().NotContain(className,
                    $"Obsolete mock '{className}' should NOT be registered in '{Path.GetFileName(diFile)}'");
            }
        }
    }

    [Fact]
    public void ActiveMocks_ShouldBeRegisteredInDI()
    {
        var srcRoot = FindSrcRoot();

        // Collect all DI registration content
        var diContent = "";
        foreach (var appXaml in Directory.GetFiles(srcRoot, "App.xaml.cs", SearchOption.AllDirectories))
            diContent += File.ReadAllText(appXaml);
        foreach (var reg in Directory.GetFiles(srcRoot, "*Registration*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("test", StringComparison.OrdinalIgnoreCase)
                     && !f.Contains("Test", StringComparison.OrdinalIgnoreCase)))
            diContent += File.ReadAllText(reg);

        foreach (var activeMock in ActiveMockFiles)
        {
            var className = Path.GetFileNameWithoutExtension(activeMock);
            diContent.Should().Contain(className,
                $"Active mock '{className}' should be registered in DI (App.xaml.cs or *Registration*.cs)");
        }
    }
}
