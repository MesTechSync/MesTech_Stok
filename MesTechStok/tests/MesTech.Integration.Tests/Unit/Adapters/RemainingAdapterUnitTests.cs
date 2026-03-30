using System.Net;
using FluentAssertions;
using MesTech.Domain.Enums;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Integration.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MesTech.Integration.Tests.Unit.Adapters;

/// <summary>
/// Remaining untested adapter tests — Amazon EU/TR, Bitrix24, PttAvm, HepsiJet, Sendeo.
/// G505: properties, TestConnection, capabilities.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "Remaining")]
public class RemainingAdapterUnitTests
{
    private readonly MockHttpMessageHandler _handler = new();

    private HttpClient CreateHttpClient(string baseUrl = "https://api.test.com/")
        => new HttpClient(_handler) { BaseAddress = new Uri(baseUrl) };

    // ═══════════════════════════════════════
    // AmazonEuAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void AmazonEu_PlatformCode_Correct()
    {
        var adapter = new AmazonEuAdapter(CreateHttpClient(), Mock.Of<ILogger<AmazonEuAdapter>>());
        adapter.PlatformCode.Should().Be(nameof(PlatformType.AmazonEu));
    }

    [Fact]
    public void AmazonEu_Capabilities_AllTrue()
    {
        var adapter = new AmazonEuAdapter(CreateHttpClient(), Mock.Of<ILogger<AmazonEuAdapter>>());
        adapter.SupportsStockUpdate.Should().BeTrue();
        adapter.SupportsPriceUpdate.Should().BeTrue();
        adapter.SupportsShipment.Should().BeTrue();
    }

    [Fact]
    public async Task AmazonEu_TestConnection_EmptyCredentials_Fails()
    {
        var adapter = new AmazonEuAdapter(CreateHttpClient(), Mock.Of<ILogger<AmazonEuAdapter>>());
        var result = await adapter.TestConnectionAsync(new Dictionary<string, string>());
        result.IsSuccess.Should().BeFalse();
    }

    // ═══════════════════════════════════════
    // AmazonTrAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void AmazonTr_PlatformCode_Correct()
    {
        var adapter = new AmazonTrAdapter(CreateHttpClient(), Mock.Of<ILogger<AmazonTrAdapter>>());
        adapter.PlatformCode.Should().Be(nameof(PlatformType.Amazon));
    }

    [Fact]
    public void AmazonTr_Capabilities_AllTrue()
    {
        var adapter = new AmazonTrAdapter(CreateHttpClient(), Mock.Of<ILogger<AmazonTrAdapter>>());
        adapter.SupportsStockUpdate.Should().BeTrue();
        adapter.SupportsPriceUpdate.Should().BeTrue();
        adapter.SupportsShipment.Should().BeTrue();
    }

    [Fact]
    public async Task AmazonTr_TestConnection_EmptyCredentials_Fails()
    {
        var adapter = new AmazonTrAdapter(CreateHttpClient(), Mock.Of<ILogger<AmazonTrAdapter>>());
        var result = await adapter.TestConnectionAsync(new Dictionary<string, string>());
        result.IsSuccess.Should().BeFalse();
    }

    // ═══════════════════════════════════════
    // Bitrix24Adapter
    // ═══════════════════════════════════════

    [Fact]
    public void Bitrix24_PlatformCode_Correct()
    {
        var adapter = new Bitrix24Adapter(CreateHttpClient(), Mock.Of<ILogger<Bitrix24Adapter>>());
        adapter.PlatformCode.Should().Be(nameof(PlatformType.Bitrix24));
    }

    // ═══════════════════════════════════════
    // PttAvmAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void PttAvm_PlatformCode_Correct()
    {
        var adapter = new PttAvmAdapter(CreateHttpClient(), Mock.Of<ILogger<PttAvmAdapter>>());
        adapter.PlatformCode.Should().Be(nameof(PlatformType.PttAVM));
    }

    [Fact]
    public void PttAvm_Capabilities_AllTrue()
    {
        var adapter = new PttAvmAdapter(CreateHttpClient(), Mock.Of<ILogger<PttAvmAdapter>>());
        adapter.SupportsStockUpdate.Should().BeTrue();
        adapter.SupportsPriceUpdate.Should().BeTrue();
        adapter.SupportsShipment.Should().BeTrue();
    }

    [Fact]
    public async Task PttAvm_TestConnection_EmptyCredentials_Fails()
    {
        var adapter = new PttAvmAdapter(CreateHttpClient(), Mock.Of<ILogger<PttAvmAdapter>>());
        var result = await adapter.TestConnectionAsync(new Dictionary<string, string>());
        result.IsSuccess.Should().BeFalse();
    }

    // ═══════════════════════════════════════
    // HepsiJetCargoAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void HepsiJet_Provider_Correct()
    {
        var adapter = new HepsiJetCargoAdapter(CreateHttpClient(), Mock.Of<ILogger<HepsiJetCargoAdapter>>());
        adapter.Provider.Should().Be(CargoProvider.Hepsijet);
    }

    [Fact]
    public async Task HepsiJet_IsAvailable_HealthOK_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, """{"status":"ok"}""");
        var adapter = new HepsiJetCargoAdapter(CreateHttpClient(), Mock.Of<ILogger<HepsiJetCargoAdapter>>());

        var result = await adapter.IsAvailableAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HepsiJet_IsAvailable_ServerDown_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        var adapter = new HepsiJetCargoAdapter(CreateHttpClient(), Mock.Of<ILogger<HepsiJetCargoAdapter>>());

        var result = await adapter.IsAvailableAsync();
        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════
    // SendeoCargoAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void Sendeo_Provider_Correct()
    {
        var adapter = new SendeoCargoAdapter(CreateHttpClient(), Mock.Of<ILogger<SendeoCargoAdapter>>());
        adapter.Provider.Should().Be(CargoProvider.Sendeo);
    }

    [Fact]
    public async Task Sendeo_IsAvailable_HealthOK_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, """{"status":"ok"}""");
        var adapter = new SendeoCargoAdapter(CreateHttpClient(), Mock.Of<ILogger<SendeoCargoAdapter>>());

        var result = await adapter.IsAvailableAsync();
        result.Should().BeTrue();
    }
}
