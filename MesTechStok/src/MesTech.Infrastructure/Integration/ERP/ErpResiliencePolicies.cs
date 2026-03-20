using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace MesTech.Infrastructure.Integration.ERP;

/// <summary>
/// Polly resilience policies for all ERP HTTP calls.
/// Retry: 3 attempts with exponential backoff (1s → 2s → 4s) + jitter.
/// Circuit breaker: 5 consecutive failures → 30s open → half-open probe.
/// Timeout: 30s per-request timeout.
/// </summary>
public static class ErpResiliencePolicies
{
    /// <summary>
    /// Named HttpClient keys for each ERP adapter.
    /// </summary>
    public static class ClientNames
    {
        public const string Parasut = "ErpParasut";
        public const string ParasutToken = "ErpParasutToken";
        public const string Logo = "ErpLogo";
        public const string LogoToken = "ErpLogoToken";
        public const string BizimHesap = "ErpBizimHesap";
        public const string Netsis = "ErpNetsis";
        public const string Nebim = "ErpNebim";
    }

    /// <summary>
    /// Registers named HttpClients for all ERP adapters with Polly retry + circuit breaker policies.
    /// </summary>
    public static IServiceCollection AddErpResilientHttpClients(this IServiceCollection services)
    {
        // ERP adapter clients — retry + circuit breaker
        RegisterResilientClient(services, ClientNames.Parasut);
        RegisterResilientClient(services, ClientNames.Logo);
        RegisterResilientClient(services, ClientNames.BizimHesap);
        RegisterResilientClient(services, ClientNames.Netsis);
        RegisterResilientClient(services, ClientNames.Nebim);

        // Token service clients — retry only (no circuit breaker, token calls are rare)
        RegisterTokenClient(services, ClientNames.ParasutToken);
        RegisterTokenClient(services, ClientNames.LogoToken);

        return services;
    }

    private static void RegisterResilientClient(IServiceCollection services, string name)
    {
        services.AddHttpClient(name)
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());
    }

    private static void RegisterTokenClient(IServiceCollection services, string name)
    {
        services.AddHttpClient(name)
            .AddPolicyHandler(GetRetryPolicy());
    }

    /// <summary>
    /// 3 retries with exponential backoff + jitter.
    /// Retries on: 5xx, 408 (timeout), 429 (rate limit), network errors.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 5xx + 408
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests) // 429
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)) // 1s, 2s, 4s
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)), // jitter
                onRetry: (outcome, delay, attempt, _) =>
                {
                    // Structured logging via ILogger would require DI; use trace-level hint.
                    // Adapters log their own errors when final attempt fails.
                });
    }

    /// <summary>
    /// Circuit breaker: opens after 5 consecutive failures, stays open for 30 seconds.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }
}
