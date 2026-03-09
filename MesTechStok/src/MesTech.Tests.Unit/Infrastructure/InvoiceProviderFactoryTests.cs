using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Invoice;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// InvoiceProviderFactory unit testleri.
/// Factory pattern: Resolve, GetAll, constructor null guards.
/// Contributes to Infrastructure layer coverage (D-15 alt-görev).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "InvoiceProviderFactory")]
[Trait("Phase", "Dalga5")]
public class InvoiceProviderFactoryTests
{
    private readonly ILogger<InvoiceProviderFactory> _logger =
        new Mock<ILogger<InvoiceProviderFactory>>().Object;

    private static Mock<IInvoiceProvider> MakeProvider(InvoiceProvider type)
    {
        var m = new Mock<IInvoiceProvider>();
        m.Setup(p => p.Provider).Returns(type);
        return m;
    }

    // ── Constructor Guards ──

    [Fact]
    public void Constructor_NullProviders_ThrowsArgumentNullException()
    {
        var act = () => new InvoiceProviderFactory(null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("providers");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InvoiceProviderFactory(
            Enumerable.Empty<IInvoiceProvider>(),
            null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    // ── Resolve ──

    [Fact]
    public void Resolve_Sovos_ReturnsSovosProvider()
    {
        var sovos = MakeProvider(InvoiceProvider.Sovos);
        var factory = new InvoiceProviderFactory(new[] { sovos.Object }, _logger);

        var result = factory.Resolve(InvoiceProvider.Sovos);

        result.Should().BeSameAs(sovos.Object);
    }

    [Fact]
    public void Resolve_Parasut_ReturnsParasutProvider()
    {
        var parasut = MakeProvider(InvoiceProvider.Parasut);
        var factory = new InvoiceProviderFactory(new[] { parasut.Object }, _logger);

        var result = factory.Resolve(InvoiceProvider.Parasut);

        result.Should().BeSameAs(parasut.Object);
    }

    [Fact]
    public void Resolve_UnknownProvider_ReturnsNull()
    {
        var sovos = MakeProvider(InvoiceProvider.Sovos);
        var factory = new InvoiceProviderFactory(new[] { sovos.Object }, _logger);

        var result = factory.Resolve(InvoiceProvider.ELogo);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_AfterEmptyFactory_ReturnsNull()
    {
        var factory = new InvoiceProviderFactory(
            Enumerable.Empty<IInvoiceProvider>(), _logger);

        var result = factory.Resolve(InvoiceProvider.Sovos);

        result.Should().BeNull();
    }

    // ── GetAll ──

    [Fact]
    public void GetAll_TwoProviders_ReturnsBoth()
    {
        var sovos = MakeProvider(InvoiceProvider.Sovos);
        var parasut = MakeProvider(InvoiceProvider.Parasut);
        var factory = new InvoiceProviderFactory(
            new[] { sovos.Object, parasut.Object }, _logger);

        var result = factory.GetAll();

        result.Should().HaveCount(2);
        result.Should().Contain(sovos.Object);
        result.Should().Contain(parasut.Object);
    }

    [Fact]
    public void GetAll_EmptyProviders_ReturnsEmptyList()
    {
        var factory = new InvoiceProviderFactory(
            Enumerable.Empty<IInvoiceProvider>(), _logger);

        var result = factory.GetAll();

        result.Should().BeEmpty();
    }
}
