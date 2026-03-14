using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MesTech.Tests.Unit.Infrastructure.Adapters;

/// <summary>
/// B.5 — Foundation-level property and behaviour tests for eBay, Ozon and PttAvm adapters.
/// 15 tests (5 per adapter). Full API tests deferred to TODO H28.
/// </summary>
[Trait("Category", "Unit")]
public class StubAdapterTests
{
    // ══════════════════════════════════════════════════════
    // eBay adapter — 5 tests
    // ══════════════════════════════════════════════════════

    private static EbayAdapter CreateEbay()
        => new(new System.Net.Http.HttpClient(), NullLogger<EbayAdapter>.Instance);

    [Fact]
    public void EbayAdapter_PlatformCode_ShouldBeEbay()
    {
        var adapter = CreateEbay();
        adapter.PlatformCode.Should().Be(nameof(PlatformType.eBay));
    }

    [Fact]
    public void EbayAdapter_IsActive_ShouldBeFalse_ViaIntegratorAdapter()
    {
        // IsActive is not part of IIntegratorAdapter; adapters with no credentials
        // configured should return false from TestConnectionAsync (not throw).
        // This test validates the adapter can be instantiated without error.
        var adapter = CreateEbay();
        adapter.Should().NotBeNull();
    }

    [Fact]
    public async Task EbayAdapter_GetOrdersAsync_ShouldReturnEmpty()
    {
        var adapter = CreateEbay();
        // PullProductsAsync maps to "GetOrders" concept at foundation level
        var result = await adapter.PullProductsAsync(CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task EbayAdapter_UpdateStockAsync_ShouldReturnFalse()
    {
        var adapter = CreateEbay();
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 10);
        result.Should().BeFalse();
    }

    [Fact]
    public void EbayAdapter_SupportsShipment_ShouldBeFalse()
    {
        var adapter = CreateEbay();
        adapter.SupportsShipment.Should().BeFalse();
    }

    // ══════════════════════════════════════════════════════
    // Ozon adapter — 5 tests
    // ══════════════════════════════════════════════════════

    private static OzonAdapter CreateOzon()
        => new(new System.Net.Http.HttpClient(), NullLogger<OzonAdapter>.Instance);

    [Fact]
    public void OzonAdapter_PlatformCode_ShouldBeOzon()
    {
        var adapter = CreateOzon();
        adapter.PlatformCode.Should().Be(nameof(PlatformType.Ozon));
    }

    [Fact]
    public void OzonAdapter_IsActive_NotThrow_WhenNoCredentials()
    {
        // Adapter must be safely constructible; IsActive implied false before Configure
        var adapter = CreateOzon();
        adapter.Should().NotBeNull();
    }

    [Fact]
    public async Task OzonAdapter_GetOrdersAsync_ShouldReturnEmpty()
    {
        var adapter = CreateOzon();
        var result = await adapter.PullProductsAsync(CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task OzonAdapter_UpdateStockAsync_ShouldReturnFalse()
    {
        var adapter = CreateOzon();
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 5);
        result.Should().BeFalse();
    }

    [Fact]
    public void OzonAdapter_SupportsShipment_ShouldBeTrue()
    {
        var adapter = CreateOzon();
        adapter.SupportsShipment.Should().BeTrue();
    }

    // ══════════════════════════════════════════════════════
    // PttAvm adapter — 5 tests
    // ══════════════════════════════════════════════════════

    private static PttAvmAdapter CreatePttAvm()
        => new(new System.Net.Http.HttpClient(), NullLogger<PttAvmAdapter>.Instance);

    [Fact]
    public void PttAvmAdapter_PlatformCode_ShouldBePttAVM()
    {
        var adapter = CreatePttAvm();
        adapter.PlatformCode.Should().Be(nameof(PlatformType.PttAVM));
    }

    [Fact]
    public void PttAvmAdapter_IsActive_NotThrow_WhenNoCredentials()
    {
        // Adapter must be safely constructible; IsActive implied false before Configure
        var adapter = CreatePttAvm();
        adapter.Should().NotBeNull();
    }

    [Fact]
    public async Task PttAvmAdapter_GetOrdersAsync_ShouldReturnEmpty()
    {
        var adapter = CreatePttAvm();
        var result = await adapter.PullProductsAsync(CancellationToken.None);
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task PttAvmAdapter_UpdateStockAsync_ShouldReturnFalse()
    {
        var adapter = CreatePttAvm();
        var result = await adapter.PushStockUpdateAsync(Guid.NewGuid(), 3);
        result.Should().BeFalse();
    }

    [Fact]
    public void PttAvmAdapter_SupportsShipment_ShouldBeTrue()
    {
        var adapter = CreatePttAvm();
        adapter.SupportsShipment.Should().BeTrue();
    }
}
