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
/// Cargo adapter unit tests — 5 kargo adapter (Aras, Yurtici, Surat, MNG, PTT).
/// G505: properties, IsAvailable, Configure, unconfigured state.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Layer", "Adapter")]
[Trait("Group", "Cargo")]
public class CargoAdapterUnitTests
{
    private readonly MockHttpMessageHandler _handler = new();

    private HttpClient CreateHttpClient(string baseUrl = "https://api.test.com/")
    {
        return new HttpClient(_handler) { BaseAddress = new Uri(baseUrl) };
    }

    // ═══════════════════════════════════════
    // ArasKargoAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void ArasKargo_Provider_ReturnsCorrect()
    {
        var adapter = new ArasKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<ArasKargoAdapter>>());
        adapter.Provider.Should().Be(CargoProvider.ArasKargo);
    }

    [Fact]
    public void ArasKargo_Capabilities_Correct()
    {
        var adapter = new ArasKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<ArasKargoAdapter>>());
        adapter.SupportsCancellation.Should().BeTrue();
        adapter.SupportsLabelGeneration.Should().BeTrue();
        adapter.SupportsCashOnDelivery.Should().BeTrue();
        adapter.SupportsMultiParcel.Should().BeFalse();
    }

    [Fact]
    public async Task ArasKargo_IsAvailable_HealthEndpoint_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, """{"status":"healthy"}""");
        var adapter = new ArasKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<ArasKargoAdapter>>());

        var result = await adapter.IsAvailableAsync();
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ArasKargo_IsAvailable_ServerDown_ReturnsFalse()
    {
        _handler.EnqueueResponse(HttpStatusCode.ServiceUnavailable);
        var adapter = new ArasKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<ArasKargoAdapter>>());

        var result = await adapter.IsAvailableAsync();
        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════
    // YurticiKargoAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void YurticiKargo_Provider_ReturnsCorrect()
    {
        var adapter = new YurticiKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<YurticiKargoAdapter>>());
        adapter.Provider.Should().Be(CargoProvider.YurticiKargo);
    }

    [Fact]
    public void YurticiKargo_Capabilities_Correct()
    {
        var adapter = new YurticiKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<YurticiKargoAdapter>>());
        adapter.SupportsCancellation.Should().BeFalse(); // API does not support
        adapter.SupportsLabelGeneration.Should().BeTrue();
        adapter.SupportsCashOnDelivery.Should().BeTrue();
        adapter.SupportsMultiParcel.Should().BeTrue();
    }

    [Fact]
    public async Task YurticiKargo_CancelShipment_AlwaysReturnsFalse()
    {
        var adapter = new YurticiKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<YurticiKargoAdapter>>());
        var result = await adapter.CancelShipmentAsync("TEST-123");
        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════
    // SuratKargoAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void SuratKargo_Provider_ReturnsCorrect()
    {
        var adapter = new SuratKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<SuratKargoAdapter>>());
        adapter.Provider.Should().Be(CargoProvider.SuratKargo);
    }

    [Fact]
    public void SuratKargo_Capabilities_Correct()
    {
        var adapter = new SuratKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<SuratKargoAdapter>>());
        adapter.SupportsCancellation.Should().BeTrue();
        adapter.SupportsLabelGeneration.Should().BeTrue();
        adapter.SupportsCashOnDelivery.Should().BeFalse(); // No COD
        adapter.SupportsMultiParcel.Should().BeFalse();
    }

    [Fact]
    public async Task SuratKargo_IsAvailable_HealthOK_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, """{"status":"ok"}""");
        var adapter = new SuratKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<SuratKargoAdapter>>());

        var result = await adapter.IsAvailableAsync();
        result.Should().BeTrue();
    }

    // ═══════════════════════════════════════
    // MngKargoAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void MngKargo_Provider_ReturnsCorrect()
    {
        var adapter = new MngKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<MngKargoAdapter>>());
        adapter.Provider.Should().Be(CargoProvider.MngKargo);
    }

    [Fact]
    public void MngKargo_Capabilities_AllTrue()
    {
        var adapter = new MngKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<MngKargoAdapter>>());
        adapter.SupportsCancellation.Should().BeTrue();
        adapter.SupportsLabelGeneration.Should().BeTrue();
        adapter.SupportsCashOnDelivery.Should().BeTrue();
        adapter.SupportsMultiParcel.Should().BeTrue();
    }

    [Fact]
    public async Task MngKargo_IsAvailable_HealthOK_ReturnsTrue()
    {
        _handler.EnqueueResponse(HttpStatusCode.OK, """{"status":"healthy"}""");
        var adapter = new MngKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<MngKargoAdapter>>());

        var result = await adapter.IsAvailableAsync();
        result.Should().BeTrue();
    }

    // ═══════════════════════════════════════
    // PttKargoAdapter
    // ═══════════════════════════════════════

    [Fact]
    public void PttKargo_Provider_ReturnsCorrect()
    {
        var adapter = new PttKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<PttKargoAdapter>>());
        adapter.Provider.Should().Be(CargoProvider.PttKargo);
    }

    [Fact]
    public void PttKargo_Capabilities_AllTrue()
    {
        var adapter = new PttKargoAdapter(CreateHttpClient(), Mock.Of<ILogger<PttKargoAdapter>>());
        adapter.SupportsCancellation.Should().BeTrue();
        adapter.SupportsLabelGeneration.Should().BeTrue();
        adapter.SupportsCashOnDelivery.Should().BeTrue();
        adapter.SupportsMultiParcel.Should().BeTrue();
    }
}
