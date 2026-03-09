using System.Reflection;
using FluentAssertions;
using MesTech.Infrastructure.Integration.Adapters;

namespace MesTech.Tests.Unit.Integration;

/// <summary>
/// Polly audit verification tests — ensures all adapters with REST HTTP calls
/// have a resilience pipeline (retry + circuit breaker) and rate limiting.
///
/// Audit scope (Dalga 4, Task 6):
///   Platform adapters with standard Polly pipeline:
///     PazaramaAdapter, CiceksepetiAdapter, HepsiburadaAdapter, TrendyolAdapter
///   Cargo adapters with standard Polly pipeline:
///     ArasKargoAdapter, SuratKargoAdapter
///
///   NOT audited (different patterns):
///     OpenCartAdapter — uses Polly retry but no SemaphoreSlim rate limiter, different ExecuteWithRetry style
///     YurticiKargoAdapter — SOAP via SimpleSoapClient, no Polly pipeline
/// </summary>
public class PollyAuditTests
{
    // ── Retry pipeline presence ──────────────────────────────

    [Theory]
    [InlineData(typeof(PazaramaAdapter))]
    [InlineData(typeof(CiceksepetiAdapter))]
    [InlineData(typeof(HepsiburadaAdapter))]
    [InlineData(typeof(TrendyolAdapter))]
    [InlineData(typeof(ArasKargoAdapter))]
    [InlineData(typeof(SuratKargoAdapter))]
    public void Adapter_Has_RetryPipeline(Type adapterType)
    {
        var field = adapterType.GetField("_retryPipeline",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().NotBeNull($"{adapterType.Name} must have _retryPipeline field");
    }

    // ── Rate limiter presence ────────────────────────────────

    [Theory]
    [InlineData(typeof(PazaramaAdapter))]
    [InlineData(typeof(CiceksepetiAdapter))]
    [InlineData(typeof(HepsiburadaAdapter))]
    [InlineData(typeof(TrendyolAdapter))]
    [InlineData(typeof(ArasKargoAdapter))]
    [InlineData(typeof(SuratKargoAdapter))]
    public void Adapter_Has_RateLimiter(Type adapterType)
    {
        var field = adapterType.GetField("_rateLimitSemaphore",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        field.Should().NotBeNull($"{adapterType.Name} must have _rateLimitSemaphore field");
    }

    // ── ExecuteWithRetryAsync helper presence ────────────────

    [Theory]
    [InlineData(typeof(PazaramaAdapter))]
    [InlineData(typeof(CiceksepetiAdapter))]
    [InlineData(typeof(HepsiburadaAdapter))]
    [InlineData(typeof(ArasKargoAdapter))]
    [InlineData(typeof(SuratKargoAdapter))]
    public void Adapter_Has_ExecuteWithRetryAsync(Type adapterType)
    {
        var method = adapterType.GetMethod("ExecuteWithRetryAsync",
            BindingFlags.NonPublic | BindingFlags.Instance);
        method.Should().NotBeNull(
            $"{adapterType.Name} must route all HTTP calls through ExecuteWithRetryAsync");
    }

    // ── OpenCart has retry but no SemaphoreSlim (documented, not enforced) ──

    [Fact]
    public void OpenCartAdapter_Has_RetryPipeline()
    {
        var field = typeof(OpenCartAdapter).GetField("_retryPipeline",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().NotBeNull("OpenCartAdapter must have _retryPipeline field");
    }

    // ── YurticiKargoAdapter uses SOAP — no Polly pipeline expected ──

    [Fact]
    public void YurticiKargoAdapter_Has_No_RetryPipeline()
    {
        var field = typeof(YurticiKargoAdapter).GetField("_retryPipeline",
            BindingFlags.NonPublic | BindingFlags.Instance);
        field.Should().BeNull("YurticiKargoAdapter uses SOAP/SimpleSoapClient — no Polly pipeline");
    }
}
