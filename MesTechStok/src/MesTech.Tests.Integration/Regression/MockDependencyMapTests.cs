using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;

namespace MesTech.Tests.Integration.Regression;

/// <summary>
/// 5.D1-02 / 5.D2-04: Mock Bagimlilik Haritasi — documents and verifies the mock service landscape.
/// Active mocks: 14 (3 Desktop + 9 Infrastructure AI/MESA/Accounting + 2 Integration Invoice)
/// Obsolete mocks: 0 (4 deleted in Dalga 2: MockBarcodeService, MockInventoryService,
///   MockProductService, MockOpenCartService)
/// If a new mock is added or an existing one removed, these tests must be updated.
/// Dalga 4 added: MockBuyboxService, MockStockPredictionService, MockPriceOptimizationService, MockProductSearchService
/// Dalga 5 added: MockInvoiceAdapter
/// Dalga 8 (MUH-01/MUH-02) added: MockAdvisoryAgentClient, MockAdvisoryAgentV2, MockMesaAccountingService
/// </summary>
[Trait("Category", "Regression")]
[Trait("Category", "MockMap")]
public class MockDependencyMapTests
{
    // ── Active Mocks (registered in DI) ──
    // Desktop App.xaml.cs:
    //   ILocationService → MockLocationService
    //   IWarehouseOptimizationService → MockWarehouseOptimizationService
    //   IMobileWarehouseService → MockMobileWarehouseService
    // InfrastructureServiceRegistration.cs:
    //   IMesaAIService → MockMesaAIService
    //   IMesaBotService → MockMesaBotService
    //   IBuyboxService → MockBuyboxService                      (Dalga 4)
    //   IStockPredictionService → MockStockPredictionService    (Dalga 4)
    //   IPriceOptimizationService → MockPriceOptimizationService (Dalga 4)
    //   IProductSearchService → MockProductSearchService        (Dalga 4)
    // IntegrationServiceRegistration.cs:
    //   IInvoiceProvider → MockInvoiceProvider
    //   IInvoiceAdapter → MockInvoiceAdapter                    (Dalga 5)
    // AI/Accounting/Infrastructure:
    //   IAdvisoryAgentClient → MockAdvisoryAgentClient          (Dalga 8 MUH-01)
    //   IAdvisoryAgent → MockAdvisoryAgentV2                    (Dalga 8 MUH-01)
    //   IMesaAccountingService → MockMesaAccountingService      (Dalga 8 MUH-01)

    private static readonly string[] ActiveMockFiles =
    {
        "MockMesaAIService.cs",
        "MockMesaBotService.cs",
        "MockBuyboxService.cs",
        "MockStockPredictionService.cs",
        "MockPriceOptimizationService.cs",
        "MockProductSearchService.cs",
        "MockInvoiceProvider.cs",
        "MockInvoiceAdapter.cs",
        "MockAdvisoryAgentClient.cs",
        "MockAdvisoryAgentV2.cs",
        "MockMesaAccountingService.cs",
    };

    // ── Deleted obsolete mock class names (verify they stay deleted) ──
    private static readonly string[] DeletedObsoleteMockClassNames =
    {
        "MockBarcodeService",
        "MockInventoryService",
        "MockProductService",
        "MockOpenCartService",
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
    public void DeletedObsoleteMocks_ShouldNotExistInSourceTree()
    {
        var srcRoot = FindSrcRoot();
        foreach (var className in DeletedObsoleteMockClassNames)
        {
            var files = Directory.GetFiles(srcRoot, $"{className}.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("test", StringComparison.OrdinalIgnoreCase)
                         && !f.Contains("Test", StringComparison.OrdinalIgnoreCase)
                         && !f.Contains("obj", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            files.Should().BeEmpty(
                $"Deleted mock '{className}' should NOT exist in source tree (Dalga 2 cleanup)");
        }
    }

    [Fact]
    public void TotalMockCount_ShouldBe11()
    {
        // 11 active + 0 obsolete = 11 mock service files
        // If this fails, someone added/removed a mock without updating this map
        var srcRoot = FindSrcRoot();
        var allMocks = Directory.GetFiles(srcRoot, "Mock*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("test", StringComparison.OrdinalIgnoreCase)
                     && !f.Contains("Test", StringComparison.OrdinalIgnoreCase)
                     && !f.Contains("obj", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        allMocks.Length.Should().Be(ActiveMockFiles.Length,
            $"Expected {ActiveMockFiles.Length} active mock files. " +
            $"Found: {string.Join(", ", allMocks.Select(Path.GetFileName))}");
    }

    [Fact]
    public void DeletedMocks_ShouldNotBeRegisteredInDI()
    {
        var srcRoot = FindSrcRoot();

        var diFiles = new List<string>();
        var appXamlCs = Directory.GetFiles(srcRoot, "App.xaml.cs", SearchOption.AllDirectories);
        diFiles.AddRange(appXamlCs);
        var regFiles = Directory.GetFiles(srcRoot, "*Registration*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("test", StringComparison.OrdinalIgnoreCase)
                     && !f.Contains("Test", StringComparison.OrdinalIgnoreCase));
        diFiles.AddRange(regFiles);

        foreach (var diFile in diFiles)
        {
            var content = File.ReadAllText(diFile);
            foreach (var className in DeletedObsoleteMockClassNames)
            {
                content.Should().NotContain(className,
                    $"Deleted mock '{className}' should NOT be registered in '{Path.GetFileName(diFile)}'");
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
