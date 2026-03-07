using FluentAssertions;
using MesTech.Application.DTOs;
using MesTech.Application.Interfaces;
using MesTech.Domain.Entities;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Factory;
using Microsoft.Extensions.Logging;
using Moq;

namespace MesTech.Tests.Unit.Infrastructure;

/// <summary>
/// AdapterFactory unit tests — DEV5 cross-check for DEV3's adapter resolution logic.
/// </summary>
[Trait("Category", "Unit")]
public class AdapterFactoryTests
{
    private static Mock<IIntegratorAdapter> CreateMockAdapter(
        string platformCode,
        bool supportsStock = true,
        bool supportsPrice = true)
    {
        var mock = new Mock<IIntegratorAdapter>();
        mock.Setup(a => a.PlatformCode).Returns(platformCode);
        mock.Setup(a => a.SupportsStockUpdate).Returns(supportsStock);
        mock.Setup(a => a.SupportsPriceUpdate).Returns(supportsPrice);
        return mock;
    }

    private static AdapterFactory CreateFactory(params Mock<IIntegratorAdapter>[] adapters)
    {
        var logger = new Mock<ILogger<AdapterFactory>>();
        return new AdapterFactory(
            adapters.Select(m => m.Object),
            logger.Object);
    }

    // ── Resolve by PlatformType enum ──

    [Fact]
    public void Resolve_ByPlatformType_ShouldReturnCorrectAdapter()
    {
        var trendyol = CreateMockAdapter("Trendyol");
        var opencart = CreateMockAdapter("OpenCart");
        var factory = CreateFactory(trendyol, opencart);

        var result = factory.Resolve(PlatformType.Trendyol);

        result.Should().NotBeNull();
        result!.PlatformCode.Should().Be("Trendyol");
    }

    // ── Resolve by string (case-insensitive) ──

    [Fact]
    public void Resolve_ByString_ShouldBeCaseInsensitive()
    {
        var adapter = CreateMockAdapter("Trendyol");
        var factory = CreateFactory(adapter);

        factory.Resolve("trendyol").Should().NotBeNull();
        factory.Resolve("TRENDYOL").Should().NotBeNull();
        factory.Resolve("Trendyol").Should().NotBeNull();
    }

    // ── Resolve non-existent platform ──

    [Fact]
    public void Resolve_NonExistentPlatform_ShouldReturnNull()
    {
        var adapter = CreateMockAdapter("Trendyol");
        var factory = CreateFactory(adapter);

        factory.Resolve("Amazon").Should().BeNull();
        factory.Resolve(PlatformType.Hepsiburada).Should().BeNull();
    }

    // ── GetAll ──

    [Fact]
    public void GetAll_ShouldReturnAllRegisteredAdapters()
    {
        var a1 = CreateMockAdapter("Trendyol");
        var a2 = CreateMockAdapter("OpenCart");
        var a3 = CreateMockAdapter("Hepsiburada");
        var factory = CreateFactory(a1, a2, a3);

        var all = factory.GetAll();

        all.Should().HaveCount(3);
    }

    // ── GetAll with empty ──

    [Fact]
    public void GetAll_WhenEmpty_ShouldReturnEmptyList()
    {
        var factory = CreateFactory();

        factory.GetAll().Should().BeEmpty();
    }

    // ── ResolveCapability ──

    [Fact]
    public void ResolveCapability_WhenAdapterImplementsInterface_ShouldReturnTyped()
    {
        // IIntegratorAdapter also serves as the capability check target
        var adapter = CreateMockAdapter("Trendyol");
        var factory = CreateFactory(adapter);

        var result = factory.ResolveCapability<IIntegratorAdapter>("Trendyol");

        result.Should().NotBeNull();
    }

    [Fact]
    public void ResolveCapability_WhenAdapterDoesNotImplement_ShouldReturnNull()
    {
        var adapter = CreateMockAdapter("Trendyol");
        var factory = CreateFactory(adapter);

        // IDisposable is not implemented by Mock<IIntegratorAdapter>
        var result = factory.ResolveCapability<IDisposable>("Trendyol");

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveCapability_WhenPlatformNotFound_ShouldReturnNull()
    {
        var factory = CreateFactory();

        var result = factory.ResolveCapability<IIntegratorAdapter>("nonexistent");

        result.Should().BeNull();
    }

    // ── Constructor null guards ──

    [Fact]
    public void Constructor_NullAdapters_ShouldThrowArgumentNullException()
    {
        var logger = new Mock<ILogger<AdapterFactory>>();

        var act = () => new AdapterFactory(null!, logger.Object);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ShouldThrowArgumentNullException()
    {
        var adapters = Enumerable.Empty<IIntegratorAdapter>();

        var act = () => new AdapterFactory(adapters, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
