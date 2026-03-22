using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace MesTech.Infrastructure.Integration.Invoice;

/// <summary>
/// Polly resilience policies for all e-Invoice HTTP provider calls.
/// Retry: 3 attempts with exponential backoff (2s → 4s → 8s) + jitter.
/// Circuit breaker: 5 consecutive failures → 60s open.
/// Longer backoff than ERP — invoice APIs (GİB, Sovos) are slower and more sensitive.
/// </summary>
public static class InvoiceResiliencePolicies
{
    /// <summary>
    /// Registers Polly retry + circuit breaker on all invoice named HttpClients.
    /// Must be called AFTER IntegrationHttpClientRegistry.AddIntegrationHttpClients().
    /// </summary>
    public static IServiceCollection AddInvoiceResilientHttpClients(this IServiceCollection services)
    {
        var invoiceClients = new[]
        {
            DependencyInjection.IntegrationHttpClientRegistry.ClientNames.Sovos,
            DependencyInjection.IntegrationHttpClientRegistry.ClientNames.ParasutInvoice,
            DependencyInjection.IntegrationHttpClientRegistry.ClientNames.TrendyolEFaturam,
            DependencyInjection.IntegrationHttpClientRegistry.ClientNames.ELogo,
            DependencyInjection.IntegrationHttpClientRegistry.ClientNames.ELogoSoap,
            DependencyInjection.IntegrationHttpClientRegistry.ClientNames.BirFatura,
            DependencyInjection.IntegrationHttpClientRegistry.ClientNames.DijitalPlanet,
            DependencyInjection.IntegrationHttpClientRegistry.ClientNames.GibPortal,
            DependencyInjection.IntegrationHttpClientRegistry.ClientNames.HBFatura,
            DependencyInjection.IntegrationHttpClientRegistry.ClientNames.GibPortalEInvoice,
        };

        foreach (var name in invoiceClients)
        {
            services.AddHttpClient(name)
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());
        }

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt))
                    + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 500)),
                onRetry: (_, _, _, _) => { });
    }

    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(60));
    }
}
