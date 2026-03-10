using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Invoice;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Integration;

/// <summary>
/// InvoiceProviderFactory testleri.
/// Resolve, GetAll, null guard ve provider cozumleme dogrulanir.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "InvoiceFactory")]
[Trait("Phase", "Dalga4")]
public class InvoiceProviderFactoryTests
{
    private readonly Mock<ILogger<InvoiceProviderFactory>> _loggerMock;

    public InvoiceProviderFactoryTests()
    {
        _loggerMock = new Mock<ILogger<InvoiceProviderFactory>>();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static MockInvoiceProvider CreateMockProvider()
        => new();

    private static Mock<IInvoiceProvider> CreateProviderMock(InvoiceProvider providerType, string name)
    {
        var mock = new Mock<IInvoiceProvider>();
        mock.Setup(p => p.Provider).Returns(providerType);
        mock.Setup(p => p.ProviderName).Returns(name);
        return mock;
    }

    // ===========================================================================
    // 1. Resolve_Mock_ReturnsMockProvider
    // ===========================================================================

    [Fact]
    public void Resolve_Mock_ReturnsMockProvider()
    {
        // Arrange
        var mockProvider = CreateMockProvider();
        var factory = new InvoiceProviderFactory(
            new IInvoiceProvider[] { mockProvider },
            _loggerMock.Object);

        // Act
        var result = factory.Resolve(InvoiceProvider.Manual);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(mockProvider);
        result!.ProviderName.Should().Be("Mock e-Fatura (Test)");
    }

    // ===========================================================================
    // 2. Resolve_Unknown_ReturnsNull
    // ===========================================================================

    [Fact]
    public void Resolve_Unknown_ReturnsNull()
    {
        // Arrange
        var mockProvider = CreateMockProvider();
        var factory = new InvoiceProviderFactory(
            new IInvoiceProvider[] { mockProvider },
            _loggerMock.Object);

        // Act
        var result = factory.Resolve(InvoiceProvider.ELogo);

        // Assert
        result.Should().BeNull();
    }

    // ===========================================================================
    // 3. GetAll_ReturnsAllRegistered
    // ===========================================================================

    [Fact]
    public void GetAll_ReturnsAllRegistered()
    {
        // Arrange
        var mockProvider = CreateMockProvider();
        var sovosMock = CreateProviderMock(InvoiceProvider.Sovos, "Sovos e-Fatura");
        var parasutMock = CreateProviderMock(InvoiceProvider.Parasut, "Parasut e-Fatura");

        var factory = new InvoiceProviderFactory(
            new IInvoiceProvider[] { mockProvider, sovosMock.Object, parasutMock.Object },
            _loggerMock.Object);

        // Act
        var result = factory.GetAll();

        // Assert
        result.Should().HaveCount(3);
        result.Select(p => p.Provider).Should()
            .Contain(new[] { InvoiceProvider.Manual, InvoiceProvider.Sovos, InvoiceProvider.Parasut });
    }

    // ===========================================================================
    // 4. Resolve_Sovos_ReturnsSovosProvider
    // ===========================================================================

    [Fact]
    public void Resolve_Sovos_ReturnsSovosProvider()
    {
        // Arrange
        var sovosMock = CreateProviderMock(InvoiceProvider.Sovos, "Sovos e-Fatura");

        var factory = new InvoiceProviderFactory(
            new IInvoiceProvider[] { sovosMock.Object },
            _loggerMock.Object);

        // Act
        var result = factory.Resolve(InvoiceProvider.Sovos);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(sovosMock.Object);
        result!.Provider.Should().Be(InvoiceProvider.Sovos);
        result.ProviderName.Should().Be("Sovos e-Fatura");
    }

    // ===========================================================================
    // 5. Constructor_NullProviders_ThrowsArgumentNullException
    // ===========================================================================

    [Fact]
    public void Constructor_NullProviders_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new InvoiceProviderFactory(null!, _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("providers");
    }

    // ===========================================================================
    // 6. Constructor_NullLogger_ThrowsArgumentNullException
    // ===========================================================================

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new InvoiceProviderFactory(
            Array.Empty<IInvoiceProvider>(), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // ===========================================================================
    // 7. Resolve_None_ReturnsNull_WhenNoNoneRegistered
    // ===========================================================================

    [Fact]
    public void Resolve_None_ReturnsNull_WhenNoNoneRegistered()
    {
        // Arrange
        var mockProvider = CreateMockProvider();
        var factory = new InvoiceProviderFactory(
            new IInvoiceProvider[] { mockProvider },
            _loggerMock.Object);

        // Act
        var result = factory.Resolve(InvoiceProvider.None);

        // Assert
        result.Should().BeNull();
    }

    // ===========================================================================
    // 8. GetAll_EmptyProviders_ReturnsEmptyList
    // ===========================================================================

    [Fact]
    public void GetAll_EmptyProviders_ReturnsEmptyList()
    {
        // Arrange
        var factory = new InvoiceProviderFactory(
            Array.Empty<IInvoiceProvider>(),
            _loggerMock.Object);

        // Act
        var result = factory.GetAll();

        // Assert
        result.Should().BeEmpty();
    }

    // ===========================================================================
    // 9. Factory_Resolves_All9Providers — D5 A-09..A-16 registration smoke test
    // ===========================================================================

    [Fact]
    public void Factory_Resolves_All9Providers()
    {
        // Arrange — one mock per registered provider (9 total)
        var providers = new IInvoiceProvider[]
        {
            CreateProviderMock(InvoiceProvider.Manual,           "Mock e-Fatura (Test)").Object,
            CreateProviderMock(InvoiceProvider.Sovos,            "Sovos e-Fatura").Object,
            CreateProviderMock(InvoiceProvider.Parasut,          "Parasut e-Fatura").Object,
            CreateProviderMock(InvoiceProvider.TrendyolEFaturam, "Trendyol e-Faturam").Object,
            CreateProviderMock(InvoiceProvider.ELogo,            "eLogo e-Fatura").Object,
            CreateProviderMock(InvoiceProvider.BirFatura,        "BirFatura").Object,
            CreateProviderMock(InvoiceProvider.DijitalPlanet,    "DijitalPlanet").Object,
            CreateProviderMock(InvoiceProvider.GibPortal,        "GIB Portal").Object,
            CreateProviderMock(InvoiceProvider.HepsiburadaFatura,"Hepsiburada Fatura").Object,
        };
        var factory = new InvoiceProviderFactory(providers, _loggerMock.Object);

        // Act + Assert — GetAll returns all 9
        factory.GetAll().Should().HaveCount(9);

        // Assert — each enum value resolves to a non-null provider
        factory.Resolve(InvoiceProvider.Manual).Should().NotBeNull();
        factory.Resolve(InvoiceProvider.Sovos).Should().NotBeNull();
        factory.Resolve(InvoiceProvider.Parasut).Should().NotBeNull();
        factory.Resolve(InvoiceProvider.TrendyolEFaturam).Should().NotBeNull();
        factory.Resolve(InvoiceProvider.ELogo).Should().NotBeNull();
        factory.Resolve(InvoiceProvider.BirFatura).Should().NotBeNull();
        factory.Resolve(InvoiceProvider.DijitalPlanet).Should().NotBeNull();
        factory.Resolve(InvoiceProvider.GibPortal).Should().NotBeNull();
        factory.Resolve(InvoiceProvider.HepsiburadaFatura).Should().NotBeNull();
    }

    // ===========================================================================
    // 10. Factory_ReturnsNull_ForUnknownProvider
    // ===========================================================================

    [Fact]
    public void Factory_ReturnsNull_ForUnknownProvider()
    {
        // Arrange — only Sovos registered
        var factory = new InvoiceProviderFactory(
            [CreateProviderMock(InvoiceProvider.Sovos, "Sovos e-Fatura").Object],
            _loggerMock.Object);

        // Act
        var result = factory.Resolve(InvoiceProvider.DijitalPlanet);

        // Assert
        result.Should().BeNull();
    }
}
