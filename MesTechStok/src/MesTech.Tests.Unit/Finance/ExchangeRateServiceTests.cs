using FluentAssertions;
using MesTech.Infrastructure.Finance;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MesTech.Tests.Unit.Finance;

/// <summary>
/// DEV 5 — Dalga 11 Task 5.2: ExchangeRateService tests.
/// Tests TCMB XML kur servisi — same currency, non-TRY target,
/// TCMB failure fallback, ConvertToTry with TRY, ConvertToTry with USD fallback.
/// Depends on: DEV 4 Task 4.1 (ExchangeRateService implementation).
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "MultiCurrency")]
[Trait("Phase", "Dalga11")]
public class ExchangeRateServiceTests
{
    private ExchangeRateService CreateService(
        HttpMessageHandler? handler = null)
    {
        var httpClient = handler is not null
            ? new HttpClient(handler)
            : new HttpClient();
        var cache = new MemoryCache(
            new MemoryCacheOptions());
        return new ExchangeRateService(httpClient, cache,
            NullLogger<ExchangeRateService>.Instance);
    }

    [Fact]
    public async Task GetRateAsync_SameCurrency_ReturnsOne()
    {
        var service = CreateService();

        var rate = await service.GetRateAsync("TRY", "TRY");

        rate.Should().Be(1m);
    }

    [Fact]
    public async Task GetRateAsync_NonTryCurrencyTarget_ShouldReturnCrossRate()
    {
        // Cross-currency conversion is now supported via cross rate: fromRate/toRate
        var badHandler = new BadHttpMessageHandler();
        var service = CreateService(badHandler);

        var rate = await service.GetRateAsync("USD", "EUR");

        // Fallback rates: USD=33, EUR=36 → cross rate = 33/36
        rate.Should().BeGreaterThan(0m);
        rate.Should().BeApproximately(33m / 36m, 0.01m);
    }

    [Fact]
    public async Task GetRateAsync_WhenTcmbDown_ReturnsFallback()
    {
        // TCMB erisilemez — fallback kullan
        var badHandler = new BadHttpMessageHandler();
        var service = CreateService(badHandler);

        var rate = await service.GetRateAsync("USD");

        rate.Should().BeGreaterThan(0m);
        rate.Should().Be(33.0m); // Fallback USD kuru
    }

    [Fact]
    public async Task ConvertToTry_TryCurrency_ReturnsUnchanged()
    {
        var service = CreateService();

        var result = await service.ConvertToTryAsync(100m, "TRY");

        result.Should().Be(100m);
    }

    [Fact]
    public async Task ConvertToTry_UsdFallback_UsesApproximateRate()
    {
        var badHandler = new BadHttpMessageHandler();
        var service = CreateService(badHandler);

        var result = await service.ConvertToTryAsync(100m, "USD");

        result.Should().Be(3300m); // 100 * 33.0 fallback
    }

    /// <summary>
    /// Test helper: simulates TCMB XML API being unreachable.
    /// </summary>
    private class BadHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
            => throw new HttpRequestException("Test: TCMB erisilemez");
    }
}
