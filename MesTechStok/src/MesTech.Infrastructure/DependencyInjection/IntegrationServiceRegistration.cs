using MesTech.Application.Interfaces;
using MesTech.Infrastructure.Integration.Adapters;
using MesTech.Infrastructure.Integration.Auth;
using MesTech.Infrastructure.Integration.Factory;
using MesTech.Infrastructure.Integration.Orchestration;
using MesTech.Infrastructure.Integration.Webhooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MesTech.Infrastructure.DependencyInjection;

public static class IntegrationServiceRegistration
{
    public static IServiceCollection AddIntegrationServices(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        // Adapters — singleton with manually created HttpClient
        services.AddSingleton<TrendyolAdapter>(sp =>
            new TrendyolAdapter(new HttpClient(), sp.GetRequiredService<ILogger<TrendyolAdapter>>()));
        services.AddSingleton<OpenCartAdapter>(sp =>
            new OpenCartAdapter(new HttpClient(), sp.GetRequiredService<ILogger<OpenCartAdapter>>()));

        // Multi-registration: each adapter also registered as IIntegratorAdapter
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<TrendyolAdapter>());
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<OpenCartAdapter>());

        // Dalga 3: Ciceksepeti + Hepsiburada marketplace adapters
        services.AddSingleton<CiceksepetiAdapter>(sp =>
            new CiceksepetiAdapter(new HttpClient(), sp.GetRequiredService<ILogger<CiceksepetiAdapter>>()));
        services.AddSingleton<HepsiburadaAdapter>(sp =>
            new HepsiburadaAdapter(new HttpClient(), sp.GetRequiredService<ILogger<HepsiburadaAdapter>>()));

        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<CiceksepetiAdapter>());
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<HepsiburadaAdapter>());

        // Dalga 4: Pazarama marketplace adapter — OAuth2, async batch, 2-stage cargo
        services.AddSingleton<PazaramaAdapter>(sp =>
            new PazaramaAdapter(new HttpClient(), sp.GetRequiredService<ILogger<PazaramaAdapter>>()));
        services.AddSingleton<IIntegratorAdapter>(sp => sp.GetRequiredService<PazaramaAdapter>());

        // Dalga 3: Cargo adapters — SCOPED (multi-tenant credential isolation)
        services.AddScoped<ICargoAdapter>(sp =>
            new YurticiKargoAdapter(new HttpClient(), sp.GetRequiredService<ILogger<YurticiKargoAdapter>>()));
        services.AddScoped<ICargoAdapter>(sp =>
            new ArasKargoAdapter(new HttpClient(), sp.GetRequiredService<ILogger<ArasKargoAdapter>>()));
        services.AddScoped<ICargoAdapter>(sp =>
            new SuratKargoAdapter(new HttpClient(), sp.GetRequiredService<ILogger<SuratKargoAdapter>>()));

        // Factory — receives IEnumerable<IIntegratorAdapter>
        services.AddSingleton<IAdapterFactory, AdapterFactory>();

        // Cargo factory + selector + auto-shipment — Scoped (depends on scoped cargo adapters)
        services.AddScoped<ICargoProviderFactory, CargoProviderFactory>();
        services.AddScoped<ICargoProviderSelector, CargoProviderSelector>();
        services.AddScoped<IAutoShipmentService, AutoShipmentService>();

        // Orchestrator — receives IAdapterFactory
        services.AddSingleton<IIntegratorOrchestrator, IntegratorOrchestratorService>();

        // Webhook receiver
        services.AddScoped<IWebhookReceiverService, WebhookReceiverService>();

        // Token cache (in-memory, Redis swap later)
        services.AddSingleton<ITokenCacheProvider, InMemoryTokenCacheProvider>();

        // Invoice provider — feature flag controlled
        var mockFlagValue = configuration?["Features:UseMockInvoice"];
        var useMockInvoice = string.IsNullOrEmpty(mockFlagValue)
            || !bool.TryParse(mockFlagValue, out var parsed)
            || parsed;
        if (useMockInvoice)
        {
            services.AddSingleton<IInvoiceProvider, MockInvoiceProvider>();
        }
        else
        {
            // Gercek provider eklendiginde buraya kayit yapilacak
            // services.AddSingleton<IInvoiceProvider, SovosInvoiceProvider>();
            throw new InvalidOperationException(
                "IInvoiceProvider gercek implementasyonu henuz mevcut degil. " +
                "Features:UseMockInvoice=true yapin veya gercek provider ekleyin.");
        }

        return services;
    }
}
