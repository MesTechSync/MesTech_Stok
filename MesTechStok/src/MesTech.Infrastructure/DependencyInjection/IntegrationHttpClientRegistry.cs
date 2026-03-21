using Microsoft.Extensions.DependencyInjection;

namespace MesTech.Infrastructure.DependencyInjection;

/// <summary>
/// Registers all named HttpClients used by Integration adapters, providers, and services.
/// Centralizes HttpClient lifecycle management via IHttpClientFactory — eliminates socket exhaustion
/// from raw <c>new HttpClient()</c> calls across 40+ adapter registrations.
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
        services.AddHttpClient(ClientNames.Trendyol);
        services.AddHttpClient(ClientNames.OpenCart);
        services.AddHttpClient(ClientNames.Ciceksepeti);
        services.AddHttpClient(ClientNames.HepsiburadaToken);
        services.AddHttpClient(ClientNames.Hepsiburada);
        services.AddHttpClient(ClientNames.Pazarama);
        services.AddHttpClient(ClientNames.PazaramaToken);
        services.AddHttpClient(ClientNames.AmazonTr);
        services.AddHttpClient(ClientNames.AmazonEu);
        services.AddHttpClient(ClientNames.AmazonFBA);
        services.AddHttpClient(ClientNames.AmazonLWA);
        services.AddHttpClient(ClientNames.Bitrix24);
        services.AddHttpClient(ClientNames.Bitrix24Auth);
        services.AddHttpClient(ClientNames.Ebay);
        services.AddHttpClient(ClientNames.Ozon);
        services.AddHttpClient(ClientNames.PttAvm);
        services.AddHttpClient(ClientNames.Shopify);
        services.AddHttpClient(ClientNames.WooCommerce);
        services.AddHttpClient(ClientNames.Etsy);
        services.AddHttpClient(ClientNames.Zalando);

        // ── Cargo Adapters ─────────────────────────────────
        services.AddHttpClient(ClientNames.YurticiKargo);
        services.AddHttpClient(ClientNames.ArasKargo);
        services.AddHttpClient(ClientNames.SuratKargo);
        services.AddHttpClient(ClientNames.MngKargo);
        services.AddHttpClient(ClientNames.PttKargo);
        services.AddHttpClient(ClientNames.HepsiJet);
        services.AddHttpClient(ClientNames.Sendeo);

        // ── Invoice Providers ──────────────────────────────
        services.AddHttpClient(ClientNames.Sovos);
        services.AddHttpClient(ClientNames.ParasutInvoice);
        services.AddHttpClient(ClientNames.TrendyolEFaturam);
        services.AddHttpClient(ClientNames.ELogoSoap);
        services.AddHttpClient(ClientNames.ELogo);
        services.AddHttpClient(ClientNames.BirFatura);
        services.AddHttpClient(ClientNames.DijitalPlanet);
        services.AddHttpClient(ClientNames.GibPortal);
        services.AddHttpClient(ClientNames.HBFatura);

        // GibPortalEInvoice — 30s timeout for e-Arsiv Portal REST calls
        services.AddHttpClient(ClientNames.GibPortalEInvoice, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // ── Services ───────────────────────────────────────
        services.AddHttpClient(ClientNames.ProductScraper);
        services.AddHttpClient(ClientNames.FeedHealthCheck);
        services.AddHttpClient(ClientNames.PayTRDirect);
        services.AddHttpClient(ClientNames.PayTRiFrame);
        services.AddHttpClient(ClientNames.Hepsilojistik);

        // ParasutAccounting — pre-configured base address
        services.AddHttpClient(ClientNames.ParasutAccounting, client =>
        {
            client.BaseAddress = new Uri("https://api.parasut.com/v4/");
        });

        return services;
    }
}
