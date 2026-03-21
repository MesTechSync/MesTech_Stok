using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.DependencyInjection;

/// <summary>
/// Registers all named HttpClients used by Integration adapters, providers, and services.
/// Centralizes HttpClient lifecycle management via IHttpClientFactory — eliminates socket exhaustion
/// from raw <c>new HttpClient()</c> calls across 40+ adapter registrations.
/// Socket exhaustion prevention + DNS rotation + configurable timeouts.
/// </summary>
public static class IntegrationHttpClientRegistry
{
    /// <summary>
    /// Named HttpClient keys for all Integration-layer consumers.
    /// ERP clients live in <see cref="Integration.ERP.ErpResiliencePolicies.ClientNames"/>.
    /// </summary>
    public static class ClientNames
    {
        // ── Platform Adapters ──────────────────────────────
        public const string Trendyol = "Trendyol";
        public const string OpenCart = "OpenCart";
        public const string Ciceksepeti = "Ciceksepeti";
        public const string HepsiburadaToken = "HepsiburadaToken";
        public const string Hepsiburada = "Hepsiburada";
        public const string Pazarama = "Pazarama";
        public const string PazaramaToken = "PazaramaToken";
        public const string AmazonTr = "AmazonTr";
        public const string AmazonEu = "AmazonEu";
        public const string AmazonFBA = "AmazonFBA";
        public const string AmazonLWA = "AmazonLWA";
        public const string Bitrix24 = "Bitrix24";
        public const string Bitrix24Auth = "Bitrix24Auth";
        public const string Ebay = "Ebay";
        public const string Ozon = "Ozon";
        public const string PttAvm = "PttAvm";
        public const string Shopify = "Shopify";
        public const string WooCommerce = "WooCommerce";
        public const string Etsy = "Etsy";
        public const string Zalando = "Zalando";

        // ── Cargo Adapters ─────────────────────────────────
        public const string YurticiKargo = "YurticiKargo";
        public const string ArasKargo = "ArasKargo";
        public const string SuratKargo = "SuratKargo";
        public const string MngKargo = "MngKargo";
        public const string PttKargo = "PttKargo";
        public const string HepsiJet = "HepsiJet";
        public const string Sendeo = "Sendeo";

        // ── Invoice Providers ──────────────────────────────
        public const string Sovos = "Sovos";
        public const string ParasutInvoice = "ParasutInvoice";
        public const string TrendyolEFaturam = "TrendyolEFaturam";
        public const string ELogoSoap = "ELogoSoap";
        public const string ELogo = "ELogo";
        public const string BirFatura = "BirFatura";
        public const string DijitalPlanet = "DijitalPlanet";
        public const string GibPortal = "GibPortal";
        public const string HBFatura = "HBFatura";
        public const string GibPortalEInvoice = "GibPortalEInvoice";

        // ── Services ───────────────────────────────────────
        public const string ProductScraper = "ProductScraper";
        public const string ParasutAccounting = "ParasutAccounting";
        public const string FeedHealthCheck = "FeedHealthCheck";
        public const string PayTRDirect = "PayTRDirect";
        public const string PayTRiFrame = "PayTRiFrame";
        public const string Hepsilojistik = "Hepsilojistik";
    }

    /// <summary>
    /// Registers all named HttpClients for the Integration layer.
    /// Must be called before <c>AddIntegrationServices</c> resolves any adapter.
    /// </summary>
    public static IServiceCollection AddIntegrationHttpClients(this IServiceCollection services)
    {
        // ── Platform Adapters ──────────────────────────────
        RegisterDefault(services, ClientNames.Trendyol);
        RegisterDefault(services, ClientNames.OpenCart);
        RegisterDefault(services, ClientNames.Ciceksepeti);
        RegisterDefault(services, ClientNames.HepsiburadaToken);
        RegisterDefault(services, ClientNames.Hepsiburada);
        RegisterDefault(services, ClientNames.Pazarama);
        RegisterDefault(services, ClientNames.PazaramaToken);
        RegisterDefault(services, ClientNames.AmazonTr);
        RegisterDefault(services, ClientNames.AmazonEu);
        RegisterDefault(services, ClientNames.AmazonFBA);
        RegisterDefault(services, ClientNames.AmazonLWA);
        RegisterDefault(services, ClientNames.Bitrix24);
        RegisterDefault(services, ClientNames.Bitrix24Auth);
        RegisterDefault(services, ClientNames.Ebay);
        RegisterDefault(services, ClientNames.Ozon);
        RegisterDefault(services, ClientNames.PttAvm);
        RegisterDefault(services, ClientNames.Shopify);
        RegisterDefault(services, ClientNames.WooCommerce);
        RegisterDefault(services, ClientNames.Etsy);
        RegisterDefault(services, ClientNames.Zalando);

        // ── Cargo Adapters ─────────────────────────────────
        RegisterDefault(services, ClientNames.YurticiKargo);
        RegisterDefault(services, ClientNames.ArasKargo);
        RegisterDefault(services, ClientNames.SuratKargo);
        RegisterDefault(services, ClientNames.MngKargo);
        RegisterDefault(services, ClientNames.PttKargo);
        RegisterDefault(services, ClientNames.HepsiJet);
        RegisterDefault(services, ClientNames.Sendeo);

        // ── Invoice Providers ──────────────────────────────
        RegisterDefault(services, ClientNames.Sovos);
        RegisterDefault(services, ClientNames.ParasutInvoice);
        RegisterDefault(services, ClientNames.TrendyolEFaturam);
        RegisterDefault(services, ClientNames.ELogoSoap);
        RegisterDefault(services, ClientNames.ELogo);
        RegisterDefault(services, ClientNames.BirFatura);
        RegisterDefault(services, ClientNames.DijitalPlanet);
        RegisterDefault(services, ClientNames.GibPortal);
        RegisterDefault(services, ClientNames.HBFatura);

        // GibPortalEInvoice — 30s timeout for e-Arsiv Portal REST calls
        services.AddHttpClient(ClientNames.GibPortalEInvoice, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ── Services ───────────────────────────────────────
        RegisterDefault(services, ClientNames.ProductScraper);

        // ParasutAccounting — pre-configured base address
        services.AddHttpClient(ClientNames.ParasutAccounting, client =>
        {
            client.BaseAddress = new Uri("https://api.parasut.com/v4/");
        });
        RegisterDefault(services, ClientNames.FeedHealthCheck);
        RegisterDefault(services, ClientNames.PayTRDirect);
        RegisterDefault(services, ClientNames.PayTRiFrame);
        RegisterDefault(services, ClientNames.Hepsilojistik);

        return services;
    }

    private static void RegisterDefault(IServiceCollection services, string name)
    {
        services.AddHttpClient(name);
    }
}
