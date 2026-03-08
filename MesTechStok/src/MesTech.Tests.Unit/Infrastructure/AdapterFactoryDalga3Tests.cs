using FluentAssertions;
using MesTech.Application.Interfaces;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Infrastructure.Integration.Factory;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// AdapterFactory Dalga 3 tests — verify resolution for new platform adapters
/// (CiceksepetiAdapter, HepsiburadaAdapter).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Phase", "Dalga3")]
public class AdapterFactoryDalga3Tests
{
    private static CiceksepetiAdapter CreateCiceksepetiAdapter()
    {
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<CiceksepetiAdapter>>();
        return new CiceksepetiAdapter(httpClient, logger.Object);
    }

    private static HepsiburadaAdapter CreateHepsiburadaAdapter()
    {
        var httpClient = new HttpClient();
        var logger = new Mock<ILogger<HepsiburadaAdapter>>();
        return new HepsiburadaAdapter(httpClient, logger.Object);
    }

    private static AdapterFactory CreateFactory(params IIntegratorAdapter[] adapters)
    {
        var logger = new Mock<ILogger<AdapterFactory>>();
        return new AdapterFactory(adapters, logger.Object);
    }

    // ── 1. Resolve Ciceksepeti by PlatformType ──

    [Fact]
    public void Resolve_Ciceksepeti_ShouldReturnCiceksepetiAdapter()
    {
        // Arrange
        var csAdapter = CreateCiceksepetiAdapter();
        var factory = CreateFactory(csAdapter);

        // Act
        var result = factory.Resolve(PlatformType.Ciceksepeti);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<CiceksepetiAdapter>();
        result!.PlatformCode.Should().Be("Ciceksepeti");
    }

    // ── 2. Resolve Hepsiburada by PlatformType ──

    [Fact]
    public void Resolve_Hepsiburada_ShouldReturnHepsiburadaAdapter()
    {
        // Arrange
        var hbAdapter = CreateHepsiburadaAdapter();
        var factory = CreateFactory(hbAdapter);

        // Act
        var result = factory.Resolve(PlatformType.Hepsiburada);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<HepsiburadaAdapter>();
        result!.PlatformCode.Should().Be("Hepsiburada");
    }

    // ── 3. GetAll with new platforms should include all registered ──

    [Fact]
    public void GetAll_WithNewPlatforms_ShouldIncludeAllRegistered()
    {
        // Arrange
        var csAdapter = CreateCiceksepetiAdapter();
        var hbAdapter = CreateHepsiburadaAdapter();
        var factory = CreateFactory(csAdapter, hbAdapter);

        // Act
        var all = factory.GetAll();

        // Assert
        all.Should().HaveCount(2);
        all.Select(a => a.PlatformCode).Should().Contain("Ciceksepeti");
        all.Select(a => a.PlatformCode).Should().Contain("Hepsiburada");
    }

    // ── 4. ResolveCapability — Ciceksepeti implements IWebhookCapableAdapter ──

    [Fact]
    public void ResolveCapability_CiceksepetiWebhook_ShouldReturnWebhookCapable()
    {
        // Arrange
        var csAdapter = CreateCiceksepetiAdapter();
        var factory = CreateFactory(csAdapter);

        // Act
        var result = factory.ResolveCapability<IWebhookCapableAdapter>("Ciceksepeti");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeAssignableTo<IWebhookCapableAdapter>();
        result.Should().BeOfType<CiceksepetiAdapter>();
    }
}
