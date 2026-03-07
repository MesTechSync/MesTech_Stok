using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Infrastructure.Integration.Auth;
using MesTech.Infrastructure.Integration.Factory;
using MesTech.Infrastructure.Integration.Orchestration;
using MesTech.Infrastructure.Integration.Webhooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.DependencyInjection;

public static class IntegrationServiceRegistration
{
    public static IServiceCollection AddIntegrationServices(
        this IServiceCollection services)
    {
        // Adapters — singleton with manually created HttpClient
        services.AddSingleton<TrendyolAdapter>(sp =>
            new TrendyolAdapter(new HttpClient(), sp.GetRequiredService<ILogger<TrendyolAdapter>>()));
        services.AddSingleton<OpenCartAdapter>(sp =>
            new OpenCartAdapter(new HttpClient(), sp.GetRequiredService<ILogger<OpenCartAdapter>>()));

        // Multi-registration: each adapter also registered as IIntegratorAdapter
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<TrendyolAdapter>());
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<OpenCartAdapter>());

        // Factory — receives IEnumerable<IIntegratorAdapter>
        services.AddSingleton<IAdapterFactory, AdapterFactory>();

        // Orchestrator — receives IAdapterFactory
        services.AddSingleton<IIntegratorOrchestrator, IntegratorOrchestratorService>();

        // Webhook receiver
        services.AddScoped<IWebhookReceiverService, WebhookReceiverService>();

        // Token cache (in-memory, Redis swap later)
        services.AddSingleton<ITokenCacheProvider, InMemoryTokenCacheProvider>();

        return services;
    }
}
